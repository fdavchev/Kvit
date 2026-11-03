import { act } from '@testing-library/react'
import { vi, type Mock } from 'vitest'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import type { OutboxSender, OutboxSnapshot } from '@/core/outbox/outboxSender'

export interface FakeOutbox {
  sender: OutboxSender
  retry: Mock<(id: string) => Promise<void>>
  discard: Mock<(id: string) => Promise<boolean>>
  show: (items: readonly OutboxItem[], options?: { justFinished?: boolean }) => void
}

export function createFakeOutbox(items: readonly OutboxItem[] = [], justFinished: boolean = false): FakeOutbox {
  let snapshot: OutboxSnapshot = { items, isSending: false, justFinished }
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

  const sender: OutboxSender = {
    getSnapshot: () => snapshot,
    subscribe: (listener) => {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    enqueue: vi.fn(async (_item: OutboxItem): Promise<void> => {}),
    run: vi.fn(async (): Promise<void> => {}),
    retry,
    discard,
    pause: vi.fn(),
    resume: vi.fn(),
    isPaused: () => false,
  }

  function show(shownItems: readonly OutboxItem[], options: { justFinished?: boolean } = {}): void {
    act(() => {
      publish({ items: shownItems, justFinished: options.justFinished ?? false })
    })
  }

  return { sender, retry, discard, show }
}
