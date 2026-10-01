import { act, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { I18nextProvider } from 'react-i18next'
import { createMemoryRouter } from 'react-router'
import { RouterProvider } from 'react-router/dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createI18n } from '@/core/i18n/i18n'
import en from '@/core/i18n/locales/en.json'
import mk from '@/core/i18n/locales/mk.json'
import type { Language } from '@/core/i18n/language'
import { useApiHealth } from '@/features/auth/welcome/hooks/useApiHealth'
import { problemResponse, stubFetch } from '@/test/apiTestHelpers'
import { typeInto } from '@/test/formTestHelpers'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { testMe } from '@/test/testMe'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { routeObjects } from './router'
import { routes } from './routes'

const crash = new Error('formatMoney got a bad amount from the API')

vi.mock('@/features/auth/welcome/hooks/useApiHealth', () => ({
  useApiHealth: vi.fn(),
}))

const mustChangePasswordMe = { ...testMe, mustChangePassword: true }
const signedOutAnswer = problemResponse(401, 'AUTH_NOT_SIGNED_IN')

async function renderWelcomeRoute(language: Language): Promise<void> {
  const i18n = await createI18n(language)
  const router = createMemoryRouter(routeObjects, {
    initialEntries: ['/welcome'],
  })
  render(
    <I18nextProvider i18n={i18n}>
      <RouterProvider router={router} />
    </I18nextProvider>,
  )
}

describe('router error element', () => {
  beforeEach(() => {
    vi.mocked(useApiHealth).mockImplementation((): void => {
      throw crash
    })
  })

  afterEach(() => {
    vi.restoreAllMocks()
    vi.mocked(useApiHealth).mockReset()
  })

  it.each([
    ['en', en.errors.generic, en.common.retry],
    ['mk', mk.errors.generic, mk.common.retry],
  ] as const)(
    'shows a translated message with a retry button when a screen crashes while rendering (%s)',
    async (language, message, retry) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})

      await renderWelcomeRoute(language)

      expect((await screen.findByRole('alert')).textContent).toContain(message)
      expect(screen.getByRole('button', { name: retry })).toBeTruthy()
      expect(screen.queryByText(/Unexpected Application Error/)).toBeNull()
    },
  )

  it('logs the error that crashed the screen', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})

    await renderWelcomeRoute('en')
    await screen.findByRole('alert')

    expect(consoleError).toHaveBeenCalledWith(
      'A screen crashed while rendering',
      crash,
    )
  })
})

describe('routes followed through the real route table', () => {
  beforeEach(() => {
    stubDeviceColorScheme('light')
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends a signed-out visitor who opens the home address to the welcome screen', async () => {
    stubFetch(signedOutAnswer)

    const { router } = await renderRoutesWithProviders(routeObjects, routes.dashboard)

    expect(
      await screen.findByRole('button', { name: translated('en', 'welcome.continueWithGoogle') }),
    ).toBeTruthy()
    expect(router.state.location.pathname).toBe(routes.welcome)
  })

  it.each([
    [routes.welcome, 'button', 'welcome.continueWithGoogle'],
    [routes.signUp, 'heading', 'auth.signUp.title'],
    [routes.logIn, 'heading', 'auth.logIn.title'],
  ])('shows the screen at %s to a visitor without asking who is signed in', async (path, role, key) => {
    const fetchMock = stubFetch(signedOutAnswer)

    const { router } = await renderRoutesWithProviders(routeObjects, path)

    expect(await screen.findByRole(role, { name: translated('en', key) })).toBeTruthy()
    expect(router.state.location.pathname).toBe(path)
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('shows the home screen to a signed-in person', async () => {
    stubFetch(Response.json(testMe))

    await renderRoutesWithProviders(routeObjects, routes.dashboard)

    expect(
      await screen.findByText(translated('en', 'home.greeting', { name: testMe.displayName })),
    ).toBeTruthy()
  })

  it('shows the settings screen to a signed-in person', async () => {
    stubFetch(Response.json(testMe))

    await renderRoutesWithProviders(routeObjects, routes.settings)

    expect(
      await screen.findByRole('heading', { name: translated('en', 'settings.title') }),
    ).toBeTruthy()
  })

  it.each([routes.dashboard, routes.settings])(
    'sends a person who must change the password from %s to the change-password screen',
    async (path) => {
      stubFetch(Response.json(mustChangePasswordMe))

      const { router } = await renderRoutesWithProviders(routeObjects, path)

      expect(
        await screen.findByRole('heading', { name: translated('en', 'auth.changePassword.title') }),
      ).toBeTruthy()
      expect(router.state.location.pathname).toBe(routes.changePassword)
    },
  )

  it.each([routes.dashboard, routes.settings])(
    'keeps a person who must change the password on the change-password screen when they navigate to %s',
    async (path) => {
      stubFetch(Response.json(mustChangePasswordMe))
      const { router } = await renderRoutesWithProviders(routeObjects, routes.changePassword)
      await screen.findByRole('heading', { name: translated('en', 'auth.changePassword.title') })

      await act(() => router.navigate(path))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.changePassword)
      })
      expect(
        screen.getByRole('heading', { name: translated('en', 'auth.changePassword.title') }),
      ).toBeTruthy()
    },
  )

  it('shows the not-found screen for an unknown address', async () => {
    stubFetch(signedOutAnswer)

    await renderRoutesWithProviders(routeObjects, '/no-such-page')

    expect(
      await screen.findByRole('heading', { name: translated('en', 'notFound.title') }),
    ).toBeTruthy()
  })

  it('takes a visitor from the sign-up screen to the home screen after signing up', async () => {
    stubFetch(Response.json(testMe))
    const { router } = await renderRoutesWithProviders(routeObjects, routes.signUp)
    typeInto(translated('en', 'auth.signUp.name'), testMe.displayName)
    typeInto(translated('en', 'auth.email'), testMe.email)
    typeInto(translated('en', 'auth.password'), 'Passw0rdOk')

    fireEvent.click(screen.getByRole('button', { name: translated('en', 'auth.signUp.title') }))

    expect(
      await screen.findByText(translated('en', 'home.greeting', { name: testMe.displayName })),
    ).toBeTruthy()
    expect(router.state.location.pathname).toBe(routes.dashboard)
  })

  it('takes a signed-in person from the settings screen to the welcome screen after logging out', async () => {
    stubFetch(Response.json(testMe), new Response(null, { status: 204 }))
    const { router } = await renderRoutesWithProviders(routeObjects, routes.settings)
    await screen.findByRole('heading', { name: translated('en', 'settings.title') })

    fireEvent.click(screen.getByRole('button', { name: translated('en', 'settings.logOut') }))

    expect(
      await screen.findByRole('button', { name: translated('en', 'welcome.continueWithGoogle') }),
    ).toBeTruthy()
    expect(router.state.location.pathname).toBe(routes.welcome)
  })
})
