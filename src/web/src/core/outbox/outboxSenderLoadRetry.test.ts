import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { testOutboxItem } from '@/test/outboxTestData'
import { fakeIdbKeyval, resetFakeOutboxStorage, seedOutbox, storedIds } from '@/test/outboxTestHelpers'
import type { OutboxItem } from './outboxItem'
import { createOutboxSender } from './outboxSender'
import { outboxReadTimeLimitMs } from './outboxStorage'

type SendItem = (item: OutboxItem) => Promise<unknown>
type AfterSent = (item: OutboxItem) => Promise<void>

const personId = 'person-1'

function newSender() {
  const sendItem = vi.fn<SendItem>(async () => undefined)
  const afterSent = vi.fn<AfterSent>(async () => undefined)
  const sender = createOutboxSender({ personId, sendItem, afterSent })
  return { sender, sendItem }
}

function failTheFirstRead(): void {
  fakeIdbKeyval.get.mockRejectedValueOnce(new Error('IndexedDB read failed'))
}

function snapshotIds(sender: ReturnType<typeof createOutboxSender>): string[] {
  return sender.getSnapshot().items.map((item) => item.id)
}

async function settle(): Promise<void> {
  await vi.advanceTimersByTimeAsync(0)
}

beforeEach(() => {
  resetFakeOutboxStorage()
  vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] })
})

afterEach(() => {
  vi.useRealTimers()
  vi.restoreAllMocks()
})

describe('after the first load of the queue failed', () => {
  it('loads again on the next enqueue, which then works, with the saved items before the new one', async () => {
    seedOutbox(personId, [testOutboxItem('saved')])
    failTheFirstRead()
    const { sender } = newSender()
    await settle()

    await sender.enqueue(testOutboxItem('new'))

    expect(snapshotIds(sender)).toEqual(['saved', 'new'])
    expect(storedIds(personId)).toEqual(['saved', 'new'])
  })

  it('loads again on the next run and sends the saved items', async () => {
    seedOutbox(personId, [testOutboxItem('saved')])
    failTheFirstRead()
    const { sender, sendItem } = newSender()
    await settle()

    await sender.run()

    expect(sendItem).toHaveBeenCalledTimes(1)
    expect(sendItem.mock.calls[0][0].id).toBe('saved')
    expect(snapshotIds(sender)).toEqual([])
  })

  it('loads again on the next retry of a failed item', async () => {
    const failure = { errorCode: 'GROUP_FINISHED', httpStatus: 400 }
    seedOutbox(personId, [testOutboxItem('refused', { state: 'failed', failure })])
    failTheFirstRead()
    const { sender, sendItem } = newSender()
    await settle()

    await sender.retry('refused')
    await settle()

    expect(sendItem).toHaveBeenCalledTimes(1)
    expect(snapshotIds(sender)).toEqual([])
  })

  it('loads again on the next discard', async () => {
    seedOutbox(personId, [testOutboxItem('saved')])
    failTheFirstRead()
    const { sender } = newSender()
    await settle()

    const discarded = await sender.discard('saved')

    expect(discarded).toBe(true)
    expect(storedIds(personId)).toEqual([])
  })

  it('shows the loaded items in the snapshot once a later call loaded them', async () => {
    seedOutbox(personId, [testOutboxItem('saved')])
    failTheFirstRead()
    const { sender } = newSender()
    await settle()
    expect(snapshotIds(sender)).toEqual([])

    await sender.enqueue(testOutboxItem('new'))

    expect(snapshotIds(sender)).toContain('saved')
  })

  it('also loads again when the first read ran into its 3 second time limit', async () => {
    seedOutbox(personId, [testOutboxItem('saved')])
    fakeIdbKeyval.get.mockReturnValueOnce(new Promise<OutboxItem[] | undefined>(() => {}))
    const { sender } = newSender()
    await vi.advanceTimersByTimeAsync(outboxReadTimeLimitMs)

    await sender.enqueue(testOutboxItem('new'))

    expect(snapshotIds(sender)).toEqual(['saved', 'new'])
  })

  it('reads the browser database once more, not once for every caller, when several calls come at the same time', async () => {
    seedOutbox(personId, [testOutboxItem('saved')])
    failTheFirstRead()
    const { sender } = newSender()
    await settle()

    await Promise.all([sender.enqueue(testOutboxItem('first')), sender.enqueue(testOutboxItem('second'))])

    expect(fakeIdbKeyval.get).toHaveBeenCalledTimes(2)
    expect(snapshotIds(sender).toSorted()).toEqual(['first', 'saved', 'second'])
  })

  it('does not read the browser database again once a load worked', async () => {
    failTheFirstRead()
    const { sender } = newSender()
    await settle()
    await sender.enqueue(testOutboxItem('first'))

    await sender.enqueue(testOutboxItem('second'))
    await sender.run()

    expect(fakeIdbKeyval.get).toHaveBeenCalledTimes(2)
  })
})

describe('when the load fails again on the next call', () => {
  it('rejects that call with the error that names the person and the browser database', async () => {
    failTheFirstRead()
    const { sender } = newSender()
    await settle()
    fakeIdbKeyval.get.mockRejectedValueOnce(new Error('IndexedDB read failed again'))

    const enqueuing = sender.enqueue(testOutboxItem('new'))

    await expect(enqueuing).rejects.toThrow(`Could not load the outbox of person ${personId} from this browser (IndexedDB): IndexedDB read failed again`)
    expect(storedIds(personId)).toEqual([])
  })

  it.each(['run', 'retry', 'discard'] as const)('rejects %s with that error and sends nothing', async (method) => {
    failTheFirstRead()
    const { sender, sendItem } = newSender()
    await settle()
    fakeIdbKeyval.get.mockRejectedValueOnce(new Error('IndexedDB read failed again'))

    const calling = method === 'run' ? sender.run() : sender[method]('any')

    await expect(calling).rejects.toThrow('IndexedDB read failed again')
    expect(sendItem).not.toHaveBeenCalled()
  })

  it('works on the call after that when the browser database answers again', async () => {
    seedOutbox(personId, [testOutboxItem('saved')])
    failTheFirstRead()
    const { sender } = newSender()
    await settle()
    fakeIdbKeyval.get.mockRejectedValueOnce(new Error('IndexedDB read failed again'))
    await expect(sender.enqueue(testOutboxItem('lost'))).rejects.toThrow(`person ${personId}`)

    await sender.enqueue(testOutboxItem('new'))

    expect(snapshotIds(sender)).toEqual(['saved', 'new'])
    expect(storedIds(personId)).toEqual(['saved', 'new'])
  })
})
