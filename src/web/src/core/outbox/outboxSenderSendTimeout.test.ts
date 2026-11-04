import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest'
import { testApiError, testOutboxItem } from '@/test/outboxTestData'
import { createDeferred, resetFakeOutboxStorage, seedOutbox, storedIds } from '@/test/outboxTestHelpers'
import type { OutboxItem } from './outboxItem'
import { createOutboxSender, outboxSendTimeoutMilliseconds } from './outboxSender'

type SendItem = (item: OutboxItem) => Promise<unknown>
type AfterSent = (item: OutboxItem) => Promise<void>

const personId = 'person-1'

function newSender(sendItem: Mock<SendItem>) {
  const afterSent = vi.fn<AfterSent>(async () => undefined)
  const sender = createOutboxSender({ personId, sendItem, afterSent })
  return { sender, afterSent }
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

function neverAnswers(): Promise<unknown> {
  return new Promise<unknown>(() => {})
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

describe('the time limit of a queued send', () => {
  it('is 30 seconds', () => {
    expect(outboxSendTimeoutMilliseconds).toBe(30_000)
  })

  it('keeps the item sending until the limit is over', async () => {
    seedWaiting('a')
    const sendItem = vi.fn<SendItem>(neverAnswers)
    const { sender } = newSender(sendItem)

    void sender.run()
    await vi.advanceTimersByTimeAsync(outboxSendTimeoutMilliseconds - 1)

    expect(stateOf(sender, 'a')).toBe('sending')
    expect(sentIds(sendItem)).toEqual(['a'])
  })

  it('puts the item back to waiting, not failed, when the limit is over', async () => {
    seedWaiting('a')
    const { sender } = newSender(vi.fn<SendItem>(neverAnswers))

    void sender.run()
    await vi.advanceTimersByTimeAsync(outboxSendTimeoutMilliseconds)

    expect(sender.getSnapshot().items).toMatchObject([{ id: 'a', state: 'waiting', failure: null }])
    expect(sender.getSnapshot().isSending).toBe(false)
    expect(storedIds(personId)).toEqual(['a'])
  })

  it('lets the person discard the item once the limit is over', async () => {
    seedWaiting('a')
    const { sender } = newSender(vi.fn<SendItem>(neverAnswers))
    void sender.run()
    await vi.advanceTimersByTimeAsync(outboxSendTimeoutMilliseconds)

    const discarded = await sender.discard('a')

    expect(discarded).toBe(true)
    expect(storedIds(personId)).toEqual([])
  })

  it('tries the item again 2 seconds after the limit, like after any answer that did not come', async () => {
    seedWaiting('a')
    const sendItem = vi.fn<SendItem>().mockImplementationOnce(neverAnswers).mockResolvedValue(undefined)
    const { sender } = newSender(sendItem)

    void sender.run()
    await vi.advanceTimersByTimeAsync(outboxSendTimeoutMilliseconds + 1999)
    expect(sentIds(sendItem)).toEqual(['a'])
    await vi.advanceTimersByTimeAsync(1)

    expect(sentIds(sendItem)).toEqual(['a', 'a'])
    expect(sender.getSnapshot().items).toEqual([])
    expect(storedIds(personId)).toEqual([])
  })

  it('waits 5 seconds after a second send that did not answer within the limit either', async () => {
    seedWaiting('a')
    const sendItem = vi
      .fn<SendItem>()
      .mockImplementationOnce(neverAnswers)
      .mockImplementationOnce(neverAnswers)
      .mockResolvedValue(undefined)
    const { sender } = newSender(sendItem)

    void sender.run()
    await vi.advanceTimersByTimeAsync(outboxSendTimeoutMilliseconds + 2000)
    expect(sentIds(sendItem)).toEqual(['a', 'a'])
    await vi.advanceTimersByTimeAsync(outboxSendTimeoutMilliseconds + 4999)
    expect(sentIds(sendItem)).toEqual(['a', 'a'])
    await vi.advanceTimersByTimeAsync(1)

    expect(sentIds(sendItem)).toEqual(['a', 'a', 'a'])
  })

  it('sends the items behind the stuck one only after it was sent in the end, keeping the order', async () => {
    seedWaiting('a', 'b')
    const sendItem = vi.fn<SendItem>().mockImplementationOnce(neverAnswers).mockResolvedValue(undefined)
    const { sender } = newSender(sendItem)

    void sender.run()
    await vi.advanceTimersByTimeAsync(outboxSendTimeoutMilliseconds)
    expect(sentIds(sendItem)).toEqual(['a'])
    await vi.advanceTimersByTimeAsync(2000)

    expect(sentIds(sendItem)).toEqual(['a', 'a', 'b'])
    expect(sender.getSnapshot().items).toEqual([])
  })

  it('lets a new run send after the limit, so one stuck request no longer blocks the queue', async () => {
    seedWaiting('a', 'b')
    const sendItem = vi.fn<SendItem>().mockImplementationOnce(neverAnswers).mockResolvedValue(undefined)
    const { sender } = newSender(sendItem)
    void sender.run()
    await vi.advanceTimersByTimeAsync(outboxSendTimeoutMilliseconds)

    void sender.run()
    await vi.advanceTimersByTimeAsync(100)

    expect(sentIds(sendItem)).toEqual(['a', 'a', 'b'])
    expect(sender.getSnapshot().items).toEqual([])
  })

  it('does not time out an answer that comes just before the limit', async () => {
    seedWaiting('a')
    const answer = createDeferred<void>()
    const sendItem = vi.fn<SendItem>(() => answer.promise)
    const { sender, afterSent } = newSender(sendItem)
    void sender.run()
    await vi.advanceTimersByTimeAsync(outboxSendTimeoutMilliseconds - 1)

    answer.resolve()
    await settle()

    expect(sentIds(sendItem)).toEqual(['a'])
    expect(afterSent).toHaveBeenCalledTimes(1)
    expect(sender.getSnapshot().items).toEqual([])
  })

  it('leaves no timer running after an item was sent in time', async () => {
    seedWaiting('a')
    const { sender } = newSender(vi.fn<SendItem>(async () => undefined))

    await sender.run()

    expect(vi.getTimerCount()).toBe(0)
  })
})

describe('the late answer of a send that already timed out', () => {
  async function timedOutFirstSend() {
    seedWaiting('a')
    const lateAnswer = createDeferred<void>()
    const sendItem = vi.fn<SendItem>().mockImplementationOnce(() => lateAnswer.promise).mockResolvedValue(undefined)
    const rest = newSender(sendItem)
    void rest.sender.run()
    await vi.advanceTimersByTimeAsync(outboxSendTimeoutMilliseconds)
    return { ...rest, sendItem, lateAnswer }
  }

  it('does not remove the item or call afterSent when it succeeds before the next try', async () => {
    const { sender, afterSent, lateAnswer } = await timedOutFirstSend()

    lateAnswer.resolve()
    await settle()

    expect(sender.getSnapshot().items).toMatchObject([{ id: 'a', state: 'waiting' }])
    expect(storedIds(personId)).toEqual(['a'])
    expect(afterSent).not.toHaveBeenCalled()
  })

  it('does not mark the item failed when it is refused before the next try', async () => {
    const { sender, lateAnswer } = await timedOutFirstSend()

    lateAnswer.reject(testApiError(400, 'GROUP_FINISHED'))
    await settle()

    expect(sender.getSnapshot().items).toMatchObject([{ id: 'a', state: 'waiting', failure: null }])
  })

  it('does not remove the item a second time, or call afterSent again, when it succeeds after the next try', async () => {
    const { sender, sendItem, afterSent, lateAnswer } = await timedOutFirstSend()
    await vi.advanceTimersByTimeAsync(2000)
    expect(sentIds(sendItem)).toEqual(['a', 'a'])
    expect(sender.getSnapshot().items).toEqual([])
    await sender.enqueue(testOutboxItem('b'))

    lateAnswer.resolve()
    await settle()

    expect(afterSent).toHaveBeenCalledTimes(1)
    expect(sender.getSnapshot().items).toMatchObject([{ id: 'b', state: 'waiting' }])
    expect(storedIds(personId)).toEqual(['b'])
  })

  it('does not mark an item failed when its late refusal comes after the item was sent by the next try', async () => {
    const { sender, lateAnswer } = await timedOutFirstSend()
    await vi.advanceTimersByTimeAsync(2000)

    lateAnswer.reject(testApiError(400, 'GROUP_FINISHED'))
    await settle()

    expect(sender.getSnapshot().items).toEqual([])
    expect(storedIds(personId)).toEqual([])
  })
})
