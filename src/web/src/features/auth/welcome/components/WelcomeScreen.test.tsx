import { fireEvent, screen, waitFor } from '@testing-library/react'
import type { i18n as I18nInstance } from 'i18next'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { routes } from '@/core/router/routes'
import { stubFetch } from '@/test/apiTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { appliedTheme, resetThemeDocument, stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { WelcomeScreen } from './WelcomeScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

async function renderWelcome(options: { prepareI18n?: (i18n: I18nInstance) => void } = {}) {
  stubFetch(new Response('Healthy'))
  return renderElementWithProviders(<WelcomeScreen />, routes.welcome, options)
}

describe('WelcomeScreen', () => {
  beforeEach(() => {
    window.localStorage.clear()
    resetThemeDocument()
    stubDeviceColorScheme('light')
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('shows the coming-soon toast and stays on the screen when Continue with Google is pressed', async () => {
    const { router } = await renderWelcome()

    fireEvent.click(
      screen.getByRole('button', { name: translated('en', 'welcome.continueWithGoogle') }),
    )

    expect(toast).toHaveBeenCalledWith(translated('en', 'common.comingSoon'))
    expect(router.state.location.pathname).toBe(routes.welcome)
  })

  it.each([
    ['welcome.signUpWithEmail', routes.signUp],
    ['welcome.haveAccount', routes.logIn],
  ])('goes to the screen for the "%s" action', async (key, path) => {
    const { router } = await renderWelcome()

    fireEvent.click(screen.getByText(translated('en', key)))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(path)
    })
  })

  it('switches the screen language when the other language is pressed, with no error toast', async () => {
    const { i18n } = await renderWelcome()

    fireEvent.click(screen.getByRole('button', { name: translated('en', 'language.mk') }))

    await waitFor(() => {
      expect(i18n.language).toBe('mk')
    })
    expect(toast.error).not.toHaveBeenCalled()
  })

  it('shows one theme button in the top row, right before the language switch', async () => {
    await renderWelcome()

    const themeButton = screen.getByRole('button', {
      name: translated('en', 'common.switchToDarkMode'),
    })
    const languageGroup = screen.getByRole('group', { name: translated('en', 'language.label') })

    expect(themeButton.nextElementSibling).toBe(languageGroup)
  })

  it('switches the page to dark when the theme button is pressed on a light device', async () => {
    await renderWelcome()

    fireEvent.click(
      screen.getByRole('button', { name: translated('en', 'common.switchToDarkMode') }),
    )

    expect(appliedTheme()).toBe('dark')
  })

  it('shows the generic error toast, logs the cause and remembers nothing when the language cannot be changed', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    const failure = new Error('translation file failed to load')
    await renderWelcome({
      prepareI18n: (i18n) => {
        vi.spyOn(i18n, 'changeLanguage').mockRejectedValue(failure)
      },
    })

    fireEvent.click(screen.getByRole('button', { name: translated('en', 'language.mk') }))

    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.generic'))
    })
    expect(consoleError).toHaveBeenCalledWith(expect.any(String), failure)
    expect(window.localStorage.getItem('kvit.language')).toBeNull()
  })
})
