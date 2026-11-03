import { act } from '@testing-library/react'
import { vi, type Mock } from 'vitest'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import type { OutboxSender, OutboxSnapshot } from '@/core/outbox/outboxSender'

export interface FakeOutbox {
  sender: OutboxSender
  retry: Mock<(id: string) => Promise<void>>
  discard: Mock<(id: string) => Promise<boolean>>
  run: Mock<OutboxSender['run']>
  show: (items: readonly OutboxItem[], options?: { justFinished?: boolean; isSending?: boolean }) => void
}

export function createFakeOutbox(
  items: readonly OutboxItem[] = [],
  justFinished: boolean = false,
  isSending: boolean = false,
): FakeOutbox {
  let snapshot: OutboxSnapshot = { items, isSending, justFinished }
  const listeners = new Set<() => void>()

  function publish(change: Partial<OutboxSnapshot>): void {
    snapshot = { ...snapshot, ...change }
    listeners.forEach((listener) => listener())
  }

  const retry = vi.fn(async (_id: string): Promise<void> => {})
  const discard = vi.fn(async (id: string): Promise<boolean> => {
    publish({ items: snapshot.items.filter((item) => item.id !== id) })
    return true
  })

  const run = vi.fn<OutboxSender['run']>(async () => {})

  const sender: OutboxSender = {
    getSnapshot: () => snapshot,
    subscribe: (listener) => {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    enqueue: vi.fn(async (_item: OutboxItem): Promise<void> => {}),
    run,
    retry,
    discard,
    holdWhileSaving: vi.fn(),
    pause: vi.fn(),
    resume: vi.fn(),
    isPaused: () => false,
  }

  function show(shownItems: readonly OutboxItem[], options: { justFinished?: boolean; isSending?: boolean } = {}): void {
    act(() => {
      publish({
        items: shownItems,
        justFinished: options.justFinished ?? false,
        isSending: options.isSending ?? false,
      })
    })
  }

  return { sender, retry, discard, run, show }
}
