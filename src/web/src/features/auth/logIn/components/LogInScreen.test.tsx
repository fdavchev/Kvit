import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { endpoints } from '@/core/api/endpoints'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import {
  problemResponse,
  sentRequest,
  stubFetch,
  stubFetchThatNeverAnswers,
} from '@/test/apiTestHelpers'
import { expectDisabledWhilePending, fieldLabelled, typeInto } from '@/test/formTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { LogInScreen } from './LogInScreen'

const credentials = { email: 'filip@example.com', password: 'Passw0rdOk' }

const rejectedAnswers = [
  ['AUTH_INVALID_CREDENTIALS', 401],
  ['AUTH_LOCKED_OUT', 403],
  ['RATE_LIMITED', 429],
] as const

const rejectedAnswersInEachLanguage = languages.flatMap((language) =>
  rejectedAnswers.map(([code, status]) => [language, code, status] as const),
)

async function renderLogIn(language: Language = 'en') {
  return renderElementWithProviders(<LogInScreen />, routes.logIn, { language })
}

function fillForm(language: Language): void {
  typeInto(translated(language, 'auth.email'), credentials.email)
  typeInto(translated(language, 'auth.password'), credentials.password)
}

function pressSubmit(language: Language): void {
  fireEvent.click(screen.getByRole('button', { name: translated(language, 'auth.logIn.title') }))
}

describe('LogInScreen', () => {
  beforeEach(() => {
    window.localStorage.clear()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('shows the log-in heading (%s)', async (language) => {
    await renderLogIn(language)

    expect(
      screen.getByRole('heading', { name: translated(language, 'auth.logIn.title') }),
    ).toBeTruthy()
  })

  it('has a back button to the welcome screen', async () => {
    await renderLogIn()

    const back = screen.getByRole('link', { name: translated('en', 'common.back') })

    expect(back.getAttribute('href')).toBe(routes.welcome)
  })

  it('has a link to the sign-up screen', async () => {
    await renderLogIn()

    const link = screen.getByRole('link', { name: translated('en', 'auth.logIn.noAccount') })

    expect(link.getAttribute('href')).toBe(routes.signUp)
  })

  it.each(languages)('shows the note about asking Filip to reset a forgotten password (%s)', async (language) => {
    await renderLogIn(language)

    expect(screen.getByText(translated(language, 'auth.logIn.forgot'))).toBeTruthy()
  })

  it.each([
    ['auth.email', 'email', 'email'],
    ['auth.password', 'password', 'current-password'],
  ])('has the "%s" field of type %s with autofill hint %s', async (key, type, autoComplete) => {
    await renderLogIn()

    const field = fieldLabelled(translated('en', key))

    expect(field.type).toBe(type)
    expect(field.getAttribute('autocomplete')).toBe(autoComplete)
  })

  it('shows the form with no error before anything is sent', async () => {
    await renderLogIn()

    expect(screen.getByRole('button', { name: translated('en', 'auth.logIn.title') })).toBeTruthy()
    expect(screen.queryByRole('alert')).toBeNull()
  })

  it('sends the typed email and password to the log-in endpoint', async () => {
    const fetchMock = stubFetch(Response.json(testMe))
    await renderLogIn()
    fillForm('en')

    pressSubmit('en')

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })
    expect(sentRequest(fetchMock)).toMatchObject({
      url: endpoints.logIn,
      method: 'POST',
      body: { ...credentials, timeZone: expect.any(String) },
    })
  })

  it('disables the submit button while the request is waiting for an answer', async () => {
    stubFetchThatNeverAnswers()
    await renderLogIn()
    fillForm('en')

    pressSubmit('en')

    await expectDisabledWhilePending(
      screen.getByRole('button', { name: translated('en', 'auth.logIn.title') }),
    )
  })

  it('goes to the home screen, replacing the log-in entry in the history, after a successful log-in', async () => {
    stubFetch(Response.json(testMe))
    const { router } = await renderLogIn()
    fillForm('en')

    pressSubmit('en')

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.dashboard)
    })
    expect(router.state.historyAction).toBe('REPLACE')
  })

  it.each([
    ['en', 'mk'],
    ['mk', 'en'],
  ] as const)('switches the screen from %s to the saved language %s of the account that logged in', async (screenLanguage, accountLanguage) => {
    stubFetch(Response.json({ ...testMe, language: accountLanguage }))
    const { i18n } = await renderLogIn(screenLanguage)
    fillForm(screenLanguage)

    pressSubmit(screenLanguage)

    await waitFor(() => {
      expect(i18n.language).toBe(accountLanguage)
    })
  })

  it.each(rejectedAnswersInEachLanguage)('shows the %s text for the answer %s inline', async (language, code, status) => {
    stubFetch(problemResponse(status, code))
    await renderLogIn(language)
    fillForm(language)

    pressSubmit(language)

    expect((await screen.findByRole('alert')).textContent).toContain(
      translated(language, `errors.${code}`),
    )
  })

  it('keeps the typed values and the screen after the server refuses', async () => {
    stubFetch(problemResponse(401, 'AUTH_INVALID_CREDENTIALS'))
    const { router } = await renderLogIn()
    fillForm('en')

    pressSubmit('en')
    await screen.findByRole('alert')

    expect(fieldLabelled(translated('en', 'auth.email')).value).toBe(credentials.email)
    expect(fieldLabelled(translated('en', 'auth.password')).value).toBe(credentials.password)
    expect(router.state.location.pathname).toBe(routes.logIn)
  })
})
