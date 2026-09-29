import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import type { i18n as I18nInstance } from 'i18next'
import { I18nextProvider } from 'react-i18next'
import { toast } from 'sonner'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createI18n } from '@/core/i18n/i18n'
import en from '@/core/i18n/locales/en.json'
import { LanguageSwitch } from './LanguageSwitch'

vi.mock('sonner', () => ({ toast: { error: vi.fn() } }))

function renderLanguageSwitch(instance: I18nInstance): void {
  render(
    <I18nextProvider i18n={instance}>
      <LanguageSwitch />
    </I18nextProvider>,
  )
}

describe('LanguageSwitch', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.restoreAllMocks()
    vi.mocked(toast.error).mockClear()
  })

  it('switches the language and shows no error', async () => {
    const instance = await createI18n('en')
    renderLanguageSwitch(instance)

    fireEvent.click(screen.getByRole('button', { name: 'МК' }))

    await waitFor(() => {
      expect(instance.language).toBe('mk')
    })
    expect(toast.error).not.toHaveBeenCalled()
  })

  it('shows a translated error toast and logs the cause when the language cannot be changed', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    const instance = await createI18n('en')
    const failure = new Error('translation file failed to load')
    vi.spyOn(instance, 'changeLanguage').mockRejectedValue(failure)
    renderLanguageSwitch(instance)

    fireEvent.click(screen.getByRole('button', { name: 'МК' }))

    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledWith(en.errors.generic)
    })
    expect(consoleError).toHaveBeenCalledWith(
      'Could not change the language',
      failure,
    )
    expect(window.localStorage.getItem('kvit.language')).toBeNull()
  })
})
