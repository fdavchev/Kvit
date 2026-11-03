import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { testApiError, testOutboxItem } from '@/test/outboxTestData'
import { resetFakeOutboxStorage, seedOutbox, storedIds } from '@/test/outboxTestHelpers'
import type { OutboxItem } from './outboxItem'
import { createOutboxSender } from './outboxSender'

type SendItem = (item: OutboxItem) => Promise<unknown>

const personId = 'person-1'

beforeEach(() => {
  resetFakeOutboxStorage()
  vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] })
})

afterEach(() => {
  vi.useRealTimers()
  vi.restoreAllMocks()
})

describe('run called in the very moment the previous loop has finished', () => {
  it('sends the waiting item at once instead of leaving it to the back-off timer', async () => {
    seedOutbox(personId, [testOutboxItem('a')])
    const sendItem = vi.fn<SendItem>().mockRejectedValueOnce(testApiError(503)).mockResolvedValue(undefined)
    const sender = createOutboxSender({ personId, sendItem, afterSent: async () => undefined })
    let wasSending = false
    let hasRunAgain = false
    sender.subscribe(() => {
      const { isSending } = sender.getSnapshot()
      if (wasSending && !isSending && !hasRunAgain) {
        hasRunAgain = true
        void sender.run()
      }
      wasSending = isSending
    })

    void sender.run()
    await vi.advanceTimersByTimeAsync(0)

    expect(hasRunAgain).toBe(true)
    expect(sendItem).toHaveBeenCalledTimes(2)
    expect(sender.getSnapshot().items).toEqual([])
    expect(storedIds(personId)).toEqual([])
  })

  it('still sends only once per run call, so the extra look after a loop does not repeat sends', async () => {
    seedOutbox(personId, [testOutboxItem('a')])
    const sendItem = vi.fn<SendItem>(async () => undefined)
    const sender = createOutboxSender({ personId, sendItem, afterSent: async () => undefined })

    await sender.run()
    await vi.advanceTimersByTimeAsync(60_000)

    expect(sendItem).toHaveBeenCalledTimes(1)
  })

  it('starts nothing when run is called in that moment and no item is waiting', async () => {
    seedOutbox(personId, [testOutboxItem('a')])
    const sendItem = vi.fn<SendItem>(async () => undefined)
    const sender = createOutboxSender({ personId, sendItem, afterSent: async () => undefined })
    let wasSending = false
    sender.subscribe(() => {
      const { isSending } = sender.getSnapshot()
      if (wasSending && !isSending) {
        void sender.run()
      }
      wasSending = isSending
    })

    await sender.run()
    await vi.advanceTimersByTimeAsync(60_000)

    expect(sendItem).toHaveBeenCalledTimes(1)
  })
})
