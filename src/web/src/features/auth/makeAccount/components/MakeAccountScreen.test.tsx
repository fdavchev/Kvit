import type { QueryClient } from '@tanstack/react-query'
import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { endpoints } from '@/core/api/endpoints'
import { meQueryKey } from '@/core/auth/useMe'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { Me } from '@/core/services/me/meService'
import { groupsQueryKey } from '@/features/groups/shared/groupCache'
import { descriptionOf, expectDisabledWhilePending, fieldLabelled, typeInto } from '@/test/formTestHelpers'
import {
  findDrawnGoogleButton,
  sendGoogleCredential,
  stubGoogleSignIn,
  testGoogleProfile,
  testIdToken,
  type GoogleIdentityStub,
} from '@/test/googleTestHelpers'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import {
  jsonAnswer,
  networkFailureAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
  stubFetchByRequest,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { seedMe, testGuestMe, testMe } from '@/test/testMe'
import { shownToastTexts } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'
import { MakeAccountScreen } from './MakeAccountScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const typed = { email: 'marko@example.com', password: 'Passw0rdOk' }

const upgradedByEmail: Me = { ...testGuestMe, email: typed.email, hasPassword: true, isGuest: false }

const upgradedByGoogle: Me = {
  ...testGuestMe,
  email: testGoogleProfile.email,
  pictureUrl: 'https://lh3.googleusercontent.com/a/marko',
  isGuest: false,
}

const upgradePost = `POST ${endpoints.upgradeGuest}`
const googleUpgradePost = `POST ${endpoints.googleUpgradeGuest}`
const settingsPage = 'settings page'
const cachedGroups = { groups: [{ id: 'kept-group' }] }

const emailRefusals = [
  ['AUTH_EMAIL_INVALID', 400, 'email'],
  ['AUTH_PASSWORD_TOO_WEAK', 400, 'password'],
  ['AUTH_EMAIL_TAKEN', 409, 'email'],
  ['RATE_LIMITED', 429, 'none'],
] as const

const emailRefusalsInEachLanguage = languages.flatMap((language) =>
  emailRefusals.map(([code, status]) => [language, code, status] as const),
)

const googleFailures = [
  ['AUTH_GOOGLE_TOKEN_INVALID', 400],
  ['AUTH_GOOGLE_EMAIL_NOT_VERIFIED', 400],
  ['RATE_LIMITED', 429],
] as const

interface MakeAccountOptions {
  me?: Me
  language?: Language
  answers?: Record<string, AnswerFactory>
}

async function renderMakeAccount(options: MakeAccountOptions = {}) {
  const language = options.language ?? 'en'
  const identity = stubGoogleSignIn()
  const fetchMock = stubFetchByRequest({
    [upgradePost]: jsonAnswer(upgradedByEmail),
    [googleUpgradePost]: jsonAnswer(upgradedByGoogle),
    [`GET ${endpoints.me}`]: jsonAnswer(upgradedByEmail),
    ...options.answers,
  })
  const seedGuest = seedMe({ ...(options.me ?? testGuestMe), language })
  const rendered = await renderRoutesWithProviders(
    [
      { path: routes.makeAccount, element: <MakeAccountScreen /> },
      { path: routes.settings, element: <p>{settingsPage}</p> },
      { path: '*', element: <p>another page</p> },
    ],
    routes.makeAccount,
    {
      language,
      seedCache: (queryClient: QueryClient) => {
        seedGuest(queryClient)
        queryClient.setQueryData(groupsQueryKey, cachedGroups)
      },
    },
  )
  return { identity, fetchMock, ...rendered }
}

function emailField(language: Language = 'en'): HTMLInputElement {
  return fieldLabelled(translated(language, 'auth.email'))
}

function passwordField(language: Language = 'en'): HTMLInputElement {
  return fieldLabelled(translated(language, 'auth.password'))
}

function saveButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'groupSettings.save') })
}

function fillForm(language: Language = 'en'): void {
  typeInto(translated(language, 'auth.email'), typed.email)
  typeInto(translated(language, 'auth.password'), typed.password)
}

function saveWithEmail(language: Language = 'en'): void {
  fillForm(language)
  fireEvent.click(saveButton(language))
}

async function tapGoogle(identity: GoogleIdentityStub, idToken: string = testIdToken): Promise<void> {
  const button = await findDrawnGoogleButton(identity, screen.getByRole('main'))
  sendGoogleCredential(identity, button, idToken)
}

function emailTakenDialog(language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('dialog', { name: translated(language, 'auth.googleTaken.title') })
}

function stubFetchThatNeverAnswers(): ReturnType<typeof vi.fn<typeof fetch>> {
  const neverAnswering = vi.fn<typeof fetch>(() => new Promise<Response>(() => {}))
  vi.stubGlobal('fetch', neverAnswering)
  return neverAnswering
}

function waitForAnyTimers(): Promise<void> {
  return new Promise((resolve) => {
    setTimeout(resolve, 20)
  })
}

describe('MakeAccountScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.unstubAllEnvs()
  })

  describe('what it shows', () => {
    it.each(languages)('shows the title "Make an account" (%s)', async (language) => {
      await renderMakeAccount({ language })

      expect(
        screen.getByRole('heading', { level: 1, name: translated(language, 'settings.guest.makeAccount') }),
      ).toBeTruthy()
    })

    it('has a back button to Settings', async () => {
      await renderMakeAccount()

      const back = screen.getByRole('link', { name: translated('en', 'common.back') })

      expect(back.getAttribute('href')).toBe(routes.settings)
    })

    it('draws the Google button on the screen itself, not in a sheet', async () => {
      const { identity } = await renderMakeAccount()

      const button = await findDrawnGoogleButton(identity, screen.getByRole('main'))

      expect(button).toBeTruthy()
      expect(screen.queryByRole('dialog')).toBeNull()
    })

    it.each(languages)('shows "or" between the Google button and the email field (%s)', async (language) => {
      const { identity } = await renderMakeAccount({ language })
      const button = await findDrawnGoogleButton(identity, screen.getByRole('main'))

      const or = screen.getByText(translated(language, 'welcome.or'))

      expect(button.compareDocumentPosition(or)).toBe(Node.DOCUMENT_POSITION_FOLLOWING)
      expect(or.compareDocumentPosition(emailField(language))).toBe(Node.DOCUMENT_POSITION_FOLLOWING)
    })

    it.each([
      ['auth.email', 'email', 'email'],
      ['auth.password', 'password', 'new-password'],
    ])('has the "%s" field of type %s with the autofill hint %s', async (key, type, autoComplete) => {
      await renderMakeAccount()

      const field = fieldLabelled(translated('en', key))

      expect(field.type).toBe(type)
      expect(field.getAttribute('autocomplete')).toBe(autoComplete)
    })

    it.each(languages)('explains the password rule under the password field with the sign-up text (%s)', async (language) => {
      await renderMakeAccount({ language })

      expect(descriptionOf(passwordField(language))).toBe(translated(language, 'auth.passwordHint'))
    })

    it('shows the typed password when the eye is pressed', async () => {
      await renderMakeAccount()
      typeInto(translated('en', 'auth.password'), typed.password)

      fireEvent.click(screen.getByRole('button', { name: translated('en', 'common.showPassword') }))

      expect(passwordField().type).toBe('text')
      expect(passwordField().value).toBe(typed.password)
    })

    it('does not ask for a name, because the guest keeps their own', async () => {
      await renderMakeAccount()

      expect(screen.queryByLabelText(translated('en', 'auth.signUp.name'))).toBeNull()
      expect(screen.getAllByRole('textbox')).toEqual([emailField()])
    })

    it.each(languages)('says under Save that the guest keeps all groups and expenses (%s)', async (language) => {
      await renderMakeAccount({ language })

      const keeps = screen.getByText(translated(language, 'auth.makeAccount.keeps'))

      expect(saveButton(language).compareDocumentPosition(keeps)).toBe(Node.DOCUMENT_POSITION_FOLLOWING)
    })

    it('shows no error before anything is sent', async () => {
      await renderMakeAccount()

      expect(screen.queryByRole('alert')).toBeNull()
      expect(emailField().getAttribute('aria-invalid')).toBe('false')
      expect(passwordField().getAttribute('aria-invalid')).toBe('false')
    })

    it('is capped at the readable width on a desktop', async () => {
      await renderMakeAccount()

      expect(screen.getByRole('main').classList.contains('lg:max-w-[40rem]')).toBe(true)
    })
  })

  describe('making an account with an email and a password', () => {
    it('sends only the email and the password after two fields and one tap', async () => {
      const { fetchMock } = await renderMakeAccount()

      saveWithEmail()

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', endpoints.upgradeGuest)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', endpoints.upgradeGuest)[0].body).toStrictEqual(typed)
    })

    it('sends the request when the keyboard sends the form', async () => {
      const { fetchMock } = await renderMakeAccount()
      fillForm()

      fireEvent.submit(passwordField())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', endpoints.upgradeGuest)).toBe(1)
      })
    })

    it('sends the request even with empty fields, because the server does the checking', async () => {
      const { fetchMock } = await renderMakeAccount({
        answers: { [upgradePost]: problemAnswer(400, 'AUTH_EMAIL_INVALID') },
      })

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', endpoints.upgradeGuest)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', endpoints.upgradeGuest)[0].body).toStrictEqual({ email: '', password: '' })
    })

    it('puts the returned person into the me cache: same id, the email set, no longer a guest', async () => {
      const { queryClient } = await renderMakeAccount()

      saveWithEmail()

      await screen.findByText(settingsPage)
      expect(queryClient.getQueryData(meQueryKey)).toStrictEqual(upgradedByEmail)
      expect(queryClient.getQueryData<Me>(meQueryKey)?.id).toBe(testGuestMe.id)
    })

    it('keeps the saved groups, because the person did not change', async () => {
      const { queryClient } = await renderMakeAccount()

      saveWithEmail()

      await screen.findByText(settingsPage)
      expect(queryClient.getQueryData(groupsQueryKey)).toStrictEqual(cachedGroups)
    })

    it('marks the saved groups as old, so the members list shows the account on the next load', async () => {
      const { queryClient } = await renderMakeAccount()

      saveWithEmail()

      await screen.findByText(settingsPage)
      expect(queryClient.getQueryState(groupsQueryKey)?.isInvalidated).toBe(true)
    })

    it.each(languages)('shows the "Your account is ready" toast (%s)', async (language) => {
      await renderMakeAccount({ language })

      saveWithEmail(language)

      await screen.findByText(settingsPage)
      expect(shownToastTexts()).toEqual([translated(language, 'auth.makeAccount.done')])
    })

    it('goes to Settings, replacing the make-account screen in the history', async () => {
      const { router } = await renderMakeAccount()

      saveWithEmail()

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.settings)
      })
      expect(router.state.historyAction).toBe('REPLACE')
    })

    it('does not ask the server who is signed in after a success, because the answer already says it', async () => {
      const { fetchMock } = await renderMakeAccount()

      saveWithEmail()

      await screen.findByText(settingsPage)
      expect(requestCount(fetchMock, 'GET', endpoints.me)).toBe(0)
    })
  })

  describe('a double tap', () => {
    it('disables Save while the request waits for an answer', async () => {
      await renderMakeAccount()
      fillForm()
      stubFetchThatNeverAnswers()

      fireEvent.click(saveButton())

      await expectDisabledWhilePending(saveButton())
    })

    it('sends one request when Save is tapped twice at once', async () => {
      await renderMakeAccount()
      fillForm()
      const neverAnswering = stubFetchThatNeverAnswers()

      fireEvent.click(saveButton())
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(neverAnswering).toHaveBeenCalled()
      })
      await waitForAnyTimers()
      expect(neverAnswering).toHaveBeenCalledOnce()
    })

    it('sends one request when the keyboard sends the form twice at once', async () => {
      await renderMakeAccount()
      fillForm()
      const neverAnswering = stubFetchThatNeverAnswers()

      fireEvent.submit(passwordField())
      fireEvent.submit(passwordField())

      await waitFor(() => {
        expect(neverAnswering).toHaveBeenCalled()
      })
      await waitForAnyTimers()
      expect(neverAnswering).toHaveBeenCalledOnce()
    })

    it('sends no Google upgrade while the email upgrade waits for its answer', async () => {
      const { identity } = await renderMakeAccount()
      const googleButton = await findDrawnGoogleButton(identity, screen.getByRole('main'))
      fillForm()
      const neverAnswering = stubFetchThatNeverAnswers()

      fireEvent.click(saveButton())
      await waitFor(() => {
        expect(neverAnswering).toHaveBeenCalledOnce()
      })
      sendGoogleCredential(identity, googleButton, testIdToken)

      await waitForAnyTimers()
      expect(neverAnswering).toHaveBeenCalledOnce()
      expect(String(neverAnswering.mock.calls[0][0])).toBe(endpoints.upgradeGuest)
    })

    it('sends no email upgrade while the Google upgrade waits for its answer, and disables Save', async () => {
      const { identity } = await renderMakeAccount()
      const googleButton = await findDrawnGoogleButton(identity, screen.getByRole('main'))
      fillForm()
      const neverAnswering = stubFetchThatNeverAnswers()

      sendGoogleCredential(identity, googleButton, testIdToken)
      await expectDisabledWhilePending(saveButton())
      fireEvent.submit(passwordField())

      await waitForAnyTimers()
      expect(neverAnswering).toHaveBeenCalledOnce()
      expect(String(neverAnswering.mock.calls[0][0])).toBe(endpoints.googleUpgradeGuest)
    })

    it('lets the person try again after a refusal', async () => {
      const { fetchMock } = await renderMakeAccount({
        answers: { [upgradePost]: problemAnswer(400, 'AUTH_PASSWORD_TOO_WEAK') },
      })
      saveWithEmail()
      await screen.findByRole('alert')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', endpoints.upgradeGuest)).toBe(2)
      })
    })
  })

  describe('when the server refuses the email and password', () => {
    it.each(emailRefusalsInEachLanguage)('shows the %s text for %s inline (%s)', async (language, code, status) => {
      await renderMakeAccount({ language, answers: { [upgradePost]: problemAnswer(status, code) } })

      saveWithEmail(language)

      expect((await screen.findByRole('alert')).textContent).toContain(translated(language, `errors.${code}`))
    })

    it.each(emailRefusals)('marks the right field red for %s', async (code, status, field) => {
      await renderMakeAccount({ answers: { [upgradePost]: problemAnswer(status, code) } })

      saveWithEmail()
      await screen.findByRole('alert')

      expect(emailField().getAttribute('aria-invalid')).toBe(String(field === 'email'))
      expect(passwordField().getAttribute('aria-invalid')).toBe(String(field === 'password'))
    })

    it.each([
      ['AUTH_EMAIL_TAKEN', 409, 'auth.email'],
      ['AUTH_PASSWORD_TOO_WEAK', 400, 'auth.password'],
    ] as const)('takes the red edge away for %s once the person types in that field again', async (code, status, labelKey) => {
      await renderMakeAccount({ answers: { [upgradePost]: problemAnswer(status, code) } })
      saveWithEmail()
      await screen.findByRole('alert')

      typeInto(translated('en', labelKey), 'changed')

      expect(fieldLabelled(translated('en', labelKey)).getAttribute('aria-invalid')).toBe('false')
    })

    it.each(languages)('shows the network text inline when the server cannot be reached (%s)', async (language) => {
      await renderMakeAccount({ language, answers: { [upgradePost]: networkFailureAnswer() } })

      saveWithEmail(language)

      expect((await screen.findByRole('alert')).textContent).toContain(translated(language, 'errors.network'))
    })

    it('keeps the typed values, stays on the screen and keeps the person a guest', async () => {
      const { router, queryClient } = await renderMakeAccount({
        answers: { [upgradePost]: problemAnswer(409, 'AUTH_EMAIL_TAKEN') },
      })

      saveWithEmail()
      await screen.findByRole('alert')

      expect(emailField().value).toBe(typed.email)
      expect(passwordField().value).toBe(typed.password)
      expect(router.state.location.pathname).toBe(routes.makeAccount)
      expect(queryClient.getQueryData<Me>(meQueryKey)?.isGuest).toBe(true)
      expect(toast.success).not.toHaveBeenCalled()
    })
  })

  describe('when someone already made the account elsewhere (AUTH_NOT_GUEST)', () => {
    it.each(languages)('asks again who is signed in, shows the text as a toast and goes to Settings after Save (%s)', async (language) => {
      const { fetchMock, router } = await renderMakeAccount({
        language,
        answers: { [upgradePost]: problemAnswer(409, 'AUTH_NOT_GUEST') },
      })

      saveWithEmail(language)

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.settings)
      })
      expect(router.state.historyAction).toBe('REPLACE')
      expect(toast.error).toHaveBeenCalledWith(translated(language, 'errors.AUTH_NOT_GUEST'))
      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', endpoints.me)).toBe(1)
      })
    })

    it('does the same after the Google button', async () => {
      const { identity, fetchMock, router } = await renderMakeAccount({
        answers: { [googleUpgradePost]: problemAnswer(409, 'AUTH_NOT_GUEST') },
      })

      await tapGoogle(identity)

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.settings)
      })
      expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.AUTH_NOT_GUEST'))
      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', endpoints.me)).toBe(1)
      })
    })
  })

  describe('making an account with Google', () => {
    it('sends the Google token to the upgrade path after one tap, never to Google sign-in', async () => {
      const { identity, fetchMock } = await renderMakeAccount()

      await tapGoogle(identity)

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', endpoints.googleUpgradeGuest)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', endpoints.googleUpgradeGuest)[0].body).toStrictEqual({
        idToken: testIdToken,
      })
      expect(requestCount(fetchMock, 'POST', endpoints.googleLogIn)).toBe(0)
      expect(requestCount(fetchMock, 'POST', endpoints.googleSignUp)).toBe(0)
    })

    it.each(languages)('puts the person into the me cache, shows the toast and goes to Settings (%s)', async (language) => {
      const { identity, router, queryClient } = await renderMakeAccount({ language })

      await tapGoogle(identity)

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.settings)
      })
      expect(router.state.historyAction).toBe('REPLACE')
      expect(queryClient.getQueryData(meQueryKey)).toStrictEqual(upgradedByGoogle)
      expect(queryClient.getQueryData(groupsQueryKey)).toStrictEqual(cachedGroups)
      expect(shownToastTexts()).toEqual([translated(language, 'auth.makeAccount.done')])
    })

    it.each(googleFailures)('shows the %s text as a toast, stays on the screen and keeps the person a guest', async (code, status) => {
      const { identity, router, queryClient } = await renderMakeAccount({
        answers: { [googleUpgradePost]: problemAnswer(status, code) },
      })
      vi.spyOn(console, 'error').mockImplementation(() => {})

      await tapGoogle(identity)

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated('en', `errors.${code}`))
      })
      expect(router.state.location.pathname).toBe(routes.makeAccount)
      expect(queryClient.getQueryData<Me>(meQueryKey)?.isGuest).toBe(true)
    })
  })

  describe('when the Google email already has an account', () => {
    const emailTaken = { [googleUpgradePost]: problemAnswer(400, 'AUTH_GOOGLE_EMAIL_TAKEN') }

    it.each(languages)('opens the "This email already has an account." pop-up and shows no toast (%s)', async (language) => {
      const { identity, router } = await renderMakeAccount({ language, answers: emailTaken })

      await tapGoogle(identity)

      expect(await emailTakenDialog(language)).toBeTruthy()
      expect(router.state.location.pathname).toBe(routes.makeAccount)
      expect(toast.error).not.toHaveBeenCalled()
    })

    it('offers another Google account and Close, but no log-in, so the guest never leaves their groups behind', async () => {
      const { identity } = await renderMakeAccount({ answers: emailTaken })
      await tapGoogle(identity)
      const dialog = await emailTakenDialog()

      expect(within(dialog).getByText(translated('en', 'auth.googleTaken.another'))).toBeTruthy()
      expect(await findDrawnGoogleButton(identity, dialog)).toBeTruthy()
      expect(within(dialog).getByRole('button', { name: translated('en', 'common.close') })).toBeTruthy()
      expect(within(dialog).queryByRole('button', { name: translated('en', 'auth.googleTaken.logIn') })).toBeNull()
    })

    it('makes the account with the other Google account from the pop-up', async () => {
      const otherIdToken = 'other.google.token'
      const { identity, fetchMock, router } = await renderMakeAccount({
        answers: {
          [googleUpgradePost]: () => {
            const isFirst = requestCount(fetchMock, 'POST', endpoints.googleUpgradeGuest) === 1
            return isFirst ? problemAnswer(400, 'AUTH_GOOGLE_EMAIL_TAKEN')() : Response.json(upgradedByGoogle)
          },
        },
      })
      await tapGoogle(identity)
      const dialog = await emailTakenDialog()

      sendGoogleCredential(identity, await findDrawnGoogleButton(identity, dialog), otherIdToken)

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.settings)
      })
      expect(requestsOf(fetchMock, 'POST', endpoints.googleUpgradeGuest)[1].body).toStrictEqual({
        idToken: otherIdToken,
      })
    })

    it('closes the pop-up and stays on the screen when Close is pressed', async () => {
      const { identity, router } = await renderMakeAccount({ answers: emailTaken })
      await tapGoogle(identity)
      const dialog = await emailTakenDialog()

      fireEvent.click(within(dialog).getByRole('button', { name: translated('en', 'common.close') }))

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      expect(router.state.location.pathname).toBe(routes.makeAccount)
      expect(saveButton()).toBeTruthy()
    })
  })

  describe('for a person who already has an account', () => {
    it('goes to Settings at once, replacing the make-account screen, and sends nothing', async () => {
      const { router, fetchMock } = await renderMakeAccount({ me: testMe })

      await screen.findByText(settingsPage)

      expect(router.state.location.pathname).toBe(routes.settings)
      expect(router.state.historyAction).toBe('REPLACE')
      expect(fetchMock).not.toHaveBeenCalled()
    })
  })
})
