import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest'
import { testApiError, testOutboxItem } from '@/test/outboxTestData'
import { createDeferred, resetFakeOutboxStorage, seedOutbox, storedIds } from '@/test/outboxTestHelpers'
import type { OutboxItem } from './outboxItem'
import { createOutboxSender } from './outboxSender'

type SendItem = (item: OutboxItem) => Promise<unknown>
type AfterSent = (item: OutboxItem) => Promise<void>

const personId = 'person-1'

function newSender() {
  const sendItem = vi.fn<SendItem>(async () => undefined)
  const afterSent = vi.fn<AfterSent>(async () => undefined)
  const sender = createOutboxSender({ personId, sendItem, afterSent })
  return { sender, sendItem, afterSent }
}

function seedWaiting(...ids: string[]): void {
  seedOutbox(
    personId,
    ids.map((id) => testOutboxItem(id)),
  )
}

function sentIds(sendItem: Mock<SendItem>): string[] {
  return sendItem.mock.calls.map(([item]) => item.id)
}

function stateOf(sender: ReturnType<typeof createOutboxSender>, id: string): string | undefined {
  return sender.getSnapshot().items.find((item) => item.id === id)?.state
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

describe('an item whose original request is still running', () => {
  it('is not sent by a run while the original is running', async () => {
    seedWaiting('a')
    const { sender, sendItem } = newSender()
    await settle()
    sender.holdWhileSaving('a', createDeferred<void>().promise)

    await sender.run()

    expect(sendItem).not.toHaveBeenCalled()
    expect(storedIds(personId)).toEqual(['a'])
  })

  it('lets a run end without waiting for the original', async () => {
    seedWaiting('a')
    const { sender } = newSender()
    await settle()
    sender.holdWhileSaving('a', createDeferred<void>().promise)

    const ended = vi.fn()
    await sender.run().then(ended)

    expect(ended).toHaveBeenCalledTimes(1)
  })

  it('shows as sending, so the Discard button of the sheet is disabled', async () => {
    seedWaiting('a')
    const { sender } = newSender()
    await settle()

    sender.holdWhileSaving('a', createDeferred<void>().promise)

    expect(stateOf(sender, 'a')).toBe('sending')
  })

  it('refuses to be discarded and stays in storage', async () => {
    seedWaiting('a')
    const { sender } = newSender()
    await settle()
    sender.holdWhileSaving('a', createDeferred<void>().promise)

    const discarded = await sender.discard('a')

    expect(discarded).toBe(false)
    expect(stateOf(sender, 'a')).toBe('sending')
    expect(storedIds(personId)).toEqual(['a'])
  })

  it('does not stop the other waiting items from being sent', async () => {
    seedWaiting('a', 'b')
    const { sender, sendItem } = newSender()
    await settle()
    sender.holdWhileSaving('a', createDeferred<void>().promise)

    await sender.run()

    expect(sentIds(sendItem)).toEqual(['b'])
    expect(storedIds(personId)).toEqual(['a'])
  })

  it('stays out of the way of a retry timer and of later runs while the original is running', async () => {
    seedWaiting('a')
    const { sender, sendItem } = newSender()
    await settle()
    sender.holdWhileSaving('a', createDeferred<void>().promise)

    await sender.run()
    await vi.advanceTimersByTimeAsync(120_000)
    await sender.run()

    expect(sendItem).not.toHaveBeenCalled()
  })
})

describe('an item whose original request did not succeed', () => {
  const notSucceeded: [string, unknown][] = [
    ['no answer from the network', testApiError(null)],
    ['status 502', testApiError(502)],
    ['status 503', testApiError(503)],
    ['status 504', testApiError(504)],
    ['a refusal with status 400', testApiError(400, 'GROUP_FINISHED')],
    ['status 500', testApiError(500)],
    ['an error that is not an API error', new Error('boom')],
  ]

  it.each(notSucceeded)('is released and sent once, by the sender itself, after %s', async (_name, error) => {
    seedWaiting('a')
    const { sender, sendItem } = newSender()
    await settle()
    const original = createDeferred<void>()
    sender.holdWhileSaving('a', original.promise)
    await sender.run()

    original.reject(error)
    await settle()

    expect(sentIds(sendItem)).toEqual(['a'])
    expect(sender.getSnapshot().items).toEqual([])
    expect(storedIds(personId)).toEqual([])
  })

  it('is sent again later like any item when the sender then gets no answer either', async () => {
    seedWaiting('a')
    const { sender, sendItem } = newSender()
    await settle()
    sendItem.mockRejectedValueOnce(testApiError(null)).mockResolvedValue(undefined)
    const original = createDeferred<void>()
    sender.holdWhileSaving('a', original.promise)

    original.reject(testApiError(null))
    await settle()
    await vi.advanceTimersByTimeAsync(2000)

    expect(sentIds(sendItem)).toEqual(['a', 'a'])
    expect(sender.getSnapshot().items).toEqual([])
  })

  it('is marked failed when the sender then gets a refusal', async () => {
    seedWaiting('a')
    const { sender, sendItem } = newSender()
    await settle()
    sendItem.mockRejectedValue(testApiError(400, 'GROUP_FINISHED'))
    const original = createDeferred<void>()
    sender.holdWhileSaving('a', original.promise)

    original.reject(testApiError(400, 'GROUP_FINISHED'))
    await settle()

    expect(stateOf(sender, 'a')).toBe('failed')
  })
})

describe('an item whose original request succeeded', () => {
  async function heldThenSucceeded() {
    seedWaiting('a')
    const rest = newSender()
    await settle()
    const original = createDeferred<string>()
    rest.sender.holdWhileSaving('a', original.promise)
    await rest.sender.run()
    original.resolve('saved')
    await settle()
    return rest
  }

  it('is never sent, also not by a run right after', async () => {
    const { sender, sendItem } = await heldThenSucceeded()

    await sender.run()
    await vi.advanceTimersByTimeAsync(120_000)

    expect(sendItem).not.toHaveBeenCalled()
  })

  it('can be discarded afterwards, which is how the late success removes it', async () => {
    const { sender, sendItem } = await heldThenSucceeded()

    await sender.discard('a')

    expect(sender.getSnapshot().items).toEqual([])
    expect(storedIds(personId)).toEqual([])
    expect(sendItem).not.toHaveBeenCalled()
  })
})
