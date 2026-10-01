import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { endpoints } from '@/core/api/endpoints'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import {
  problemResponse,
  sentRequest,
  stubFetch,
  stubFetchThatNeverAnswers,
} from '@/test/apiTestHelpers'
import {
  descriptionOf,
  expectDisabledWhilePending,
  fieldLabelled,
  typeInto,
} from '@/test/formTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { SignUpScreen } from './SignUpScreen'

const newAccount = { name: 'Filip', email: 'filip@example.com', password: 'Passw0rdOk' }

const rejectedAnswers = [
  ['AUTH_EMAIL_TAKEN', 409],
  ['AUTH_PASSWORD_TOO_WEAK', 400],
  ['RATE_LIMITED', 429],
] as const

const rejectedAnswersInEachLanguage = languages.flatMap((language) =>
  rejectedAnswers.map(([code, status]) => [language, code, status] as const),
)

async function renderSignUp(language: Language = 'en') {
  return renderElementWithProviders(<SignUpScreen />, routes.signUp, { language })
}

function fillForm(language: Language): void {
  typeInto(translated(language, 'auth.signUp.name'), newAccount.name)
  typeInto(translated(language, 'auth.email'), newAccount.email)
  typeInto(translated(language, 'auth.password'), newAccount.password)
}

function pressSubmit(language: Language): void {
  fireEvent.click(screen.getByRole('button', { name: translated(language, 'auth.signUp.title') }))
}

describe('SignUpScreen', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('shows the sign-up heading (%s)', async (language) => {
    await renderSignUp(language)

    expect(
      screen.getByRole('heading', { name: translated(language, 'auth.signUp.title') }),
    ).toBeTruthy()
  })

  it('has a back button to the welcome screen', async () => {
    await renderSignUp()

    const back = screen.getByRole('link', { name: translated('en', 'common.back') })

    expect(back.getAttribute('href')).toBe(routes.welcome)
  })

  it('has a link to the log-in screen', async () => {
    await renderSignUp()

    const link = screen.getByRole('link', { name: translated('en', 'welcome.haveAccount') })

    expect(link.getAttribute('href')).toBe(routes.logIn)
  })

  it.each([
    ['auth.signUp.name', 'text', 'name'],
    ['auth.email', 'email', 'email'],
    ['auth.password', 'password', 'new-password'],
  ])('has the "%s" field of type %s with autofill hint %s', async (key, type, autoComplete) => {
    await renderSignUp()

    const field = fieldLabelled(translated('en', key))

    expect(field.type).toBe(type)
    expect(field.getAttribute('autocomplete')).toBe(autoComplete)
  })

  it('explains the password rule under the password field', async () => {
    await renderSignUp()

    const field = fieldLabelled(translated('en', 'auth.password'))

    expect(descriptionOf(field)).toBe(translated('en', 'auth.passwordHint'))
  })

  it('shows the form with no error before anything is sent', async () => {
    await renderSignUp()

    expect(screen.getByRole('button', { name: translated('en', 'auth.signUp.title') })).toBeTruthy()
    expect(screen.queryByRole('alert')).toBeNull()
  })

  it.each(languages)('sends the typed values with the screen language %s to the register endpoint', async (language) => {
    const fetchMock = stubFetch(Response.json(testMe))
    await renderSignUp(language)
    fillForm(language)

    pressSubmit(language)

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })
    expect(sentRequest(fetchMock)).toMatchObject({
      url: endpoints.register,
      method: 'POST',
      body: {
        displayName: newAccount.name,
        email: newAccount.email,
        password: newAccount.password,
        timeZone: expect.any(String),
        language,
      },
    })
  })

  it('sends the request even when every field is empty, because the server does the checking', async () => {
    const fetchMock = stubFetch(problemResponse(400, 'AUTH_DISPLAY_NAME_INVALID'))
    await renderSignUp()

    pressSubmit('en')

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })
    expect(sentRequest(fetchMock).body).toMatchObject({ displayName: '', email: '', password: '' })
  })

  it('disables the submit button while the request is waiting for an answer', async () => {
    stubFetchThatNeverAnswers()
    await renderSignUp()
    fillForm('en')

    pressSubmit('en')

    await expectDisabledWhilePending(
      screen.getByRole('button', { name: translated('en', 'auth.signUp.title') }),
    )
  })

  it('goes to the home screen, replacing the sign-up entry in the history, after a successful sign-up', async () => {
    stubFetch(Response.json(testMe))
    const { router } = await renderSignUp()
    fillForm('en')

    pressSubmit('en')

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.dashboard)
    })
    expect(router.state.historyAction).toBe('REPLACE')
  })

  it.each(rejectedAnswersInEachLanguage)('shows the %s text for the answer %s inline', async (language, code, status) => {
    stubFetch(problemResponse(status, code))
    await renderSignUp(language)
    fillForm(language)

    pressSubmit(language)

    expect((await screen.findByRole('alert')).textContent).toContain(
      translated(language, `errors.${code}`),
    )
  })

  it.each(languages)('shows the network text inline when the server cannot be reached (%s)', async (language) => {
    stubFetch(new TypeError('Failed to fetch'))
    await renderSignUp(language)
    fillForm(language)

    pressSubmit(language)

    expect((await screen.findByRole('alert')).textContent).toContain(
      translated(language, 'errors.network'),
    )
  })

  it('keeps the typed values in the fields after the server refuses', async () => {
    stubFetch(problemResponse(409, 'AUTH_EMAIL_TAKEN'))
    const { router } = await renderSignUp()
    fillForm('en')

    pressSubmit('en')
    await screen.findByRole('alert')

    expect(fieldLabelled(translated('en', 'auth.signUp.name')).value).toBe(newAccount.name)
    expect(fieldLabelled(translated('en', 'auth.email')).value).toBe(newAccount.email)
    expect(fieldLabelled(translated('en', 'auth.password')).value).toBe(newAccount.password)
    expect(router.state.location.pathname).toBe(routes.signUp)
  })
})
