import { renderHook } from '@testing-library/react'
import type { i18n as I18nInstance } from 'i18next'
import type { ReactNode } from 'react'
import { I18nextProvider } from 'react-i18next'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createI18n } from './i18n'
import { useLanguage } from './useLanguage'

const storageKey = 'kvit.language'

async function renderUseLanguage(instance: I18nInstance) {
  function Wrapper({ children }: { children: ReactNode }) {
    return <I18nextProvider i18n={instance}>{children}</I18nextProvider>
  }
  return renderHook(() => useLanguage(), { wrapper: Wrapper })
}

describe('useLanguage', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.restoreAllMocks()
  })

  it('saves the choice only after the language has changed', async () => {
    const instance = await createI18n('en')
    const realChangeLanguage = instance.changeLanguage.bind(instance)
    let savedWhileChanging: string | null = 'not read yet'
    vi.spyOn(instance, 'changeLanguage').mockImplementation(async (language) => {
      savedWhileChanging = window.localStorage.getItem(storageKey)
      return realChangeLanguage(language)
    })
    const { result } = await renderUseLanguage(instance)

    await result.current.changeLanguage('mk')

    expect(savedWhileChanging).toBeNull()
    expect(window.localStorage.getItem(storageKey)).toBe('mk')
    expect(instance.language).toBe('mk')
  })

  it('does not save the choice when the language could not be changed, and rethrows the failure', async () => {
    const instance = await createI18n('en')
    const failure = new Error('translation file failed to load')
    vi.spyOn(instance, 'changeLanguage').mockRejectedValue(failure)
    const { result } = await renderUseLanguage(instance)

    await expect(result.current.changeLanguage('mk')).rejects.toBe(failure)

    expect(window.localStorage.getItem(storageKey)).toBeNull()
    expect(instance.language).toBe('en')
  })

  it('still changes the language when the choice cannot be remembered', async () => {
    const instance = await createI18n('en')
    const consoleWarn = vi.spyOn(console, 'warn').mockImplementation(() => {})
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new DOMException('storage is blocked', 'SecurityError')
    })
    const { result } = await renderUseLanguage(instance)

    await result.current.changeLanguage('mk')

    expect(instance.language).toBe('mk')
    expect(consoleWarn).toHaveBeenCalledWith(
      `Could not save "${storageKey}" to localStorage`,
      expect.any(DOMException),
    )
  })
})
