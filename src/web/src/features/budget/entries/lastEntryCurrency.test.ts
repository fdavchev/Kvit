import { beforeEach, describe, expect, it, vi } from 'vitest'
import { testMe } from '@/test/testMe'
import { readLastEntryCurrency, saveLastEntryCurrency } from './lastEntryCurrency'

const otherUserId = '9e8d7c6b-5a49-4382-9170-6f5e4d3c2b1a'
const storageKey = `kvit.lastEntryCurrency.${testMe.id}`

describe('the currency of the last personal entry', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.restoreAllMocks()
  })

  it('is MKD before the first entry ever', () => {
    expect(readLastEntryCurrency(testMe.id)).toBe('MKD')
  })

  it.each(['EUR', 'MKD'] as const)('is %s after an entry in %s was saved', (currency) => {
    saveLastEntryCurrency(testMe.id, currency)

    expect(readLastEntryCurrency(testMe.id)).toBe(currency)
  })

  it('is kept per person, so another person on the same browser still starts in MKD', () => {
    saveLastEntryCurrency(testMe.id, 'EUR')

    expect(readLastEntryCurrency(otherUserId)).toBe('MKD')
    expect(window.localStorage.getItem(storageKey)).toBe('EUR')
  })

  it('is MKD when the saved text is not a currency', () => {
    window.localStorage.setItem(storageKey, 'USD')

    expect(readLastEntryCurrency(testMe.id)).toBe('MKD')
  })

  it('warns and is MKD when the browser storage cannot be read', () => {
    const consoleWarn = vi.spyOn(console, 'warn').mockImplementation(() => {})
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new DOMException('storage is blocked', 'SecurityError')
    })

    expect(readLastEntryCurrency(testMe.id)).toBe('MKD')
    expect(consoleWarn).toHaveBeenCalledWith(expect.stringContaining(storageKey), expect.any(DOMException))
  })

  it('warns and does not throw when the browser storage cannot be written', () => {
    const consoleWarn = vi.spyOn(console, 'warn').mockImplementation(() => {})
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new DOMException('storage is full', 'QuotaExceededError')
    })

    expect(() => saveLastEntryCurrency(testMe.id, 'EUR')).not.toThrow()
    expect(consoleWarn).toHaveBeenCalledWith(expect.stringContaining(storageKey), expect.any(DOMException))
  })
})
