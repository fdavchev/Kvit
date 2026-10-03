import { fireEvent, screen, waitFor } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { endpoints } from '@/core/api/endpoints'
import { meQueryKey } from '@/core/auth/useMe'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import {
  problemResponse,
  requestCountTo,
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
import { SetPasswordScreen } from './SetPasswordScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const newPassword = 'Sunce2026'

const googleOnlyMe = { ...testMe, hasPassword: false }

const rejectedAnswers = [
  ['AUTH_PASSWORD_TOO_WEAK', 400],
  ['AUTH_PASSWORD_ALREADY_SET', 400],
] as const

const rejectedAnswersInEachLanguage = languages.flatMap((language) =>
  rejectedAnswers.map(([code, status]) => [language, code, status] as const),
)

async function renderSetPassword(
  options: { language?: Language; hasPassword?: boolean } = {},
) {
  return renderElementWithProviders(<SetPasswordScreen />, routes.setPassword, {
    language: options.language ?? 'en',
    seedCache: seedMe({ ...testMe, hasPassword: options.hasPassword ?? false }),
  })
}

function passwordField(language: Language = 'en'): HTMLInputElement {
  return fieldLabelled(translated(language, 'auth.changePassword.new'))
}

function fillForm(language: Language = 'en'): void {
  typeInto(translated(language, 'auth.changePassword.new'), newPassword)
}

function pressSubmit(language: Language = 'en'): void {
  fireEvent.click(
    screen.getByRole('button', { name: translated(language, 'auth.setPassword.submit') }),
  )
}

function shownSuccessToasts(): unknown[] {
  return vi.mocked(toast.success).mock.calls.map(([text]) => text)
}

describe('SetPasswordScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('shows the set-password heading (%s)', async (language) => {
    await renderSetPassword({ language })

    expect(
      screen.getByRole('heading', { name: translated(language, 'auth.setPassword.title') }),
    ).toBeTruthy()
  })

  it('has a back button to the settings screen', async () => {
    await renderSetPassword()

    const back = screen.getByRole('link', { name: translated('en', 'common.back') })

    expect(back.getAttribute('href')).toBe(routes.settings)
  })

  it('has a password field with the new-password autofill hint', async () => {
    await renderSetPassword()

    const field = passwordField()

    expect(field.type).toBe('password')
    expect(field.getAttribute('autocomplete')).toBe('new-password')
  })

  it('has no other field than the password field', async () => {
    await renderSetPassword()

    expect(document.querySelectorAll('input')).toHaveLength(1)
  })

  it('lets the person show the password with the eye button', async () => {
    await renderSetPassword()

    fireEvent.click(screen.getByRole('button', { name: translated('en', 'common.showPassword') }))

    expect(passwordField().type).toBe('text')
  })

  it('explains the password rule under the field', async () => {
    await renderSetPassword()

    expect(descriptionOf(passwordField())).toBe(translated('en', 'auth.passwordHint'))
  })

  it('shows the form with no error before anything is sent', async () => {
    await renderSetPassword()

    expect(
      screen.getByRole('button', { name: translated('en', 'auth.setPassword.submit') }),
    ).toBeTruthy()
    expect(screen.queryByRole('alert')).toBeNull()
  })

  it('sends the new password to the set-password endpoint', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    await renderSetPassword()
    fillForm()

    pressSubmit()

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })
    expect(sentRequest(fetchMock)).toMatchObject({
      url: endpoints.setPassword,
      method: 'POST',
      body: { newPassword },
    })
  })

  it('sends the request even when the field is empty, because the server does the checking', async () => {
    const fetchMock = stubFetch(problemResponse(400, 'AUTH_PASSWORD_TOO_WEAK'))
    await renderSetPassword()

    pressSubmit()

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })
    expect(sentRequest(fetchMock).body).toEqual({ newPassword: '' })
  })

  it('disables the submit button while the request is waiting for an answer', async () => {
    stubFetchThatNeverAnswers()
    await renderSetPassword()
    fillForm()

    pressSubmit()

    await expectDisabledWhilePending(
      screen.getByRole('button', { name: translated('en', 'auth.setPassword.submit') }),
    )
  })

  it('marks the signed-in person in the me cache as having a password after a successful save', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { queryClient } = await renderSetPassword()
    fillForm()

    pressSubmit()

    await waitFor(() => {
      expect(queryClient.getQueryData(meQueryKey)).toEqual({ ...testMe, hasPassword: true })
    })
  })

  it.each(languages)('shows the "password saved" toast after a successful save (%s)', async (language) => {
    stubFetch(new Response(null, { status: 204 }))
    await renderSetPassword({ language })
    fillForm(language)

    pressSubmit(language)

    await waitFor(() => {
      expect(shownSuccessToasts()).toContain(translated(language, 'auth.setPassword.done'))
    })
  })

  it('goes back to the settings screen, replacing the set-password entry in the history, after a successful save', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { router } = await renderSetPassword()
    fillForm()

    pressSubmit()

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.settings)
    })
    expect(router.state.historyAction).toBe('REPLACE')
  })

  it.each(rejectedAnswersInEachLanguage)('shows the %s text for the answer %s inline', async (language, code, status) => {
    stubFetch(problemResponse(status, code))
    await renderSetPassword({ language })
    fillForm(language)

    pressSubmit(language)

    expect((await screen.findByRole('alert')).textContent).toContain(
      translated(language, `errors.${code}`),
    )
  })

  it('keeps the typed value, the screen, the cache and shows no toast after the server refuses', async () => {
    stubFetch(problemResponse(400, 'AUTH_PASSWORD_TOO_WEAK'))
    const { router, queryClient } = await renderSetPassword()
    fillForm()

    pressSubmit()
    await screen.findByRole('alert')

    expect(passwordField().value).toBe(newPassword)
    expect(router.state.location.pathname).toBe(routes.setPassword)
    expect(queryClient.getQueryData(meQueryKey)).toEqual(googleOnlyMe)
    expect(toast.success).not.toHaveBeenCalled()
  })

  it('sends a person who already has a password to the change-password screen without calling the server', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))

    const { router } = await renderSetPassword({ hasPassword: true })

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.changePassword)
    })
    expect(requestCountTo(fetchMock, endpoints.setPassword)).toBe(0)
  })
})
