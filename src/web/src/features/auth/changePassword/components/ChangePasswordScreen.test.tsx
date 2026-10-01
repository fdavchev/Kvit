import { fireEvent, screen, waitFor } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { endpoints } from '@/core/api/endpoints'
import { meQueryKey } from '@/core/auth/useMe'
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
import { seedMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { ChangePasswordScreen } from './ChangePasswordScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const passwords = { current: 'OldPassw0rd', new: 'NewPassw0rd' }

const rejectedAnswers = [
  ['AUTH_CURRENT_PASSWORD_WRONG', 400],
  ['AUTH_PASSWORD_UNCHANGED', 400],
  ['AUTH_PASSWORD_TOO_WEAK', 400],
] as const

const rejectedAnswersInEachLanguage = languages.flatMap((language) =>
  rejectedAnswers.map(([code, status]) => [language, code, status] as const),
)

async function renderChangePassword(
  options: { language?: Language; mustChangePassword?: boolean } = {},
) {
  return renderElementWithProviders(<ChangePasswordScreen />, routes.changePassword, {
    language: options.language ?? 'en',
    seedCache: seedMe({ ...testMe, mustChangePassword: options.mustChangePassword ?? false }),
  })
}

function fillForm(language: Language): void {
  typeInto(translated(language, 'auth.changePassword.current'), passwords.current)
  typeInto(translated(language, 'auth.changePassword.new'), passwords.new)
}

function pressSubmit(language: Language): void {
  fireEvent.click(
    screen.getByRole('button', { name: translated(language, 'auth.changePassword.submit') }),
  )
}

function shownToastTexts(): unknown[] {
  return [...vi.mocked(toast).mock.calls, ...vi.mocked(toast.success).mock.calls].map(
    ([text]) => text,
  )
}

describe('ChangePasswordScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('shows the change-password heading (%s)', async (language) => {
    await renderChangePassword({ language })

    expect(
      screen.getByRole('heading', { name: translated(language, 'auth.changePassword.title') }),
    ).toBeTruthy()
  })

  it.each(languages)('explains the reset to a person who has to change the password (%s)', async (language) => {
    await renderChangePassword({ language, mustChangePassword: true })

    expect(
      screen.getByText(translated(language, 'auth.changePassword.resetNote')),
    ).toBeTruthy()
  })

  it('has no back button for a person who has to change the password', async () => {
    await renderChangePassword({ mustChangePassword: true })

    const backName = translated('en', 'common.back')

    expect(screen.queryByRole('link', { name: backName })).toBeNull()
    expect(screen.queryByRole('button', { name: backName })).toBeNull()
  })

  it('shows no reset note to a person who chose to change the password', async () => {
    await renderChangePassword()

    expect(screen.queryByText(translated('en', 'auth.changePassword.resetNote'))).toBeNull()
  })

  it('has a back button to the settings screen for a person who chose to change the password', async () => {
    await renderChangePassword()

    const back = screen.getByRole('link', { name: translated('en', 'common.back') })

    expect(back.getAttribute('href')).toBe(routes.settings)
  })

  it.each([
    ['auth.changePassword.current', 'current-password'],
    ['auth.changePassword.new', 'new-password'],
  ])('has the "%s" password field with autofill hint %s', async (key, autoComplete) => {
    await renderChangePassword()

    const field = fieldLabelled(translated('en', key))

    expect(field.type).toBe('password')
    expect(field.getAttribute('autocomplete')).toBe(autoComplete)
  })

  it('explains the password rule under the new-password field', async () => {
    await renderChangePassword()

    const field = fieldLabelled(translated('en', 'auth.changePassword.new'))

    expect(descriptionOf(field)).toBe(translated('en', 'auth.passwordHint'))
  })

  it('shows the form with no error before anything is sent', async () => {
    await renderChangePassword()

    expect(screen.getByRole('button', { name: translated('en', 'auth.changePassword.submit') })).toBeTruthy()
    expect(screen.queryByRole('alert')).toBeNull()
  })

  it('sends the current and the new password to the change-password endpoint', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    await renderChangePassword()
    fillForm('en')

    pressSubmit('en')

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })
    expect(sentRequest(fetchMock)).toMatchObject({
      url: endpoints.changePassword,
      method: 'POST',
      body: { currentPassword: passwords.current, newPassword: passwords.new },
    })
  })

  it('disables the submit button while the request is waiting for an answer', async () => {
    stubFetchThatNeverAnswers()
    await renderChangePassword()
    fillForm('en')

    pressSubmit('en')

    await expectDisabledWhilePending(
      screen.getByRole('button', { name: translated('en', 'auth.changePassword.submit') }),
    )
  })

  it.each(languages)('shows the "password changed" toast after a successful change (%s)', async (language) => {
    stubFetch(new Response(null, { status: 204 }))
    await renderChangePassword({ language })
    fillForm(language)

    pressSubmit(language)

    await waitFor(() => {
      expect(shownToastTexts()).toContain(translated(language, 'auth.changePassword.done'))
    })
  })

  it('clears the must-change-password flag of the signed-in person after a successful change', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { queryClient } = await renderChangePassword({ mustChangePassword: true })
    fillForm('en')

    pressSubmit('en')

    await waitFor(() => {
      expect(queryClient.getQueryData(meQueryKey)).toEqual({ ...testMe, mustChangePassword: false })
    })
  })

  it('goes to the home screen, replacing the change-password entry in the history, after a successful change', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { router } = await renderChangePassword({ mustChangePassword: true })
    fillForm('en')

    pressSubmit('en')

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.dashboard)
    })
    expect(router.state.historyAction).toBe('REPLACE')
  })

  it.each(rejectedAnswersInEachLanguage)('shows the %s text for the answer %s inline', async (language, code, status) => {
    stubFetch(problemResponse(status, code))
    await renderChangePassword({ language })
    fillForm(language)

    pressSubmit(language)

    expect((await screen.findByRole('alert')).textContent).toContain(
      translated(language, `errors.${code}`),
    )
  })

  it('keeps the typed values, the screen and the flag after the server refuses', async () => {
    stubFetch(problemResponse(400, 'AUTH_CURRENT_PASSWORD_WRONG'))
    const { router, queryClient } = await renderChangePassword({ mustChangePassword: true })
    fillForm('en')

    pressSubmit('en')
    await screen.findByRole('alert')

    expect(fieldLabelled(translated('en', 'auth.changePassword.current')).value).toBe(passwords.current)
    expect(fieldLabelled(translated('en', 'auth.changePassword.new')).value).toBe(passwords.new)
    expect(router.state.location.pathname).toBe(routes.changePassword)
    expect(queryClient.getQueryData(meQueryKey)).toEqual({ ...testMe, mustChangePassword: true })
  })
})
