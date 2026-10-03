import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import type { i18n as I18nInstance } from 'i18next'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { endpoints } from '@/core/api/endpoints'
import { meQueryKey } from '@/core/auth/useMe'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import {
  problemResponse,
  requestCountTo,
  sentRequestTo,
  stubFetchByPath,
} from '@/test/apiTestHelpers'
import {
  findDrawnGoogleButton,
  sendGoogleCredential,
  stubGoogleSignIn,
  testGoogleProfile,
  testIdToken,
  type GoogleIdentityStub,
} from '@/test/googleTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { testMe } from '@/test/testMe'
import { appliedTheme, resetThemeDocument, stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { WelcomeScreen } from './WelcomeScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const removedPitchTexts: Record<Language, string> = {
  en: 'Only you need an account. Friends can be just names.',
  mk: 'Само тебе ти треба профил. Пријателите може да бидат само имиња.',
}

const otherFailures = [
  ['AUTH_GOOGLE_TOKEN_INVALID', 401],
  ['AUTH_GOOGLE_EMAIL_NOT_VERIFIED', 400],
  ['RATE_LIMITED', 429],
] as const

const otherFailuresInEachLanguage = languages.flatMap((language) =>
  otherFailures.map(([code, status]) => [language, code, status] as const),
)

interface WelcomeOptions {
  language?: Language
  prepareI18n?: (i18n: I18nInstance) => void
  googleLogIn?: Response | Error | (Response | Error)[]
}

async function renderWelcome(options: WelcomeOptions = {}) {
  const identity = stubGoogleSignIn()
  const fetchMock = stubFetchByPath({
    [endpoints.health]: new Response('Healthy'),
    [endpoints.googleLogIn]: options.googleLogIn ?? Response.json(testMe),
  })
  const rendered = await renderElementWithProviders(<WelcomeScreen />, routes.welcome, {
    language: options.language,
    prepareI18n: options.prepareI18n,
  })
  return { ...rendered, identity, fetchMock }
}

async function sendCredentialFromScreen(
  identity: GoogleIdentityStub,
  idToken: string = testIdToken,
): Promise<void> {
  const button = await findDrawnGoogleButton(identity, screen.getByRole('main'))
  sendGoogleCredential(identity, button, idToken)
}

async function openTakenDialog(identity: GoogleIdentityStub): Promise<HTMLElement> {
  await sendCredentialFromScreen(identity)
  return screen.findByRole('dialog', { name: translated('en', 'auth.googleTaken.title') })
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

  it('draws the Google button on the screen', async () => {
    const { identity } = await renderWelcome()

    const button = await findDrawnGoogleButton(identity, screen.getByRole('main'))

    expect(button).toBeTruthy()
  })

  it.each(languages)('no longer shows the pitch line (%s)', async (language) => {
    await renderWelcome({ language })

    expect(screen.queryByText(removedPitchTexts[language])).toBeNull()
  })

  it.each(languages)('shows the word "or" between the Google button and the sign-up link (%s)', async (language) => {
    const { identity } = await renderWelcome({ language })

    const googleButton = await findDrawnGoogleButton(identity, screen.getByRole('main'))
    const orWord = screen.getByText(translated(language, 'welcome.or'))
    const signUpLink = screen.getByRole('link', {
      name: translated(language, 'welcome.signUpWithEmail'),
    })

    expect(googleButton.compareDocumentPosition(orWord)).toBe(Node.DOCUMENT_POSITION_FOLLOWING)
    expect(orWord.compareDocumentPosition(signUpLink)).toBe(Node.DOCUMENT_POSITION_FOLLOWING)
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

  it('shows a Privacy link right after the "I already have an account" link', async () => {
    await renderWelcome()

    const haveAccount = screen.getByRole('link', { name: translated('en', 'welcome.haveAccount') })
    const privacy = screen.getByRole('link', { name: translated('en', 'common.privacy') })

    expect(haveAccount.compareDocumentPosition(privacy)).toBe(Node.DOCUMENT_POSITION_FOLLOWING)
  })

  it('opens the privacy screen and tells it to come back here when the Privacy link is pressed', async () => {
    const { router } = await renderWelcome()

    fireEvent.click(screen.getByRole('link', { name: translated('en', 'common.privacy') }))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.privacy)
    })
    expect(router.state.location.state).toEqual({ from: routes.welcome })
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

  describe('after Google returns a credential', () => {
    it('sends the ID token and the device time zone to the Google log-in endpoint', async () => {
      const { identity, fetchMock } = await renderWelcome()

      await sendCredentialFromScreen(identity)

      await waitFor(() => {
        expect(requestCountTo(fetchMock, endpoints.googleLogIn)).toBe(1)
      })
      expect(sentRequestTo(fetchMock, endpoints.googleLogIn)).toMatchObject({
        method: 'POST',
        body: { idToken: testIdToken, timeZone: expect.any(String) },
      })
    })

    it('puts the signed-in person into the me cache', async () => {
      const { identity, queryClient } = await renderWelcome()

      await sendCredentialFromScreen(identity)

      await waitFor(() => {
        expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
      })
    })

    it.each([
      ['en', 'mk'],
      ['mk', 'en'],
    ] as const)('switches the screen from %s to the saved language %s of the account and remembers it', async (screenLanguage, accountLanguage) => {
      const { identity, i18n } = await renderWelcome({
        language: screenLanguage,
        googleLogIn: Response.json({ ...testMe, language: accountLanguage }),
      })

      await sendCredentialFromScreen(identity)

      await waitFor(() => {
        expect(i18n.language).toBe(accountLanguage)
      })
      expect(window.localStorage.getItem('kvit.language')).toBe(accountLanguage)
    })

    it('goes to the home screen, replacing the welcome entry in the history', async () => {
      const { identity, router } = await renderWelcome()

      await sendCredentialFromScreen(identity)

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.dashboard)
      })
      expect(router.state.historyAction).toBe('REPLACE')
    })

    it('goes to the Google sign-up screen carrying the ID token when the person has no account yet', async () => {
      const { identity, router, queryClient } = await renderWelcome({
        googleLogIn: problemResponse(404, 'AUTH_GOOGLE_NO_ACCOUNT'),
      })

      await sendCredentialFromScreen(identity)

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.googleSignUp)
      })
      expect(router.state.location.state).toEqual({ idToken: testIdToken })
      expect(queryClient.getQueryData(meQueryKey)).toBeUndefined()
    })

    it.each(otherFailuresInEachLanguage)('shows the %s text for %s as an error toast and stays on the screen', async (language, code, status) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { identity, router } = await renderWelcome({
        language,
        googleLogIn: problemResponse(status, code),
      })

      await sendCredentialFromScreen(identity)

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated(language, `errors.${code}`))
      })
      expect(router.state.location.pathname).toBe(routes.welcome)
      expect(screen.queryByRole('dialog')).toBeNull()
    })

    it.each([
      ['the server cannot be reached', new TypeError('Failed to fetch'), 'errors.network'],
      ['the server fails without an error code', new Response(null, { status: 500 }), 'errors.generic'],
    ])('shows an error toast when %s', async (_name, answer, key) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { identity } = await renderWelcome({ googleLogIn: answer })

      await sendCredentialFromScreen(identity)

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated('en', key))
      })
    })

    it('logs the failure with console.error', async () => {
      const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
      const { identity } = await renderWelcome({
        googleLogIn: problemResponse(401, 'AUTH_GOOGLE_TOKEN_INVALID'),
      })

      await sendCredentialFromScreen(identity)

      await waitFor(() => {
        expect(consoleError).toHaveBeenCalledWith(expect.any(String), expect.any(ApiError))
      })
    })
  })

  describe('when the email already has an account', () => {
    const emailTaken = (): Response => problemResponse(400, 'AUTH_GOOGLE_EMAIL_TAKEN')

    it('opens a dialog named by its title and does not leave the screen', async () => {
      const { identity, router } = await renderWelcome({ googleLogIn: emailTaken() })

      const dialog = await openTakenDialog(identity)

      expect(dialog).toBeTruthy()
      expect(router.state.location.pathname).toBe(routes.welcome)
      expect(toast.error).not.toHaveBeenCalled()
    })

    it.each(languages)('shows the title, the log-in button and the close button in %s', async (language) => {
      const { identity } = await renderWelcome({ language, googleLogIn: emailTaken() })

      await sendCredentialFromScreen(identity)

      const dialog = await screen.findByRole('dialog', {
        name: translated(language, 'auth.googleTaken.title'),
      })
      expect(
        within(dialog).getByRole('button', { name: translated(language, 'auth.googleTaken.logIn') }),
      ).toBeTruthy()
      expect(
        within(dialog).getByRole('button', { name: translated(language, 'common.close') }),
      ).toBeTruthy()
    })

    it('shows a label above a second Google button inside the dialog', async () => {
      const { identity } = await renderWelcome({ googleLogIn: emailTaken() })
      const dialog = await openTakenDialog(identity)

      const label = within(dialog).getByText(translated('en', 'auth.googleTaken.another'))
      const button = await findDrawnGoogleButton(identity, dialog)

      expect(label.compareDocumentPosition(button)).toBe(Node.DOCUMENT_POSITION_FOLLOWING)
    })

    it('goes to the log-in screen with the Google email filled in when "Log in with password" is pressed', async () => {
      const { identity, router } = await renderWelcome({ googleLogIn: emailTaken() })
      const dialog = await openTakenDialog(identity)

      fireEvent.click(
        within(dialog).getByRole('button', { name: translated('en', 'auth.googleTaken.logIn') }),
      )

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.logIn)
      })
      expect(router.state.location.state).toEqual({ email: testGoogleProfile.email })
    })

    it('closes the dialog and stays on the screen when Close is pressed', async () => {
      const { identity, router } = await renderWelcome({ googleLogIn: emailTaken() })
      const dialog = await openTakenDialog(identity)

      fireEvent.click(within(dialog).getByRole('button', { name: translated('en', 'common.close') }))

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      expect(router.state.location.pathname).toBe(routes.welcome)
    })

    it('runs the same log-in flow for a credential from the Google button inside the dialog', async () => {
      const { identity, router, queryClient } = await renderWelcome({
        googleLogIn: [emailTaken(), Response.json(testMe)],
      })
      const dialog = await openTakenDialog(identity)
      const dialogButton = await findDrawnGoogleButton(identity, dialog)

      sendGoogleCredential(identity, dialogButton, testIdToken)

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.dashboard)
      })
      expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
    })
  })
})
