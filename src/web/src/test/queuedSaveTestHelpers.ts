import { act } from '@testing-library/react'
import { vi, type MockInstance } from 'vitest'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import type { OutboxSender } from '@/core/outbox/outboxSender'
import { outboxQueueAfterMilliseconds } from '@/core/outbox/saveOrQueue'
import { storedItems } from './outboxTestHelpers'
import { testMe } from './testMe'

export function readSavedOutbox(personId: string = testMe.id): OutboxItem[] {
  return storedItems(personId)
}

export function holdOutboxSender(sender: OutboxSender): MockInstance<() => Promise<void>> {
  return vi.spyOn(sender, 'run').mockResolvedValue(undefined)
}

export function installFakeTimers(frozenNow?: Date): void {
  if (frozenNow === undefined) {
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] })
    return
  }
  vi.useFakeTimers({ toFake: ['Date', 'setTimeout', 'clearTimeout'] })
  vi.setSystemTime(frozenNow)
}

export async function letTimeGoBy(milliseconds: number): Promise<void> {
  await act(async () => {
    await vi.advanceTimersByTimeAsync(milliseconds)
  })
}

export async function letQueueTimeGoBy(): Promise<void> {
  await letTimeGoBy(outboxQueueAfterMilliseconds)
}
