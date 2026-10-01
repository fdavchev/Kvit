import { beforeEach, describe, expect, it, vi } from 'vitest'
import { readSavedTheme, saveTheme } from './savedTheme'
import { themeChoices } from './theme'

const storageKey = 'kvit.theme'

describe('readSavedTheme', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.restoreAllMocks()
  })

  it('answers system when nothing is saved', () => {
    expect(readSavedTheme()).toBe('system')
  })

  it.each(themeChoices)('answers the saved choice %s', (choice) => {
    window.localStorage.setItem(storageKey, choice)

    expect(readSavedTheme()).toBe(choice)
  })

  it('answers system when the saved text is not a valid choice', () => {
    window.localStorage.setItem(storageKey, 'purple')

    expect(readSavedTheme()).toBe('system')
  })

  it('warns and answers system when the saved choice cannot be read', () => {
    const consoleWarn = vi.spyOn(console, 'warn').mockImplementation(() => {})
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new DOMException('storage is blocked', 'SecurityError')
    })

    expect(readSavedTheme()).toBe('system')
    expect(consoleWarn).toHaveBeenCalledOnce()
    expect(consoleWarn).toHaveBeenCalledWith(
      expect.stringContaining(storageKey),
      expect.any(DOMException),
    )
  })
})

describe('saveTheme', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.restoreAllMocks()
  })

  it.each(themeChoices)('writes the choice %s under kvit.theme', (choice) => {
    saveTheme(choice)

    expect(window.localStorage.getItem(storageKey)).toBe(choice)
  })

  it('warns and does not throw when the choice cannot be saved', () => {
    const consoleWarn = vi.spyOn(console, 'warn').mockImplementation(() => {})
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new DOMException('storage is blocked', 'SecurityError')
    })

    expect(() => saveTheme('dark')).not.toThrow()
    expect(consoleWarn).toHaveBeenCalledOnce()
    expect(consoleWarn).toHaveBeenCalledWith(
      expect.stringContaining(storageKey),
      expect.any(DOMException),
    )
  })
})
