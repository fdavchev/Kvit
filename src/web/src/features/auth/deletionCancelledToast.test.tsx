import type { QueryClient } from '@tanstack/react-query'
import { fireEvent, screen, waitFor } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { endpoints } from '@/core/api/endpoints'
import { meQueryKey } from '@/core/auth/useMe'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { Me } from '@/core/services/me/meService'
import { groupsQueryKey } from '@/features/groups/shared/groupCache'
import { problemResponse, stubFetchByPath } from '@/test/apiTestHelpers'
import { typeInto } from '@/test/formTestHelpers'
import {
  findDrawnGoogleButton,
  sendGoogleCredential,
  stubGoogleSignIn,
  testIdToken,
  type GoogleIdentityStub,
} from '@/test/googleTestHelpers'
import { testInviteToken } from '@/test/groupTestData'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { seedMe, testGuestMe, testMe } from '@/test/testMe'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { GoogleSignUpScreen } from './googleSignUp/components/GoogleSignUpScreen'
import { LogInScreen } from './logIn/components/LogInScreen'
import { MakeAccountScreen } from './makeAccount/components/MakeAccountScreen'
import { SignUpScreen } from './signUp/components/SignUpScreen'
import { WelcomeScreen } from './welcome/components/WelcomeScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const credentials = { email: 'filip@example.com', password: 'Passw0rdOk' }
const cancelledAnswer = (language: Language = 'en'): Me => ({ ...testMe, language, deletionCancelled: true })
const millisecondsToWatchForExtraToasts = 50

function showsNoDeletionCancelledToast(language: Language = 'en'): void {
  const text = translated(language, 'auth.deletionCancelled')
  expect(vi.mocked(toast.success).mock.calls.map(([shown]) => shown)).not.toContain(text)
  expect(vi.mocked(toast).mock.calls.map(([shown]) => shown)).not.toContain(text)
}

async function watchForExtraToasts(): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve, millisecondsToWatchForExtraToasts))
}

async function renderLogIn(answer: Response | Error, language: Language = 'en', routerState?: unknown) {
  const fetchMock = stubFetchByPath({ [endpoints.logIn]: answer })
  const rendered = await renderElementWithProviders(<LogInScreen />, routes.logIn, { language, routerState })
  return { ...rendered, fetchMock }
}

function logInWithEmail(language: Language = 'en'): void {
  typeInto(translated(language, 'auth.email'), credentials.email)
  typeInto(translated(language, 'auth.password'), credentials.password)
  fireEvent.click(screen.getByRole('button', { name: translated(language, 'auth.logIn.title') }))
}

async function renderLogInRefusedBecauseTheAccountUsesGoogle(
  googleAnswer: Response | Error,
  language: Language = 'en',
  routerState?: unknown,
) {
  const identity = stubGoogleSignIn()
  stubDeviceColorScheme('light')
  stubFetchByPath({
    [endpoints.logIn]: problemResponse(400, 'AUTH_USES_GOOGLE'),
    [endpoints.googleLogIn]: googleAnswer,
  })
  const rendered = await renderElementWithProviders(<LogInScreen />, routes.logIn, { language, routerState })
  logInWithEmail(language)
  const dialog = await screen.findByRole('dialog', { name: translated(language, 'auth.googleUses.title') })
  return { ...rendered, identity, dialog }
}

async function renderWelcome(googleAnswer: Response | Error, language: Language = 'en') {
  const identity = stubGoogleSignIn()
  stubDeviceColorScheme('light')
  stubFetchByPath({
    [endpoints.health]: new Response('Healthy'),
    [endpoints.googleLogIn]: googleAnswer,
  })
  const rendered = await renderElementWithProviders(<WelcomeScreen />, routes.welcome, { language })
  return { ...rendered, identity }
}

async function sendCredentialFromWelcome(identity: GoogleIdentityStub): Promise<void> {
  const button = await findDrawnGoogleButton(identity, screen.getByRole('main'))
  sendGoogleCredential(identity, button, testIdToken)
}

describe('the "Deletion cancelled" toast', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('after an email log-in', () => {
    it.each(languages)('is shown once as a success toast when the answer says the deletion was cancelled (%s)', async (language) => {
      await renderLogIn(Response.json(cancelledAnswer(language)), language)

      logInWithEmail(language)

      await waitFor(() => {
        expect(toast.success).toHaveBeenCalledWith(translated(language, 'auth.deletionCancelled'))
      })
      await watchForExtraToasts()
      expect(toast.success).toHaveBeenCalledOnce()
      expect(toast.error).not.toHaveBeenCalled()
    })

    it('still goes to the home screen, replacing the log-in entry in the history, and signs the person in', async () => {
      const { router, queryClient } = await renderLogIn(Response.json(cancelledAnswer()))

      logInWithEmail()

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.dashboard)
      })
      expect(router.state.historyAction).toBe('REPLACE')
      expect(queryClient.getQueryData(meQueryKey)).toEqual(cancelledAnswer())
      expect(toast.success).toHaveBeenCalledWith(translated('en', 'auth.deletionCancelled'))
    })

    it('still goes back to the join screen of the invite and shows the toast', async () => {
      const { router } = await renderLogIn(Response.json(cancelledAnswer()), 'en', { joinToken: testInviteToken })

      logInWithEmail()

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.join(testInviteToken))
      })
      expect(toast.success).toHaveBeenCalledWith(translated('en', 'auth.deletionCancelled'))
    })

    it('shows nothing extra when the answer says the deletion was not cancelled', async () => {
      const { router } = await renderLogIn(Response.json({ ...testMe, deletionCancelled: false }))

      logInWithEmail()

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.dashboard)
      })
      await watchForExtraToasts()
      expect(toast.success).not.toHaveBeenCalled()
      expect(toast).not.toHaveBeenCalled()
    })

    it('shows nothing extra when the answer does not carry the field at all', async () => {
      const olderAnswer = Object.fromEntries(Object.entries(testMe).filter(([name]) => name !== 'deletionCancelled'))
      const { router } = await renderLogIn(Response.json(olderAnswer))

      logInWithEmail()

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.dashboard)
      })
      await watchForExtraToasts()
      expect(toast.success).not.toHaveBeenCalled()
    })

    it('shows no toast and stays on the screen when the log-in is refused', async () => {
      const { router } = await renderLogIn(problemResponse(401, 'AUTH_INVALID_CREDENTIALS'))

      logInWithEmail()
      await screen.findByRole('alert')
      await watchForExtraToasts()

      expect(router.state.location.pathname).toBe(routes.logIn)
      showsNoDeletionCancelledToast()
    })
  })

  describe('after a Google log-in on the welcome screen', () => {
    it.each(languages)('is shown once as a success toast when the answer says the deletion was cancelled (%s)', async (language) => {
      const { identity } = await renderWelcome(Response.json(cancelledAnswer(language)), language)

      await sendCredentialFromWelcome(identity)

      await waitFor(() => {
        expect(toast.success).toHaveBeenCalledWith(translated(language, 'auth.deletionCancelled'))
      })
      await watchForExtraToasts()
      expect(toast.success).toHaveBeenCalledOnce()
      expect(toast.error).not.toHaveBeenCalled()
    })

    it('still goes to the home screen, replacing the welcome entry in the history, and signs the person in', async () => {
      const { identity, router, queryClient } = await renderWelcome(Response.json(cancelledAnswer()))

      await sendCredentialFromWelcome(identity)

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.dashboard)
      })
      expect(router.state.historyAction).toBe('REPLACE')
      expect(queryClient.getQueryData(meQueryKey)).toEqual(cancelledAnswer())
      expect(toast.success).toHaveBeenCalledWith(translated('en', 'auth.deletionCancelled'))
    })

    it('shows nothing extra when the answer says the deletion was not cancelled', async () => {
      const { identity, router } = await renderWelcome(Response.json({ ...testMe, deletionCancelled: false }))

      await sendCredentialFromWelcome(identity)

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.dashboard)
      })
      await watchForExtraToasts()
      expect(toast.success).not.toHaveBeenCalled()
      expect(toast).not.toHaveBeenCalled()
    })

    it('shows no cancelled toast when Google has no account for the person', async () => {
      const { identity, router } = await renderWelcome(problemResponse(404, 'AUTH_GOOGLE_NO_ACCOUNT'))

      await sendCredentialFromWelcome(identity)
      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.googleSignUp)
      })
      await watchForExtraToasts()

      showsNoDeletionCancelledToast()
    })

    it('shows only the error toast, no cancelled toast, when the Google log-in fails', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { identity } = await renderWelcome(problemResponse(401, 'AUTH_GOOGLE_TOKEN_INVALID'))

      await sendCredentialFromWelcome(identity)
      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledOnce()
      })

      showsNoDeletionCancelledToast()
    })
  })

  describe('after a Google log-in through the dialog of the log-in screen', () => {
    it('is shown once as a success toast when the answer says the deletion was cancelled', async () => {
      const { identity, dialog, router } = await renderLogInRefusedBecauseTheAccountUsesGoogle(
        Response.json(cancelledAnswer()),
      )
      const button = await findDrawnGoogleButton(identity, dialog)

      sendGoogleCredential(identity, button, testIdToken)

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.dashboard)
      })
      await watchForExtraToasts()
      expect(toast.success).toHaveBeenCalledOnce()
      expect(toast.success).toHaveBeenCalledWith(translated('en', 'auth.deletionCancelled'))
    })

    it('still goes back to the join screen of the invite and shows the toast', async () => {
      const { identity, dialog, router } = await renderLogInRefusedBecauseTheAccountUsesGoogle(
        Response.json(cancelledAnswer()),
        'en',
        { joinToken: testInviteToken },
      )
      const button = await findDrawnGoogleButton(identity, dialog)

      sendGoogleCredential(identity, button, testIdToken)

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.join(testInviteToken))
      })
      expect(toast.success).toHaveBeenCalledWith(translated('en', 'auth.deletionCancelled'))
    })

    it('shows nothing extra when the answer says the deletion was not cancelled', async () => {
      const { identity, dialog, router } = await renderLogInRefusedBecauseTheAccountUsesGoogle(
        Response.json({ ...testMe, deletionCancelled: false }),
      )
      const button = await findDrawnGoogleButton(identity, dialog)

      sendGoogleCredential(identity, button, testIdToken)

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.dashboard)
      })
      await watchForExtraToasts()
      expect(toast.success).not.toHaveBeenCalled()
      expect(screen.queryByRole('dialog')).toBeNull()
    })
  })

  describe('never after making an account', () => {
    const signedUpWithTheFlag = { ...testMe, deletionCancelled: true }

    it('is not shown after an email sign-up even if the answer carries the flag', async () => {
      stubFetchByPath({ [endpoints.register]: Response.json(signedUpWithTheFlag) })
      const { router } = await renderElementWithProviders(<SignUpScreen />, routes.signUp)
      typeInto(translated('en', 'auth.signUp.name'), 'Filip')
      typeInto(translated('en', 'auth.email'), credentials.email)
      typeInto(translated('en', 'auth.password'), credentials.password)

      fireEvent.click(screen.getByRole('button', { name: translated('en', 'auth.signUp.title') }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.dashboard)
      })
      await watchForExtraToasts()
      showsNoDeletionCancelledToast()
    })

    it('is not shown after a Google sign-up even if the answer carries the flag', async () => {
      stubFetchByPath({ [endpoints.googleSignUp]: Response.json(signedUpWithTheFlag) })
      const { router } = await renderElementWithProviders(<GoogleSignUpScreen />, routes.googleSignUp, {
        routerState: { idToken: testIdToken },
      })

      fireEvent.click(screen.getByRole('button', { name: translated('en', 'auth.googleSignUp.submit') }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.dashboard)
      })
      await watchForExtraToasts()
      showsNoDeletionCancelledToast()
    })

    it('is not shown after a guest turns into an account even if the answer carries the flag', async () => {
      stubGoogleSignIn()
      stubFetchByPath({
        [endpoints.upgradeGuest]: Response.json({ ...signedUpWithTheFlag, isGuest: false }),
        [endpoints.me]: Response.json({ ...signedUpWithTheFlag, isGuest: false }),
      })
      const { router } = await renderElementWithProviders(<MakeAccountScreen />, routes.makeAccount, {
        seedCache: (queryClient: QueryClient) => {
          seedMe(testGuestMe)(queryClient)
          queryClient.setQueryData(groupsQueryKey, { groups: [] })
        },
      })
      typeInto(translated('en', 'auth.email'), credentials.email)
      typeInto(translated('en', 'auth.password'), credentials.password)

      fireEvent.click(screen.getByRole('button', { name: translated('en', 'groupSettings.save') }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.settings)
      })
      await watchForExtraToasts()
      showsNoDeletionCancelledToast()
    })
  })
})
