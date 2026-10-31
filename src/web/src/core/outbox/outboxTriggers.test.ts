import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { resetFakeOutboxStorage } from '@/test/outboxTestHelpers'
import { createOutboxSender } from './outboxSender'
import { startOutboxTriggers } from './outboxTriggers'

vi.mock('idb-keyval', async () => (await import('@/test/outboxTestHelpers')).fakeIdbKeyval)

function newSenderWithCountedRun() {
  const sender = createOutboxSender({
    personId: 'person-1',
    sendItem: async () => undefined,
    afterSent: async () => undefined,
  })
  const run = vi.fn<() => Promise<void>>(async () => undefined)
  return { sender: { ...sender, run }, run }
}

function setVisibility(visibilityState: DocumentVisibilityState): void {
  Object.defineProperty(document, 'visibilityState', {
    configurable: true,
    get: () => visibilityState,
  })
}

function fireVisibilityChange(visibilityState: DocumentVisibilityState): void {
  setVisibility(visibilityState)
  document.dispatchEvent(new Event('visibilitychange'))
}

beforeEach(() => {
  resetFakeOutboxStorage()
  setVisibility('visible')
})

afterEach(() => {
  Reflect.deleteProperty(document, 'visibilityState')
  vi.useRealTimers()
  vi.restoreAllMocks()
})

describe('startOutboxTriggers', () => {
  it('runs the sender once right away', () => {
    const { sender, run } = newSenderWithCountedRun()

    const stop = startOutboxTriggers(sender, { document, window })

    expect(run).toHaveBeenCalledTimes(1)
    stop()
  })

  it('runs the sender each time the browser comes back online', () => {
    const { sender, run } = newSenderWithCountedRun()
    const stop = startOutboxTriggers(sender, { document, window })

    window.dispatchEvent(new Event('online'))
    window.dispatchEvent(new Event('online'))

    expect(run).toHaveBeenCalledTimes(3)
    stop()
  })

  it('runs the sender when the page becomes visible', () => {
    const { sender, run } = newSenderWithCountedRun()
    const stop = startOutboxTriggers(sender, { document, window })

    fireVisibilityChange('visible')

    expect(run).toHaveBeenCalledTimes(2)
    stop()
  })

  it('does not run the sender when the page becomes hidden', () => {
    const { sender, run } = newSenderWithCountedRun()
    const stop = startOutboxTriggers(sender, { document, window })

    fireVisibilityChange('hidden')

    expect(run).toHaveBeenCalledTimes(1)
    stop()
  })

  it('runs the sender again when the page is visible again after being hidden', () => {
    const { sender, run } = newSenderWithCountedRun()
    const stop = startOutboxTriggers(sender, { document, window })

    fireVisibilityChange('hidden')
    fireVisibilityChange('visible')

    expect(run).toHaveBeenCalledTimes(2)
    stop()
  })

  it('does not run the sender when the browser goes offline', () => {
    const { sender, run } = newSenderWithCountedRun()
    const stop = startOutboxTriggers(sender, { document, window })

    window.dispatchEvent(new Event('offline'))

    expect(run).toHaveBeenCalledTimes(1)
    stop()
  })

  it('does not run the sender on a timer of its own', () => {
    vi.useFakeTimers()
    const { sender, run } = newSenderWithCountedRun()
    const stop = startOutboxTriggers(sender, { document, window })

    vi.advanceTimersByTime(10 * 60 * 1000)

    expect(run).toHaveBeenCalledTimes(1)
    stop()
  })
})

describe('the function startOutboxTriggers gives back', () => {
  it('stops running the sender on online events', () => {
    const { sender, run } = newSenderWithCountedRun()
    const stop = startOutboxTriggers(sender, { document, window })

    stop()
    window.dispatchEvent(new Event('online'))

    expect(run).toHaveBeenCalledTimes(1)
  })

  it('stops running the sender when the page becomes visible', () => {
    const { sender, run } = newSenderWithCountedRun()
    const stop = startOutboxTriggers(sender, { document, window })

    stop()
    fireVisibilityChange('visible')

    expect(run).toHaveBeenCalledTimes(1)
  })

  it('removes the listeners it added and nothing else', () => {
    const addOnWindow = vi.spyOn(window, 'addEventListener')
    const removeOnWindow = vi.spyOn(window, 'removeEventListener')
    const addOnDocument = vi.spyOn(document, 'addEventListener')
    const removeOnDocument = vi.spyOn(document, 'removeEventListener')
    const { sender } = newSenderWithCountedRun()
    const stop = startOutboxTriggers(sender, { document, window })

    stop()

    const added = [...addOnWindow.mock.calls, ...addOnDocument.mock.calls]
    const removed = [...removeOnWindow.mock.calls, ...removeOnDocument.mock.calls]
    expect(added.length).toBeGreaterThan(0)
    expect(removed).toHaveLength(added.length)
    expect(removed.map(([type, listener]) => [type, listener])).toEqual(
      expect.arrayContaining(added.map(([type, listener]) => [type, listener])),
    )
  })

  it('leaves listeners that other code added alone', () => {
    const otherListener = vi.fn()
    window.addEventListener('online', otherListener)
    const { sender } = newSenderWithCountedRun()
    const stop = startOutboxTriggers(sender, { document, window })

    stop()
    window.dispatchEvent(new Event('online'))

    expect(otherListener).toHaveBeenCalledTimes(1)
    window.removeEventListener('online', otherListener)
  })

  it('can be started again after it was stopped', () => {
    const { sender, run } = newSenderWithCountedRun()
    startOutboxTriggers(sender, { document, window })()

    const stop = startOutboxTriggers(sender, { document, window })
    window.dispatchEvent(new Event('online'))

    expect(run).toHaveBeenCalledTimes(3)
    stop()
  })
})
