import type { OutboxItem } from '@/core/outbox/outboxItem'
import { useOutboxItems } from '@/core/outbox/useOutbox'

export function useWaitingItems(belongsHere: (item: OutboxItem) => boolean): OutboxItem[] {
  const items = useOutboxItems((item) => item.state !== 'failed' && belongsHere(item))
  return items.toSorted((first, second) => second.createdAt.localeCompare(first.createdAt))
}
