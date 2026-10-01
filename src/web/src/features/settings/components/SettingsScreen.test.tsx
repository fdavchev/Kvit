import { fireEvent, screen, waitFor } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { endpoints } from '@/core/api/endpoints'
import { meQueryKey } from '@/core/auth/useMe'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import { themeChoices, type ThemeChoice } from '@/core/theme/theme'
import { sentRequest, stubFetch } from '@/test/apiTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { seedMe, testMe } from '@/test/testMe'
import { appliedTheme, resetThemeDocument, themeButtonName } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { SettingsScreen } from './SettingsScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

async function renderSettings(language: Language = 'en') {
  return renderElementWithProviders(<SettingsScreen />, routes.settings, {
    language,
    seedCache: seedMe({ ...testMe, language }),
  })
}

function pressLanguage(language: Language): void {
  fireEvent.click(screen.getByRole('button', { name: translated('en', `language.${language}`) }))
}

function themeButton(choice: ThemeChoice): HTMLElement {
  return screen.getByRole('button', { name: themeButtonName('en', choice) })
}

function pressTheme(choice: ThemeChoice): void {
  fireEvent.click(themeButton(choice))
}

function pressLogOut(): void {
  fireEvent.click(screen.getByRole('button', { name: translated('en', 'settings.logOut') }))
}

describe('SettingsScreen', () => {
  beforeEach(() => {
    window.localStorage.clear()
    resetThemeDocument()
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('shows the settings heading (%s)', async (language) => {
    await renderSettings(language)

    expect(
      screen.getByRole('heading', { name: translated(language, 'settings.title') }),
    ).toBeTruthy()
  })

  it('has a back button to the home screen', async () => {
    await renderSettings()

    const back = screen.getByRole('link', { name: translated('en', 'common.back') })

    expect(back.getAttribute('href')).toBe(routes.dashboard)
  })

  it('has a link to the change-password screen', async () => {
    await renderSettings()

    const link = screen.getByRole('link', { name: translated('en', 'auth.changePassword.title') })

    expect(link.getAttribute('href')).toBe(routes.changePassword)
  })

  it.each(languages)('shows the language of the screen as the pressed one (%s)', async (language) => {
    await renderSettings(language)

    expect(
      screen.getByRole('button', { name: translated('en', `language.${language}`) })
        .getAttribute('aria-pressed'),
    ).toBe('true')
  })

  it('asks the server to save the new language when the other language is pressed', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    await renderSettings()

    pressLanguage('mk')

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })
    expect(sentRequest(fetchMock)).toMatchObject({
      url: endpoints.meLanguage,
      method: 'PUT',
      body: { language: 'mk' },
    })
  })

  it('switches the screen language once the server has saved it', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { i18n } = await renderSettings()

    pressLanguage('mk')

    await waitFor(() => {
      expect(i18n.language).toBe('mk')
    })
    expect(toast.error).not.toHaveBeenCalled()
  })

  it('shows the generic error toast when the server does not save the language', async () => {
    stubFetch(new Response(null, { status: 500 }))
    await renderSettings()

    pressLanguage('mk')

    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.generic'))
    })
  })

  it('keeps the screen language when the server does not save the language', async () => {
    stubFetch(new Response(null, { status: 500 }))
    const { i18n } = await renderSettings()

    pressLanguage('mk')
    await waitFor(() => {
      expect(toast.error).toHaveBeenCalled()
    })

    expect(i18n.language).toBe('en')
    expect(window.localStorage.getItem('kvit.language')).toBeNull()
  })

  it('shows Same as device as the pressed theme when nothing is saved', async () => {
    await renderSettings()

    expect(themeButton('system').getAttribute('aria-pressed')).toBe('true')
  })

  it.each(['light', 'dark'] as const)('shows the saved theme %s as the pressed one', async (saved) => {
    window.localStorage.setItem('kvit.theme', saved)

    await renderSettings()

    expect(themeButton(saved).getAttribute('aria-pressed')).toBe('true')
    expect(themeButton('system').getAttribute('aria-pressed')).toBe('false')
  })

  it.each(['light', 'dark'] as const)('saves the theme %s on this device when it is pressed', async (choice) => {
    await renderSettings()

    pressTheme(choice)

    expect(window.localStorage.getItem('kvit.theme')).toBe(choice)
  })

  it.each(['light', 'dark'] as const)('applies the theme %s to the page when it is pressed', async (choice) => {
    await renderSettings()

    pressTheme(choice)

    expect(appliedTheme()).toBe(choice)
  })

  it.each(themeChoices)('shows only %s as pressed after it is pressed', async (choice) => {
    window.localStorage.setItem('kvit.theme', choice === 'dark' ? 'light' : 'dark')
    await renderSettings()

    pressTheme(choice)

    for (const other of themeChoices) {
      expect(themeButton(other).getAttribute('aria-pressed')).toBe(String(other === choice))
    }
  })

  it('removes the theme from the page when Same as device is pressed after Dark', async () => {
    await renderSettings()
    pressTheme('dark')

    pressTheme('system')

    expect(appliedTheme()).toBeUndefined()
  })

  it.each(themeChoices)('sends no request to the server when the theme %s is pressed', async (choice) => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    await renderSettings()

    pressTheme(choice === 'dark' ? 'light' : 'dark')
    pressTheme(choice)

    await waitFor(() => {
      expect(themeButton(choice).getAttribute('aria-pressed')).toBe('true')
    })
    expect(fetchMock).not.toHaveBeenCalled()
    expect(toast.error).not.toHaveBeenCalled()
  })

  it('asks the server to log out when Log out is pressed', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    await renderSettings()

    pressLogOut()

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })
    expect(sentRequest(fetchMock)).toMatchObject({ url: endpoints.logOut, method: 'POST' })
  })

  it('shows an error toast when the log-out fails', async () => {
    stubFetch(new Response(null, { status: 500 }))
    await renderSettings()

    pressLogOut()

    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.generic'))
    })
  })

  it('keeps the person signed in and on the settings screen when the log-out fails', async () => {
    stubFetch(new Response(null, { status: 500 }))
    const { router, queryClient } = await renderSettings()

    pressLogOut()
    await waitFor(() => {
      expect(toast.error).toHaveBeenCalled()
    })

    expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
    expect(router.state.location.pathname).toBe(routes.settings)
  })
})
