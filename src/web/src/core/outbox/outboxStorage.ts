import { createStore, del, get, update } from 'idb-keyval'
import { readWithinTimeLimit } from '@/core/api/readWithinTimeLimit'
import type { OutboxItem, OutboxItemChange } from './outboxItem'

export const outboxReadTimeLimitMs = 3000

const outboxStore = createStore('kvit-outbox', 'items')

export async function readOutbox(personId: string): Promise<OutboxItem[]> {
  const items = await readWithinTimeLimit(
    get<OutboxItem[]>(personId, outboxStore),
    'the outbox',
    outboxReadTimeLimitMs,
  )
  return items ?? []
}

export function addOutboxItem(personId: string, item: OutboxItem): Promise<void> {
  return update<OutboxItem[]>(
    personId,
    (items = []) => (items.some((queued) => queued.id === item.id) ? items : [...items, item]),
    outboxStore,
  )
}

export function updateOutboxItem(personId: string, id: string, change: OutboxItemChange): Promise<void> {
  return update<OutboxItem[]>(
    personId,
    (items = []) => items.map((item) => (item.id === id ? { ...item, ...change } : item)),
    outboxStore,
  )
}

export function removeOutboxItem(personId: string, id: string): Promise<void> {
  return update<OutboxItem[]>(
    personId,
    (items = []) => items.filter((item) => item.id !== id),
    outboxStore,
  )
}

export function clearOutbox(personId: string): Promise<void> {
  return del(personId, outboxStore)
}
