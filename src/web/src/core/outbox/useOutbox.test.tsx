import { act, renderHook, waitFor } from '@testing-library/react'
import type { ReactNode } from 'react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  createDeferred,
  resetFakeOutboxStorage,
  seedOutbox,
} from '@/test/outboxTestHelpers'
import { testApiError, testOutboxItem } from '@/test/outboxTestData'
import type { OutboxItem } from './outboxItem'
import { createOutboxSender } from './outboxSender'
import { OutboxProvider } from './OutboxProvider'
import { useOutboxItems, useOutboxSnapshot, useOutboxSummary } from './useOutbox'

vi.mock('idb-keyval', async () => (await import('@/test/outboxTestHelpers')).fakeIdbKeyval)

type Sender = ReturnType<typeof createOutboxSender>

const personId = 'person-1'

function newSender(sendItem: () => Promise<unknown> = () => new Promise(() => {})): Sender {
  return createOutboxSender({
    personId,
    sendItem,
    afterSent: async () => undefined,
  })
}

function wrapperFor(sender: Sender): (props: { children: ReactNode }) => ReactNode {
  return ({ children }) => <OutboxProvider sender={sender}>{children}</OutboxProvider>
}

async function enqueueWhenLoaded(sender: Sender, item: OutboxItem): Promise<void> {
  await act(async () => {
    await new Promise<void>((resolve) => setTimeout(resolve, 0))
    await sender.enqueue(item)
  })
}

beforeEach(() => {
  resetFakeOutboxStorage()
})

afterEach(() => {
  vi.restoreAllMocks()
})

describe('useOutboxSnapshot', () => {
  it('starts with an empty snapshot', () => {
    const sender = newSender()

    const { result } = renderHook(() => useOutboxSnapshot(), { wrapper: wrapperFor(sender) })

    expect(result.current).toEqual({ items: [], isSending: false, justFinished: false })
  })

  it('shows the items the sender loaded from storage', async () => {
    seedOutbox(personId, [testOutboxItem('a'), testOutboxItem('b')])
    const sender = newSender()

    const { result } = renderHook(() => useOutboxSnapshot(), { wrapper: wrapperFor(sender) })

    await waitFor(() => {
      expect(result.current.items.map((item) => item.id)).toEqual(['a', 'b'])
    })
  })

  it('shows an item the moment it is queued', async () => {
    const sender = newSender()
    const { result } = renderHook(() => useOutboxSnapshot(), { wrapper: wrapperFor(sender) })

    await enqueueWhenLoaded(sender, testOutboxItem('a'))

    expect(result.current.items.map((item) => item.id)).toEqual(['a'])
  })

  it('shows that the queue is sending and then that it just finished', async () => {
    const sending = createDeferred<void>()
    const sender = newSender(() => sending.promise)
    const { result } = renderHook(() => useOutboxSnapshot(), { wrapper: wrapperFor(sender) })
    await enqueueWhenLoaded(sender, testOutboxItem('a'))

    let running: Promise<void> = Promise.resolve()
    await act(async () => {
      running = sender.run()
    })
    expect(result.current.isSending).toBe(true)
    expect(result.current.items[0].state).toBe('sending')

    await act(async () => {
      sending.resolve()
      await running
    })
    expect(result.current).toEqual({ items: [], isSending: false, justFinished: true })
  })

  it('shares one snapshot between components using the same provider', async () => {
    const sender = newSender()
    const { result } = renderHook(() => [useOutboxSnapshot(), useOutboxSnapshot()], {
      wrapper: wrapperFor(sender),
    })

    await enqueueWhenLoaded(sender, testOutboxItem('a'))

    expect(result.current[0]).toBe(result.current[1])
  })

  it('stops listening to the sender when the component goes away', async () => {
    const sender = newSender()
    const realSubscribe = sender.subscribe
    const unsubscribed = vi.fn()
    const subscribe = vi.spyOn(sender, 'subscribe').mockImplementation((listener) => {
      const stopListening = realSubscribe(listener)
      return () => {
        unsubscribed()
        stopListening()
      }
    })
    const { unmount } = renderHook(() => useOutboxSnapshot(), { wrapper: wrapperFor(sender) })
    expect(subscribe).toHaveBeenCalled()

    unmount()

    expect(unsubscribed).toHaveBeenCalledTimes(subscribe.mock.calls.length)
  })
})

describe('useOutboxItems', () => {
  async function renderWithSeededItems<Result>(hook: () => Result) {
    seedOutbox(personId, [
      testOutboxItem('expense-here', { kind: 'expense', groupId: 'group-1' }),
      testOutboxItem('expense-elsewhere', { kind: 'expense', groupId: 'group-2' }),
      testOutboxItem('payment-here', { kind: 'settlement', groupId: 'group-1' }),
      testOutboxItem('personal', { kind: 'personalEntry', groupId: null }),
    ])
    const sender = newSender()
    const rendered = renderHook(hook, { wrapper: wrapperFor(sender) })
    await waitFor(() => {
      expect(sender.getSnapshot().items).toHaveLength(4)
    })
    return { ...rendered, sender }
  }

  it('gives every item when there is no filter', async () => {
    const { result } = await renderWithSeededItems(() => useOutboxItems())

    expect(result.current.map((item) => item.id)).toEqual([
      'expense-here',
      'expense-elsewhere',
      'payment-here',
      'personal',
    ])
  })

  it('gives only the items the filter accepts, in the same order', async () => {
    const { result } = await renderWithSeededItems(() =>
      useOutboxItems((item) => item.groupId === 'group-1'),
    )

    expect(result.current.map((item) => item.id)).toEqual(['expense-here', 'payment-here'])
  })

  it('gives an empty list when the filter accepts nothing', async () => {
    const { result } = await renderWithSeededItems(() => useOutboxItems(() => false))

    expect(result.current).toEqual([])
  })

  it('uses a new filter on the next render', async () => {
    seedOutbox(personId, [
      testOutboxItem('expense', { kind: 'expense' }),
      testOutboxItem('payment', { kind: 'settlement' }),
    ])
    const sender = newSender()
    const { result, rerender } = renderHook(
      ({ kind }) => useOutboxItems((item) => item.kind === kind),
      { wrapper: wrapperFor(sender), initialProps: { kind: 'expense' } },
    )
    await waitFor(() => {
      expect(result.current.map((item) => item.id)).toEqual(['expense'])
    })

    rerender({ kind: 'settlement' })

    expect(result.current.map((item) => item.id)).toEqual(['payment'])
  })

  it('adds a newly queued item that the filter accepts', async () => {
    const { result, sender } = await renderWithSeededItems(() =>
      useOutboxItems((item) => item.groupId === 'group-1'),
    )

    await enqueueWhenLoaded(sender, testOutboxItem('new-here', { groupId: 'group-1' }))

    expect(result.current.map((item) => item.id)).toEqual(['expense-here', 'payment-here', 'new-here'])
  })
})

describe('useOutboxSummary', () => {
  it('counts nothing for an empty queue', () => {
    const sender = newSender()

    const { result } = renderHook(() => useOutboxSummary(), { wrapper: wrapperFor(sender) })

    expect(result.current).toEqual({ waiting: 0, failed: 0, total: 0, justFinished: false })
  })

  it('counts waiting and failed items and their total', async () => {
    seedOutbox(personId, [
      testOutboxItem('a'),
      testOutboxItem('b'),
      testOutboxItem('c', { state: 'failed', failure: { errorCode: 'GROUP_FINISHED', httpStatus: 400 } }),
    ])
    const sender = newSender()

    const { result } = renderHook(() => useOutboxSummary(), { wrapper: wrapperFor(sender) })

    await waitFor(() => {
      expect(result.current).toEqual({ waiting: 2, failed: 1, total: 3, justFinished: false })
    })
  })

  it('counts the item being sent as still waiting', async () => {
    const sender = newSender()
    const { result } = renderHook(() => useOutboxSummary(), { wrapper: wrapperFor(sender) })
    await enqueueWhenLoaded(sender, testOutboxItem('a'))

    await act(async () => {
      void sender.run()
    })

    expect(result.current).toEqual({ waiting: 1, failed: 0, total: 1, justFinished: false })
  })

  it('counts an item that the server refused as failed and not as waiting', async () => {
    const sender = newSender(() => Promise.reject(testApiError(404, 'GROUP_NOT_FOUND')))
    const { result } = renderHook(() => useOutboxSummary(), { wrapper: wrapperFor(sender) })
    await enqueueWhenLoaded(sender, testOutboxItem('a'))

    await act(async () => {
      await sender.run()
    })

    expect(result.current).toEqual({ waiting: 0, failed: 1, total: 1, justFinished: false })
  })

  it('says it just finished after the last waiting item was sent', async () => {
    const sender = newSender(async () => undefined)
    const { result } = renderHook(() => useOutboxSummary(), { wrapper: wrapperFor(sender) })
    await enqueueWhenLoaded(sender, testOutboxItem('a'))

    await act(async () => {
      await sender.run()
    })

    expect(result.current).toEqual({ waiting: 0, failed: 0, total: 0, justFinished: true })
  })
})

describe('outside an OutboxProvider', () => {
  beforeEach(() => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
  })

  const hooks: [string, () => unknown][] = [
    ['useOutboxSnapshot', () => useOutboxSnapshot()],
    ['useOutboxItems', () => useOutboxItems()],
    ['useOutboxSummary', () => useOutboxSummary()],
  ]

  it.each(hooks)('%s throws an error that names OutboxProvider', (_name, hook) => {
    expect(() => renderHook(hook)).toThrow('OutboxProvider')
  })
})
