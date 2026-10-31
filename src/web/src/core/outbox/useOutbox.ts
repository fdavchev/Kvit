import { createContext, useContext, useSyncExternalStore } from 'react'
import type { OutboxItem } from './outboxItem'
import type { OutboxSender, OutboxSnapshot } from './outboxSender'

export interface OutboxSummary {
  waiting: number
  failed: number
  total: number
  justFinished: boolean
}

export const OutboxContext = createContext<OutboxSender | null>(null)

function useOutboxSender(): OutboxSender {
  const sender = useContext(OutboxContext)
  if (sender === null) {
    throw new Error('The outbox hooks were used outside an OutboxProvider; wrap the signed-in screens in OutboxProvider')
  }
  return sender
}

export function useOutboxSnapshot(): OutboxSnapshot {
  const sender = useOutboxSender()
  return useSyncExternalStore(sender.subscribe, sender.getSnapshot)
}

export function useOutboxItems(filter?: (item: OutboxItem) => boolean): readonly OutboxItem[] {
  const { items } = useOutboxSnapshot()
  return filter === undefined ? items : items.filter(filter)
}

export function useOutboxSummary(): OutboxSummary {
  const { items, justFinished } = useOutboxSnapshot()
  const failed = items.filter((item) => item.state === 'failed').length
  return { waiting: items.length - failed, failed, total: items.length, justFinished }
}
