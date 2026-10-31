import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest'
import {
  createDeferred,
  resetFakeOutboxStorage,
  seedOutbox,
  storedIds,
  storedItems,
} from '@/test/outboxTestHelpers'
import { testApiError, testOutboxItem } from '@/test/outboxTestData'
import type { OutboxItem } from './outboxItem'
import { createOutboxSender } from './outboxSender'

vi.mock('idb-keyval', async () => (await import('@/test/outboxTestHelpers')).fakeIdbKeyval)

type SendItem = (item: OutboxItem) => Promise<unknown>
type AfterSent = (item: OutboxItem) => Promise<void>

const personId = 'person-1'
const otherPersonId = 'person-2'

interface SenderOptions {
  sendItem?: Mock<SendItem>
  afterSent?: Mock<AfterSent>
  personId?: string
}

function newSender(options: SenderOptions = {}) {
  const sendItem = options.sendItem ?? vi.fn<SendItem>(async () => undefined)
  const afterSent = options.afterSent ?? vi.fn<AfterSent>(async () => undefined)
  const sender = createOutboxSender({
    personId: options.personId ?? personId,
    sendItem,
    afterSent,
  })
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

function snapshotIds(sender: ReturnType<typeof createOutboxSender>): string[] {
  return sender.getSnapshot().items.map((item) => item.id)
}

function snapshotItem(
  sender: ReturnType<typeof createOutboxSender>,
  id: string,
): OutboxItem | undefined {
  return sender.getSnapshot().items.find((item) => item.id === id)
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

describe('loading the queue', () => {
  it('starts with an empty snapshot before anything is loaded', () => {
    seedWaiting('a')

    const { sender } = newSender()

    expect(sender.getSnapshot()).toEqual({ items: [], isSending: false, justFinished: false })
  })

  it('loads the items of the person on creation, oldest first', async () => {
    seedWaiting('a', 'b', 'c')

    const { sender } = newSender()
    await settle()

    expect(snapshotIds(sender)).toEqual(['a', 'b', 'c'])
  })

  it('treats an item left sending by a closed tab as waiting again', async () => {
    seedOutbox(personId, [testOutboxItem('a', { state: 'sending' })])

    const { sender } = newSender()
    await settle()

    expect(snapshotItem(sender, 'a')?.state).toBe('waiting')
  })

  it('sends an item left sending by a closed tab with the same id', async () => {
    seedOutbox(personId, [testOutboxItem('a', { state: 'sending' })])
    const { sender, sendItem } = newSender()

    await sender.run()

    expect(sentIds(sendItem)).toEqual(['a'])
  })

  it('keeps a failed item failed after loading', async () => {
    const failure = { errorCode: 'GROUP_FINISHED', httpStatus: 400 }
    seedOutbox(personId, [testOutboxItem('a', { state: 'failed', failure })])

    const { sender } = newSender()
    await settle()

    expect(snapshotItem(sender, 'a')).toMatchObject({ state: 'failed', failure })
  })

  it('shows and sends only the items of its own person', async () => {
    seedWaiting('mine')
    seedOutbox(otherPersonId, [testOutboxItem('theirs')])
    const { sender, sendItem } = newSender()

    await sender.run()

    expect(sentIds(sendItem)).toEqual(['mine'])
    expect(snapshotIds(sender)).toEqual([])
    expect(storedIds(otherPersonId)).toEqual(['theirs'])
  })

  it('adds a queued item after the loaded ones in the snapshot and in storage', async () => {
    seedWaiting('a')
    const { sender } = newSender()
    await settle()

    await sender.enqueue(testOutboxItem('b'))

    expect(snapshotIds(sender)).toEqual(['a', 'b'])
    expect(storedIds(personId)).toEqual(['a', 'b'])
  })

  it('keeps an item that is queued before the saved items finished loading, after the saved ones', async () => {
    seedWaiting('a')
    const { sender } = newSender()

    await sender.enqueue(testOutboxItem('b'))
    await settle()

    expect(snapshotIds(sender)).toEqual(['a', 'b'])
    expect(storedIds(personId)).toEqual(['a', 'b'])
  })

  it('does not show a queued item twice when its id is queued again', async () => {
    const { sender } = newSender()
    await settle()
    await sender.enqueue(testOutboxItem('a'))

    await sender.enqueue(testOutboxItem('a'))

    expect(snapshotIds(sender)).toEqual(['a'])
    expect(storedIds(personId)).toEqual(['a'])
  })

  it('sends an item that was queued after the sender was created', async () => {
    const { sender, sendItem } = newSender()
    await settle()
    await sender.enqueue(testOutboxItem('a'))

    await sender.run()

    expect(sentIds(sendItem)).toEqual(['a'])
  })
})

describe('sending the queue', () => {
  it('sends waiting items oldest first', async () => {
    seedWaiting('c', 'a', 'b')
    const { sender, sendItem } = newSender()

    await sender.run()

    expect(sentIds(sendItem)).toEqual(['c', 'a', 'b'])
  })

  it('gives the item with its payload to the send function', async () => {
    const item = testOutboxItem('a', { kind: 'oneBill', groupId: null })
    seedOutbox(personId, [item])
    const { sender, sendItem } = newSender()

    await sender.run()

    expect(sendItem).toHaveBeenCalledWith(
      expect.objectContaining({ id: 'a', kind: 'oneBill', payload: item.payload }),
    )
  })

  it('never has two items in flight at once', async () => {
    seedWaiting('a', 'b', 'c')
    let inFlight = 0
    let mostInFlight = 0
    const sendItem = vi.fn<SendItem>(async () => {
      inFlight += 1
      mostInFlight = Math.max(mostInFlight, inFlight)
      await Promise.resolve()
      await Promise.resolve()
      inFlight -= 1
    })
    const { sender } = newSender({ sendItem })

    await sender.run()

    expect(sendItem).toHaveBeenCalledTimes(3)
    expect(mostInFlight).toBe(1)
  })

  it('marks the item sending while its request is in flight and the others waiting', async () => {
    seedWaiting('a', 'b')
    const sending = createDeferred<void>()
    const { sender } = newSender({ sendItem: vi.fn<SendItem>(() => sending.promise) })

    const running = sender.run()
    await settle()

    expect(snapshotItem(sender, 'a')?.state).toBe('sending')
    expect(snapshotItem(sender, 'b')?.state).toBe('waiting')
    expect(sender.getSnapshot().isSending).toBe(true)
    sending.resolve()
    await running
  })

  it('does not send the next item before the one in flight has answered', async () => {
    seedWaiting('a', 'b')
    const sending = createDeferred<void>()
    const sendItem = vi.fn<SendItem>(() => sending.promise)
    const { sender } = newSender({ sendItem })

    const running = sender.run()
    await settle()

    expect(sentIds(sendItem)).toEqual(['a'])
    sending.resolve()
    await running
    expect(sentIds(sendItem)).toEqual(['a', 'b'])
  })

  it('removes a sent item from the snapshot and from storage', async () => {
    seedWaiting('a', 'b')
    const { sender } = newSender()

    await sender.run()

    expect(snapshotIds(sender)).toEqual([])
    expect(storedIds(personId)).toEqual([])
  })

  it('calls afterSent with each sent item', async () => {
    seedWaiting('a', 'b')
    const { sender, afterSent } = newSender()

    await sender.run()

    expect(afterSent.mock.calls.map(([item]) => item.id)).toEqual(['a', 'b'])
  })

  it('calls afterSent only after the item was sent', async () => {
    seedWaiting('a')
    const sending = createDeferred<void>()
    const { sender, afterSent } = newSender({ sendItem: vi.fn<SendItem>(() => sending.promise) })

    const running = sender.run()
    await settle()

    expect(afterSent).not.toHaveBeenCalled()
    sending.resolve()
    await running
    expect(afterSent).toHaveBeenCalledTimes(1)
  })

  it('keeps the item in the snapshot and in storage until afterSent has finished', async () => {
    seedWaiting('a')
    const refreshed = createDeferred<void>()
    const afterSent = vi.fn<AfterSent>(() => refreshed.promise)
    const { sender } = newSender({ afterSent })

    const running = sender.run()
    await settle()

    expect(afterSent).toHaveBeenCalledTimes(1)
    expect(snapshotIds(sender)).toEqual(['a'])
    expect(storedIds(personId)).toEqual(['a'])
    refreshed.resolve()
    await running
    expect(snapshotIds(sender)).toEqual([])
    expect(storedIds(personId)).toEqual([])
  })

  it('does not send the next item before afterSent of the previous one has finished', async () => {
    seedWaiting('a', 'b')
    const refreshed = createDeferred<void>()
    const afterSent = vi.fn<AfterSent>(() => refreshed.promise)
    const { sender, sendItem } = newSender({ afterSent })

    const running = sender.run()
    await settle()

    expect(sentIds(sendItem)).toEqual(['a'])
    refreshed.resolve()
    await running
    expect(sentIds(sendItem)).toEqual(['a', 'b'])
  })

  it('does nothing when the queue is empty', async () => {
    const { sender, sendItem, afterSent } = newSender()

    await sender.run()

    expect(sendItem).not.toHaveBeenCalled()
    expect(afterSent).not.toHaveBeenCalled()
  })

  it('does not start a second loop when run is called while it is already running', async () => {
    seedWaiting('a', 'b')
    const sending = createDeferred<void>()
    const sendItem = vi.fn<SendItem>(() => sending.promise)
    const { sender } = newSender({ sendItem })

    const first = sender.run()
    const second = sender.run()
    await settle()

    expect(sentIds(sendItem)).toEqual(['a'])
    sending.resolve()
    await Promise.all([first, second])
    expect(sentIds(sendItem)).toEqual(['a', 'b'])
  })

  it('can run again after a finished run to send items queued meanwhile', async () => {
    seedWaiting('a')
    const { sender, sendItem } = newSender()
    await sender.run()
    await sender.enqueue(testOutboxItem('b'))

    await sender.run()

    expect(sentIds(sendItem)).toEqual(['a', 'b'])
  })

})

describe('telling the screen that everything is sent', () => {
  it('is not finished before anything ran', async () => {
    seedWaiting('a')
    const { sender } = newSender()
    await settle()

    expect(sender.getSnapshot().justFinished).toBe(false)
  })

  it('is not finished after a run that found nothing to send', async () => {
    const { sender } = newSender()

    await sender.run()

    expect(sender.getSnapshot().justFinished).toBe(false)
  })

  it('is finished and no longer sending after the last item was sent', async () => {
    seedWaiting('a', 'b')
    const { sender } = newSender()

    await sender.run()

    expect(sender.getSnapshot()).toEqual({ items: [], isSending: false, justFinished: true })
  })

  it('is not finished while a later item is still waiting', async () => {
    seedWaiting('a', 'b')
    const secondSending = createDeferred<void>()
    const sendItem = vi
      .fn<SendItem>()
      .mockResolvedValueOnce(undefined)
      .mockReturnValueOnce(secondSending.promise)
    const { sender } = newSender({ sendItem })

    const running = sender.run()
    await settle()

    expect(sender.getSnapshot().justFinished).toBe(false)
    secondSending.resolve()
    await running
    expect(sender.getSnapshot().justFinished).toBe(true)
  })

  it('is not finished when a failed item is left in the queue', async () => {
    seedWaiting('a')
    const sendItem = vi.fn<SendItem>().mockRejectedValue(testApiError(400, 'GROUP_FINISHED'))
    const { sender } = newSender({ sendItem })

    await sender.run()

    expect(sender.getSnapshot().justFinished).toBe(false)
  })

  it('is no longer finished once a new item is queued', async () => {
    seedWaiting('a')
    const { sender } = newSender()
    await sender.run()
    expect(sender.getSnapshot().justFinished).toBe(true)

    await sender.enqueue(testOutboxItem('b'))

    expect(sender.getSnapshot().justFinished).toBe(false)
  })

  it('is no longer finished while the next run is sending', async () => {
    seedWaiting('a')
    const nextSending = createDeferred<void>()
    const sendItem = vi
      .fn<SendItem>()
      .mockResolvedValueOnce(undefined)
      .mockReturnValueOnce(nextSending.promise)
    const { sender } = newSender({ sendItem })
    await sender.run()
    await sender.enqueue(testOutboxItem('b'))

    const running = sender.run()
    await settle()

    expect(sender.getSnapshot().justFinished).toBe(false)
    expect(sender.getSnapshot().isSending).toBe(true)
    nextSending.resolve()
    await running
  })

  it('is finished again after the next item was sent', async () => {
    seedWaiting('a')
    const { sender } = newSender()
    await sender.run()
    await sender.enqueue(testOutboxItem('b'))

    await sender.run()

    expect(sender.getSnapshot().justFinished).toBe(true)
  })
})

describe('trying again after the server could not be reached', () => {
  const retryStatuses: (number | null)[] = [null, 502, 503, 504, 408, 425, 429]

  it.each(retryStatuses)(
    'puts the item back to waiting and tries it again after 2 seconds on status %s',
    async (httpStatus) => {
      seedWaiting('a')
      const sendItem = vi
        .fn<SendItem>()
        .mockRejectedValueOnce(testApiError(httpStatus))
        .mockResolvedValue(undefined)
      const { sender } = newSender({ sendItem })

      void sender.run()
      await settle()

      expect(sentIds(sendItem)).toEqual(['a'])
      expect(snapshotItem(sender, 'a')).toMatchObject({ state: 'waiting', failure: null })
      expect(storedItems(personId)[0]).toMatchObject({ state: 'waiting', failure: null })
      await vi.advanceTimersByTimeAsync(1999)
      expect(sentIds(sendItem)).toEqual(['a'])
      await vi.advanceTimersByTimeAsync(1)
      expect(sentIds(sendItem)).toEqual(['a', 'a'])
      expect(snapshotIds(sender)).toEqual([])
    },
  )

  it('waits 2, 5, 15, 30 and then 60 seconds between tries while it keeps failing', async () => {
    seedWaiting('a')
    const sendItem = vi.fn<SendItem>()
    for (let failure = 0; failure < 6; failure += 1) {
      sendItem.mockRejectedValueOnce(testApiError(null))
    }
    sendItem.mockResolvedValue(undefined)
    const { sender } = newSender({ sendItem })
    void sender.run()
    await settle()

    const waits = [2000, 5000, 15000, 30000, 60000, 60000]
    for (const [index, wait] of waits.entries()) {
      await vi.advanceTimersByTimeAsync(wait - 1)
      expect(sendItem).toHaveBeenCalledTimes(index + 1)
      await vi.advanceTimersByTimeAsync(1)
      expect(sendItem).toHaveBeenCalledTimes(index + 2)
    }
    expect(snapshotIds(sender)).toEqual([])
  })

  it('starts again at 2 seconds after a success', async () => {
    seedWaiting('a', 'b')
    const sendItem = vi
      .fn<SendItem>()
      .mockRejectedValueOnce(testApiError(null))
      .mockResolvedValueOnce(undefined)
      .mockRejectedValueOnce(testApiError(null))
      .mockResolvedValue(undefined)
    const { sender } = newSender({ sendItem })

    void sender.run()
    await settle()
    await vi.advanceTimersByTimeAsync(2000)
    expect(sentIds(sendItem)).toEqual(['a', 'a', 'b'])

    await vi.advanceTimersByTimeAsync(1999)
    expect(sentIds(sendItem)).toEqual(['a', 'a', 'b'])
    await vi.advanceTimersByTimeAsync(1)
    expect(sentIds(sendItem)).toEqual(['a', 'a', 'b', 'b'])
  })

  it('keeps the order: items behind the one being retried are not sent meanwhile', async () => {
    seedWaiting('a', 'b', 'c')
    const sendItem = vi
      .fn<SendItem>()
      .mockRejectedValueOnce(testApiError(503))
      .mockResolvedValue(undefined)
    const { sender } = newSender({ sendItem })

    void sender.run()
    await settle()
    await vi.advanceTimersByTimeAsync(1999)

    expect(sentIds(sendItem)).toEqual(['a'])
    expect(snapshotIds(sender)).toEqual(['a', 'b', 'c'])
    await vi.advanceTimersByTimeAsync(1)
    expect(sentIds(sendItem)).toEqual(['a', 'a', 'b', 'c'])
  })

  it('does not call afterSent for an item that has not been sent', async () => {
    seedWaiting('a')
    const sendItem = vi.fn<SendItem>().mockRejectedValue(testApiError(null))
    const { sender, afterSent } = newSender({ sendItem })

    void sender.run()
    await settle()
    await vi.advanceTimersByTimeAsync(2000)

    expect(afterSent).not.toHaveBeenCalled()
  })

  it('does not try again on its own while paused', async () => {
    seedWaiting('a')
    const sendItem = vi
      .fn<SendItem>()
      .mockRejectedValueOnce(testApiError(null))
      .mockResolvedValue(undefined)
    const { sender } = newSender({ sendItem })
    void sender.run()
    await settle()

    sender.pause()
    await vi.advanceTimersByTimeAsync(120000)

    expect(sentIds(sendItem)).toEqual(['a'])
  })

  it('goes on with the item after it was paused and resumed', async () => {
    seedWaiting('a')
    const sendItem = vi
      .fn<SendItem>()
      .mockRejectedValueOnce(testApiError(null))
      .mockResolvedValue(undefined)
    const { sender } = newSender({ sendItem })
    void sender.run()
    await settle()
    sender.pause()
    await vi.advanceTimersByTimeAsync(120000)

    sender.resume()
    void sender.run()
    await vi.advanceTimersByTimeAsync(60000)

    expect(sentIds(sendItem)).toEqual(['a', 'a'])
    expect(snapshotIds(sender)).toEqual([])
  })
})

describe('pausing when the person must sign in', () => {
  const pausingFailures: [string, number, string | null][] = [
    ['401', 401, null],
    ['403 AUTH_MUST_CHANGE_PASSWORD', 403, 'AUTH_MUST_CHANGE_PASSWORD'],
  ]

  it.each(pausingFailures)('pauses the queue on %s', async (_name, httpStatus, errorCode) => {
    seedWaiting('a', 'b')
    const sendItem = vi.fn<SendItem>().mockRejectedValue(testApiError(httpStatus, errorCode))
    const { sender } = newSender({ sendItem })

    await sender.run()

    expect(sender.isPaused()).toBe(true)
    expect(sentIds(sendItem)).toEqual(['a'])
  })

  it.each(pausingFailures)('keeps every item waiting in storage on %s', async (_name, httpStatus, errorCode) => {
    seedWaiting('a', 'b')
    const sendItem = vi.fn<SendItem>().mockRejectedValue(testApiError(httpStatus, errorCode))
    const { sender } = newSender({ sendItem })

    await sender.run()

    expect(storedItems(personId).map((item) => [item.id, item.state, item.failure])).toEqual([
      ['a', 'waiting', null],
      ['b', 'waiting', null],
    ])
    expect(sender.getSnapshot().items.map((item) => item.state)).toEqual(['waiting', 'waiting'])
  })

  it.each(pausingFailures)('sends nothing more on its own after %s', async (_name, httpStatus, errorCode) => {
    seedWaiting('a', 'b')
    const sendItem = vi.fn<SendItem>().mockRejectedValue(testApiError(httpStatus, errorCode))
    const { sender } = newSender({ sendItem })
    await sender.run()

    await vi.advanceTimersByTimeAsync(120000)
    await sender.run()

    expect(sentIds(sendItem)).toEqual(['a'])
  })

  it.each(pausingFailures)('sends the items again after resume on %s', async (_name, httpStatus, errorCode) => {
    seedWaiting('a', 'b')
    const sendItem = vi
      .fn<SendItem>()
      .mockRejectedValueOnce(testApiError(httpStatus, errorCode))
      .mockResolvedValue(undefined)
    const { sender } = newSender({ sendItem })
    await sender.run()

    sender.resume()
    void sender.run()
    await settle()

    expect(sender.isPaused()).toBe(false)
    expect(sentIds(sendItem)).toEqual(['a', 'a', 'b'])
    expect(snapshotIds(sender)).toEqual([])
  })

  it('is not paused when created', () => {
    const { sender } = newSender()

    expect(sender.isPaused()).toBe(false)
  })

  it('sends nothing while it was paused by hand', async () => {
    seedWaiting('a')
    const { sender, sendItem } = newSender()
    sender.pause()

    await sender.run()

    expect(sender.isPaused()).toBe(true)
    expect(sendItem).not.toHaveBeenCalled()
    expect(snapshotItem(sender, 'a')?.state).toBe('waiting')
  })

  it('sends again after a pause by hand is resumed', async () => {
    seedWaiting('a')
    const { sender, sendItem } = newSender()
    sender.pause()
    await sender.run()

    sender.resume()
    await sender.run()

    expect(sender.isPaused()).toBe(false)
    expect(sentIds(sendItem)).toEqual(['a'])
  })
})

describe('items the server refused', () => {
  const refusals: [number, string | null][] = [
    [400, 'GROUP_FINISHED'],
    [404, 'GROUP_NOT_FOUND'],
    [403, 'SETTLEMENT_NOT_ALLOWED'],
    [400, null],
  ]

  it.each(refusals)(
    'marks the item failed with its reason on status %i and code %s',
    async (httpStatus, errorCode) => {
      seedWaiting('a')
      const sendItem = vi.fn<SendItem>().mockRejectedValue(testApiError(httpStatus, errorCode))
      const { sender, afterSent } = newSender({ sendItem })

      await sender.run()

      const failure = { errorCode, httpStatus }
      expect(snapshotItem(sender, 'a')).toMatchObject({ state: 'failed', failure })
      expect(storedItems(personId)[0]).toMatchObject({ state: 'failed', failure })
      expect(afterSent).not.toHaveBeenCalled()
      expect(sender.isPaused()).toBe(false)
    },
  )

  it.each(refusals)(
    'still sends the next waiting item after status %i and code %s',
    async (httpStatus, errorCode) => {
      seedWaiting('a', 'b')
      const sendItem = vi
        .fn<SendItem>()
        .mockRejectedValueOnce(testApiError(httpStatus, errorCode))
        .mockResolvedValue(undefined)
      const { sender } = newSender({ sendItem })

      await sender.run()

      expect(sentIds(sendItem)).toEqual(['a', 'b'])
      expect(snapshotIds(sender)).toEqual(['a'])
      expect(storedIds(personId)).toEqual(['a'])
    },
  )

  it('goes on past several refused items in one run', async () => {
    seedWaiting('a', 'b', 'c', 'd')
    const sendItem = vi
      .fn<SendItem>()
      .mockRejectedValueOnce(testApiError(400, 'GROUP_FINISHED'))
      .mockResolvedValueOnce(undefined)
      .mockRejectedValueOnce(testApiError(404, 'GROUP_NOT_FOUND'))
      .mockResolvedValue(undefined)
    const { sender } = newSender({ sendItem })

    await sender.run()

    expect(sentIds(sendItem)).toEqual(['a', 'b', 'c', 'd'])
    expect(snapshotIds(sender)).toEqual(['a', 'c'])
  })

  it('does not send a failed item again in a later run', async () => {
    seedWaiting('a')
    const sendItem = vi.fn<SendItem>().mockRejectedValue(testApiError(400, 'GROUP_FINISHED'))
    const { sender } = newSender({ sendItem })
    await sender.run()

    await sender.run()
    await vi.advanceTimersByTimeAsync(120000)

    expect(sentIds(sendItem)).toEqual(['a'])
  })
})

describe('errors that are bugs', () => {
  it('marks the item failed with no code and no status', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    seedWaiting('a')
    const sendItem = vi.fn<SendItem>().mockRejectedValue(new TypeError('payload is undefined'))
    const { sender } = newSender({ sendItem })

    await sender.run()

    const failure = { errorCode: null, httpStatus: null }
    expect(snapshotItem(sender, 'a')).toMatchObject({ state: 'failed', failure })
    expect(storedItems(personId)[0]).toMatchObject({ state: 'failed', failure })
  })

  it('logs the error with console.error', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    seedWaiting('a')
    const bug = new TypeError('payload is undefined')
    const { sender } = newSender({ sendItem: vi.fn<SendItem>().mockRejectedValue(bug) })

    await sender.run()

    expect(consoleError.mock.calls.flat()).toContain(bug)
  })

  it('does not retry the item on its own', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    seedWaiting('a')
    const sendItem = vi.fn<SendItem>().mockRejectedValue(new Error('boom'))
    const { sender } = newSender({ sendItem })

    await sender.run()
    await vi.advanceTimersByTimeAsync(120000)

    expect(sentIds(sendItem)).toEqual(['a'])
  })

  it('does not log anything for a refusal the server explained', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    seedWaiting('a')
    const sendItem = vi.fn<SendItem>().mockRejectedValue(testApiError(400, 'GROUP_FINISHED'))
    const { sender } = newSender({ sendItem })

    await sender.run()

    expect(consoleError).not.toHaveBeenCalled()
  })
})

describe('retry', () => {
  it('sends a failed item again with the same id and removes it on success', async () => {
    seedWaiting('a')
    const sendItem = vi
      .fn<SendItem>()
      .mockRejectedValueOnce(testApiError(400, 'GROUP_FINISHED'))
      .mockResolvedValue(undefined)
    const { sender } = newSender({ sendItem })
    await sender.run()

    await sender.retry('a')
    await settle()

    expect(sentIds(sendItem)).toEqual(['a', 'a'])
    expect(snapshotIds(sender)).toEqual([])
    expect(storedIds(personId)).toEqual([])
  })

  it('clears the failure of the item and sets it back to waiting', async () => {
    seedWaiting('a')
    const secondSend = createDeferred<void>()
    const sendItem = vi
      .fn<SendItem>()
      .mockRejectedValueOnce(testApiError(400, 'GROUP_FINISHED'))
      .mockReturnValueOnce(secondSend.promise)
    const { sender } = newSender({ sendItem })
    await sender.run()

    await sender.retry('a')
    await settle()

    expect(snapshotItem(sender, 'a')).toMatchObject({ state: 'sending', failure: null })
    expect(storedItems(personId)[0].failure).toBeNull()
    secondSend.resolve()
    await settle()
  })

  it('fails the item again with the new reason when the server refuses it again', async () => {
    seedWaiting('a')
    const sendItem = vi
      .fn<SendItem>()
      .mockRejectedValueOnce(testApiError(400, 'GROUP_FINISHED'))
      .mockRejectedValueOnce(testApiError(404, 'GROUP_NOT_FOUND'))
    const { sender } = newSender({ sendItem })
    await sender.run()

    await sender.retry('a')
    await settle()

    expect(snapshotItem(sender, 'a')).toMatchObject({
      state: 'failed',
      failure: { errorCode: 'GROUP_NOT_FOUND', httpStatus: 404 },
    })
  })

  it('starts a run by itself so the retried item is sent without calling run', async () => {
    seedWaiting('a', 'b')
    const sendItem = vi
      .fn<SendItem>()
      .mockRejectedValueOnce(testApiError(400, 'GROUP_FINISHED'))
      .mockResolvedValue(undefined)
    const { sender } = newSender({ sendItem })
    await sender.run()
    expect(sentIds(sendItem)).toEqual(['a', 'b'])

    await sender.retry('a')
    await settle()

    expect(sentIds(sendItem)).toEqual(['a', 'b', 'a'])
  })
})

describe('discard', () => {
  it('removes a waiting item and says yes', async () => {
    seedWaiting('a', 'b')
    const { sender } = newSender()
    sender.pause()
    await settle()

    const discarded = await sender.discard('a')

    expect(discarded).toBe(true)
    expect(snapshotIds(sender)).toEqual(['b'])
    expect(storedIds(personId)).toEqual(['b'])
  })

  it('never sends a discarded item', async () => {
    seedWaiting('a', 'b')
    const { sender, sendItem } = newSender()
    sender.pause()
    await settle()
    await sender.discard('a')

    sender.resume()
    await sender.run()

    expect(sentIds(sendItem)).toEqual(['b'])
  })

  it('removes a failed item and says yes', async () => {
    seedWaiting('a')
    const sendItem = vi.fn<SendItem>().mockRejectedValue(testApiError(400, 'GROUP_FINISHED'))
    const { sender } = newSender({ sendItem })
    await sender.run()

    const discarded = await sender.discard('a')

    expect(discarded).toBe(true)
    expect(snapshotIds(sender)).toEqual([])
    expect(storedIds(personId)).toEqual([])
  })

  it('refuses to discard the item that is being sent and leaves it in place', async () => {
    seedWaiting('a')
    const sending = createDeferred<void>()
    const { sender } = newSender({ sendItem: vi.fn<SendItem>(() => sending.promise) })
    const running = sender.run()
    await settle()

    const discarded = await sender.discard('a')

    expect(discarded).toBe(false)
    expect(snapshotItem(sender, 'a')?.state).toBe('sending')
    expect(storedIds(personId)).toEqual(['a'])
    sending.resolve()
    await running
    expect(storedIds(personId)).toEqual([])
  })

  it('discards a waiting item that is queued behind the one being sent', async () => {
    seedWaiting('a', 'b')
    const sending = createDeferred<void>()
    const sendItem = vi.fn<SendItem>(() => sending.promise)
    const { sender } = newSender({ sendItem })
    const running = sender.run()
    await settle()

    const discarded = await sender.discard('b')

    expect(discarded).toBe(true)
    sending.resolve()
    await running
    expect(sentIds(sendItem)).toEqual(['a'])
    expect(storedIds(personId)).toEqual([])
  })

  it('tells the screen right away that the item is gone', async () => {
    seedWaiting('a')
    const { sender } = newSender()
    sender.pause()
    await settle()
    const listener = vi.fn()
    sender.subscribe(listener)

    await sender.discard('a')

    expect(listener).toHaveBeenCalled()
    expect(snapshotIds(sender)).toEqual([])
  })
})

describe('subscribing to the snapshot', () => {
  it('gives the same snapshot object while nothing has changed', async () => {
    seedWaiting('a')
    const { sender } = newSender()
    await settle()

    expect(sender.getSnapshot()).toBe(sender.getSnapshot())
  })

  it('gives a new snapshot object after a change and leaves the old one as it was', async () => {
    const { sender } = newSender()
    await settle()
    const before = sender.getSnapshot()

    await sender.enqueue(testOutboxItem('a'))

    expect(sender.getSnapshot()).not.toBe(before)
    expect(before.items).toEqual([])
  })

  it('tells a listener when an item is queued', async () => {
    const { sender } = newSender()
    await settle()
    const listener = vi.fn()
    sender.subscribe(listener)

    await sender.enqueue(testOutboxItem('a'))

    expect(listener).toHaveBeenCalled()
  })

  it('tells a listener when the items are loaded', async () => {
    seedWaiting('a')
    const { sender } = newSender()
    const listener = vi.fn()
    sender.subscribe(listener)

    await settle()

    expect(listener).toHaveBeenCalled()
    expect(snapshotIds(sender)).toEqual(['a'])
  })

  it('tells a listener about each step while the queue is sent', async () => {
    seedWaiting('a')
    const { sender } = newSender()
    await settle()
    const seenStates: (string | undefined)[] = []
    sender.subscribe(() => {
      seenStates.push(snapshotItem(sender, 'a')?.state)
    })

    await sender.run()

    expect(seenStates).toContain('sending')
    expect(seenStates.at(-1)).toBeUndefined()
  })

  it('stops telling a listener after it unsubscribed', async () => {
    const { sender } = newSender()
    await settle()
    const listener = vi.fn()
    const unsubscribe = sender.subscribe(listener)
    unsubscribe()

    await sender.enqueue(testOutboxItem('a'))

    expect(listener).not.toHaveBeenCalled()
  })

  it('keeps telling the other listeners after one unsubscribed', async () => {
    const { sender } = newSender()
    await settle()
    const stays = vi.fn()
    const leaves = vi.fn()
    sender.subscribe(stays)
    sender.subscribe(leaves)()

    await sender.enqueue(testOutboxItem('a'))

    expect(stays).toHaveBeenCalled()
    expect(leaves).not.toHaveBeenCalled()
  })
})
