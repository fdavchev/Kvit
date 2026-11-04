import { useState } from 'react'
import type { OutboxItem } from '@/core/outbox/outboxItem'

export interface OpenedWaitingItem {
  openedItem: OutboxItem | null
  open: (item: OutboxItem) => void
  close: () => void
}

export function useOpenedWaitingItem(waitingItems: readonly OutboxItem[]): OpenedWaitingItem {
  const [opened, setOpened] = useState<OutboxItem | null>(null)
  const openedItem = opened === null ? null : (waitingItems.find((item) => item.id === opened.id) ?? opened)

  return { openedItem, open: setOpened, close: () => setOpened(null) }
}
