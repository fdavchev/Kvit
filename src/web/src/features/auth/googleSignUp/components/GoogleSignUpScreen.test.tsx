import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
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
import { testGoogleProfile, testIdToken } from '@/test/googleTestHelpers'
import { testInviteToken } from '@/test/groupTestData'
import { joinPathOf, unsafeJoinTokens } from '@/test/inviteTestData'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { GoogleSignUpScreen } from './GoogleSignUpScreen'

const rejectedAnswers = [
  ['AUTH_DISPLAY_NAME_INVALID', 400],
  ['AUTH_GOOGLE_TOKEN_INVALID', 401],
  ['AUTH_GOOGLE_EMAIL_TAKEN', 400],
] as const

const rejectedAnswersInEachLanguage = languages.flatMap((language) =>
  rejectedAnswers.map(([code, status]) => [language, code, status] as const),
)

async function renderGoogleSignUp(
  language: Language = 'en',
  routerState: unknown = { idToken: testIdToken },
) {
  return renderElementWithProviders(<GoogleSignUpScreen />, routes.googleSignUp, {
    language,
    routerState,
  })
}

function nameField(language: Language = 'en'): HTMLInputElement {
  return fieldLabelled(translated(language, 'auth.signUp.name'))
}

function pressSubmit(language: Language = 'en'): void {
  fireEvent.click(
    screen.getByRole('button', { name: translated(language, 'auth.googleSignUp.submit') }),
  )
}

describe('GoogleSignUpScreen', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends a visitor who arrives without a Google token to the welcome screen, replacing the history entry', async () => {
    const { router } = await renderElementWithProviders(
      <GoogleSignUpScreen />,
      routes.googleSignUp,
    )

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.welcome)
    })
    expect(router.state.historyAction).toBe('REPLACE')
  })

  it.each(languages)('shows the title (%s)', async (language) => {
    await renderGoogleSignUp(language)

    expect(
      screen.getByRole('heading', { name: translated(language, 'auth.googleSignUp.title') }),
    ).toBeTruthy()
  })

  it('has a back button to the welcome screen', async () => {
    await renderGoogleSignUp()

    const back = screen.getByRole('link', { name: translated('en', 'common.back') })

    expect(back.getAttribute('href')).toBe(routes.welcome)
  })

  it('fills the name field with the name from the Google token', async () => {
    await renderGoogleSignUp()

    expect(nameField().value).toBe(testGoogleProfile.name)
  })

  it('explains under the name field how friends will see the name', async () => {
    await renderGoogleSignUp()

    expect(descriptionOf(nameField())).toBe(translated('en', 'auth.googleSignUp.hint'))
  })

  it('shows the form with no error before anything is sent', async () => {
    await renderGoogleSignUp()

    expect(
      screen.getByRole('button', { name: translated('en', 'auth.googleSignUp.submit') }),
    ).toBeTruthy()
    expect(screen.queryByRole('alert')).toBeNull()
  })

  it.each(languages)('sends the token, the unchanged name, the device time zone and the screen language %s to the Google sign-up endpoint', async (language) => {
    const fetchMock = stubFetch(Response.json(testMe))
    await renderGoogleSignUp(language)

    pressSubmit(language)

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })
    expect(sentRequest(fetchMock)).toMatchObject({
      url: endpoints.googleSignUp,
      method: 'POST',
      body: {
        idToken: testIdToken,
        displayName: testGoogleProfile.name,
        timeZone: expect.any(String),
        language,
      },
    })
  })

  it('sends the name the person typed instead of the Google name', async () => {
    const fetchMock = stubFetch(Response.json(testMe))
    await renderGoogleSignUp()
    typeInto(translated('en', 'auth.signUp.name'), 'Марко П.')

    pressSubmit()

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })
    expect(sentRequest(fetchMock).body).toMatchObject({ displayName: 'Марко П.' })
  })

  it('sends the request even when the name is empty, because the server does the checking', async () => {
    const fetchMock = stubFetch(problemResponse(400, 'AUTH_DISPLAY_NAME_INVALID'))
    await renderGoogleSignUp()
    typeInto(translated('en', 'auth.signUp.name'), '')

    pressSubmit()

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })
    expect(sentRequest(fetchMock).body).toMatchObject({ displayName: '' })
  })

  it('disables the submit button while the request is waiting for an answer', async () => {
    stubFetchThatNeverAnswers()
    await renderGoogleSignUp()

    pressSubmit()

    await expectDisabledWhilePending(
      screen.getByRole('button', { name: translated('en', 'auth.googleSignUp.submit') }),
    )
  })

  it('puts the new person into the me cache after a successful sign-up', async () => {
    stubFetch(Response.json(testMe))
    const { queryClient } = await renderGoogleSignUp()

    pressSubmit()

    await waitFor(() => {
      expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
    })
  })

  it('goes to the home screen, replacing the sign-up entry in the history, after a successful sign-up', async () => {
    stubFetch(Response.json(testMe))
    const { router } = await renderGoogleSignUp()

    pressSubmit()

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.dashboard)
    })
    expect(router.state.historyAction).toBe('REPLACE')
  })

  it.each(rejectedAnswersInEachLanguage)('shows the %s text for the answer %s inline', async (language, code, status) => {
    stubFetch(problemResponse(status, code))
    await renderGoogleSignUp(language)

    pressSubmit(language)

    expect((await screen.findByRole('alert')).textContent).toContain(
      translated(language, `errors.${code}`),
    )
  })

  it.each(languages)('shows the network text inline when the server cannot be reached (%s)', async (language) => {
    stubFetch(new TypeError('Failed to fetch'))
    await renderGoogleSignUp(language)

    pressSubmit(language)

    expect((await screen.findByRole('alert')).textContent).toContain(
      translated(language, 'errors.network'),
    )
  })

  it('keeps the typed name, the screen and an empty me cache after the server refuses', async () => {
    stubFetch(problemResponse(400, 'AUTH_DISPLAY_NAME_INVALID'))
    const { router, queryClient } = await renderGoogleSignUp()
    typeInto(translated('en', 'auth.signUp.name'), 'Марко П.')

    pressSubmit()
    await screen.findByRole('alert')

    expect(nameField().value).toBe('Марко П.')
    expect(router.state.location.pathname).toBe(routes.googleSignUp)
    expect(queryClient.getQueryData(meQueryKey)).toBeUndefined()
  })

  describe('when it is reached from an invite card', () => {
    const inviteState = { idToken: testIdToken, joinToken: testInviteToken }

    it('shows the name form as usual', async () => {
      await renderGoogleSignUp('en', inviteState)

      expect(nameField().value).toBe(testGoogleProfile.name)
    })

    it('goes to the join screen, replacing the sign-up entry in the history, after a successful sign-up', async () => {
      stubFetch(Response.json(testMe))
      const { router } = await renderGoogleSignUp('en', inviteState)

      pressSubmit()

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.join(testInviteToken))
      })
      expect(router.state.historyAction).toBe('REPLACE')
    })

    it('sends the same request to the Google sign-up endpoint and does not send the invite token', async () => {
      const fetchMock = stubFetch(Response.json(testMe))
      await renderGoogleSignUp('en', inviteState)

      pressSubmit()

      await waitFor(() => {
        expect(fetchMock).toHaveBeenCalledOnce()
      })
      const request = sentRequest(fetchMock)
      expect(request).toMatchObject({
        url: endpoints.googleSignUp,
        body: { idToken: testIdToken, displayName: testGoogleProfile.name },
      })
      expect(JSON.stringify(request)).not.toContain(testInviteToken)
    })

    it('puts the new person into the me cache before going to the join screen', async () => {
      stubFetch(Response.json(testMe))
      const { router, queryClient } = await renderGoogleSignUp('en', inviteState)

      pressSubmit()

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.join(testInviteToken))
      })
      expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
    })

    it('stays on the screen after the server refuses', async () => {
      stubFetch(problemResponse(400, 'AUTH_DISPLAY_NAME_INVALID'))
      const { router } = await renderGoogleSignUp('en', inviteState)

      pressSubmit()
      await screen.findByRole('alert')

      expect(router.state.location.pathname).toBe(routes.googleSignUp)
    })

    it.each(unsafeJoinTokens)('goes only to the join path with the token %j encoded, never to an address of its own', async (token) => {
      stubFetch(Response.json(testMe))
      const { router } = await renderGoogleSignUp('en', { idToken: testIdToken, joinToken: token })

      pressSubmit()

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(joinPathOf(token))
      })
      expect(router.state.location.search).toBe('')
      expect(router.state.location.hash).toBe('')
    })

    it('goes to the home screen when the token in the state is not a text', async () => {
      stubFetch(Response.json(testMe))
      const { router } = await renderGoogleSignUp('en', { idToken: testIdToken, joinToken: 42 })

      pressSubmit()

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.dashboard)
      })
    })

    it('still sends a visitor who has an invite token but no Google token to the welcome screen', async () => {
      const { router } = await renderGoogleSignUp('en', { joinToken: testInviteToken })

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.welcome)
      })
    })
  })
})
