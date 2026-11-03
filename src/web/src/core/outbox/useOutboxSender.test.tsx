import { renderHook } from '@testing-library/react'
import type { ReactNode } from 'react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { resetFakeOutboxStorage } from '@/test/outboxTestHelpers'
import { createOutboxSender } from './outboxSender'
import { OutboxProvider } from './OutboxProvider'
import { useOutboxSender } from './useOutbox'

vi.mock('idb-keyval', async () => (await import('@/test/outboxTestHelpers')).fakeIdbKeyval)

function newSender() {
  return createOutboxSender({
    personId: 'person-1',
    sendItem: async () => undefined,
    afterSent: async () => undefined,
  })
}

beforeEach(() => {
  resetFakeOutboxStorage()
})

afterEach(() => {
  vi.restoreAllMocks()
})

describe('useOutboxSender', () => {
  it('gives the sender of the provider', () => {
    const sender = newSender()
    const wrapper = ({ children }: { children: ReactNode }) => (
      <OutboxProvider sender={sender}>{children}</OutboxProvider>
    )

    const { result } = renderHook(() => useOutboxSender(), { wrapper })

    expect(result.current).toBe(sender)
  })

  it('gives the sender of the nearest provider when providers are nested', () => {
    const outer = newSender()
    const inner = newSender()
    const wrapper = ({ children }: { children: ReactNode }) => (
      <OutboxProvider sender={outer}>
        <OutboxProvider sender={inner}>{children}</OutboxProvider>
      </OutboxProvider>
    )

    const { result } = renderHook(() => useOutboxSender(), { wrapper })

    expect(result.current).toBe(inner)
  })

  it('throws an error that names OutboxProvider when it is used outside one', () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})

    expect(() => renderHook(() => useOutboxSender())).toThrow('OutboxProvider')
  })
})
