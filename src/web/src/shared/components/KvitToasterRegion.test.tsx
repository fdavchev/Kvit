import { screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { resetThemeDocument, stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { KvitToaster } from './KvitToaster'

describe('the notifications region of KvitToaster', () => {
  beforeEach(() => {
    resetThemeDocument()
    stubDeviceColorScheme('light')
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('is labelled with the translated toasts.region text (%s)', async (language) => {
    await renderElementWithProviders(<KvitToaster />, '/toaster', { language })

    const region = screen.getByRole('region', { name: new RegExp(`^${translated(language, 'toasts.region')}`) })

    expect(region).toBeTruthy()
  })

  it('does not keep the English label when the language is Macedonian', async () => {
    await renderElementWithProviders(<KvitToaster />, '/toaster', { language: 'mk' })

    expect(screen.queryByRole('region', { name: /^Notifications/ })).toBeNull()
  })
})
