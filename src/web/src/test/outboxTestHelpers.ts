import { vi } from 'vitest'
import type { OutboxItem } from '@/core/outbox/outboxItem'

export const fakeOutboxStore = { name: 'kvit-outbox' }

export const storedOutbox = new Map<string, OutboxItem[] | undefined>()

export const fakeIdbKeyval = {
  createStore: vi.fn(() => fakeOutboxStore),
  get: vi.fn(async (key: string) => structuredClone(storedOutbox.get(key))),
  update: vi.fn(
    async (
      key: string,
      updater: (old: OutboxItem[] | undefined) => OutboxItem[] | undefined,
    ) => {
      const updated = updater(structuredClone(storedOutbox.get(key)))
      storedOutbox.set(key, structuredClone(updated))
    },
  ),
  del: vi.fn(async (key: string) => {
    storedOutbox.delete(key)
  }),
}

export function resetFakeOutboxStorage(): void {
  storedOutbox.clear()
  fakeIdbKeyval.get.mockClear()
  fakeIdbKeyval.update.mockClear()
  fakeIdbKeyval.del.mockClear()
}

export function seedOutbox(personId: string, items: OutboxItem[]): void {
  storedOutbox.set(personId, structuredClone(items))
}

export function storedItems(personId: string): OutboxItem[] {
  return storedOutbox.get(personId) ?? []
}

export function storedIds(personId: string): string[] {
  return storedItems(personId).map((item) => item.id)
}

export interface Deferred<T> {
  promise: Promise<T>
  resolve: (value: T) => void
  reject: (error: unknown) => void
}

export function createDeferred<T>(): Deferred<T> {
  let resolve!: (value: T) => void
  let reject!: (error: unknown) => void
  const promise = new Promise<T>((resolvePromise, rejectPromise) => {
    resolve = resolvePromise
    reject = rejectPromise
  })
  return { promise, resolve, reject }
}
