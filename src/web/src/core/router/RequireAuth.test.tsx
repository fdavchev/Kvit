import type { QueryClient } from '@tanstack/react-query'
import { act, fireEvent, screen, waitFor } from '@testing-library/react'
import type { RouteObject } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { checkRestoredMeWithServer } from '@/core/api/savedQueryCache'
import { meQueryKey } from '@/core/auth/useMe'
import type { Me } from '@/core/services/me/meService'
import en from '@/core/i18n/locales/en.json'
import mk from '@/core/i18n/locales/mk.json'
import {
  problemResponse,
  requestCountTo,
  stubFetch,
  stubFetchThatNeverAnswers,
} from '@/test/apiTestHelpers'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { seedMe, testMe } from '@/test/testMe'
import { RequireAuth } from './RequireAuth'
import { routes } from './routes'

const routeTable: RouteObject[] = [
  { path: routes.welcome, element: <p>welcome page</p> },
  {
    element: <RequireAuth />,
    children: [
      { path: routes.dashboard, element: <p>home page</p> },
      { path: routes.changePassword, element: <p>change password page</p> },
    ],
  },
]

const mustChangePasswordMe = { ...testMe, mustChangePassword: true }

function seedRestoredMe(restoredMe: Me | null) {
  return (queryClient: QueryClient): void => {
    seedMe(restoredMe)(queryClient)
    void checkRestoredMeWithServer(queryClient)
  }
}

describe('RequireAuth', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('shows the loading spinner while me is being asked', async () => {
    stubFetchThatNeverAnswers()

    await renderRoutesWithProviders(routeTable, routes.dashboard)

    expect(screen.getByRole('status')).toBeTruthy()
    expect(screen.queryByText('home page')).toBeNull()
    expect(screen.queryByText('welcome page')).toBeNull()
  })

  it('shows the protected page to a signed-in person', async () => {
    stubFetch(Response.json(testMe))

    await renderRoutesWithProviders(routeTable, routes.dashboard)

    expect(await screen.findByText('home page')).toBeTruthy()
  })

  it('lets a signed-in person who does not have to change the password open the change-password page', async () => {
    stubFetch(Response.json(testMe))

    await renderRoutesWithProviders(routeTable, routes.changePassword)

    expect(await screen.findByText('change password page')).toBeTruthy()
  })

  it.each([
    ['a signed-out visitor goes to the welcome page', problemResponse(401, 'AUTH_NOT_SIGNED_IN'), routes.welcome, 'welcome page'],
    ['a person who must change the password goes to the change-password page', Response.json(mustChangePasswordMe), routes.changePassword, 'change password page'],
  ])('redirects by replacing the history entry: %s', async (_name, meAnswer, path, pageText) => {
    stubFetch(meAnswer)

    const { router } = await renderRoutesWithProviders(routeTable, routes.dashboard)

    expect(await screen.findByText(pageText)).toBeTruthy()
    expect(router.state.location.pathname).toBe(path)
    expect(router.state.historyAction).toBe('REPLACE')
  })

  it('keeps a person who must change the password on the change-password page', async () => {
    stubFetch(Response.json(mustChangePasswordMe))

    const { router } = await renderRoutesWithProviders(routeTable, routes.changePassword)

    expect(await screen.findByText('change password page')).toBeTruthy()
    expect(router.state.historyAction).toBe('POP')
  })

  it('sends a signed-in person to the welcome page when me becomes null, for example after logging out', async () => {
    stubFetch(Response.json(testMe))
    const { queryClient } = await renderRoutesWithProviders(routeTable, routes.dashboard)
    await screen.findByText('home page')

    act(() => {
      queryClient.setQueryData(meQueryKey, null)
    })

    expect(await screen.findByText('welcome page')).toBeTruthy()
  })

  it.each([
    ['a server error', 'en', () => new Response(null, { status: 500 }), en.errors.generic, en.common.retry],
    ['a network failure', 'en', () => new TypeError('Failed to fetch'), en.errors.network, en.common.retry],
    ['a network failure', 'mk', () => new TypeError('Failed to fetch'), mk.errors.network, mk.common.retry],
  ] as const)(
    'shows the translated error with a retry button and never treats it as signed out when me fails with %s (%s)',
    async (_name, language, makeFailure, message, retry) => {
      stubFetch(makeFailure())

      await renderRoutesWithProviders(routeTable, routes.dashboard, { language })

      expect((await screen.findByRole('alert')).textContent).toContain(message)
      expect(screen.getByRole('button', { name: retry })).toBeTruthy()
      expect(screen.queryByText('welcome page')).toBeNull()
      expect(screen.queryByText('home page')).toBeNull()
    },
  )

  it('asks for me again when the retry button is pressed', async () => {
    stubFetch(new Response(null, { status: 500 }), Response.json(testMe))
    await renderRoutesWithProviders(routeTable, routes.dashboard)

    fireEvent.click(await screen.findByRole('button', { name: en.common.retry }))

    expect(await screen.findByText('home page')).toBeTruthy()
  })

  describe('with a person restored from the saved copy', () => {
    it('shows the screen at once while /api/me has not answered yet', async () => {
      stubFetchThatNeverAnswers()

      const { queryClient } = await renderRoutesWithProviders(routeTable, routes.dashboard, {
        seedCache: seedRestoredMe(testMe),
      })

      expect(screen.getByText('home page')).toBeTruthy()
      expect(screen.queryByRole('status')).toBeNull()
      expect(queryClient.getQueryState(meQueryKey)?.fetchStatus).toBe('fetching')
    })

    it('asks /api/me exactly once in the background', async () => {
      const fetchMock = stubFetch(Response.json(testMe))

      const { queryClient } = await renderRoutesWithProviders(routeTable, routes.dashboard, {
        seedCache: seedRestoredMe(testMe),
      })

      await waitFor(() => {
        expect(queryClient.getQueryState(meQueryKey)?.fetchStatus).toBe('idle')
      })
      expect(screen.getByText('home page')).toBeTruthy()
      expect(requestCountTo(fetchMock, '/api/me')).toBe(1)
    })

    it.each([
      ['a 502 from the server', () => new Response(null, { status: 502 })],
      ['a 500 from the server', () => new Response(null, { status: 500 })],
      ['a network failure', () => new TypeError('Failed to fetch')],
    ])('keeps the screen up with no error and no spinner when the background /api/me fails with %s', async (_name, makeFailure) => {
      const fetchMock = stubFetch(makeFailure())

      const { queryClient } = await renderRoutesWithProviders(routeTable, routes.dashboard, {
        seedCache: seedRestoredMe(testMe),
      })

      await waitFor(() => {
        expect(queryClient.getQueryState(meQueryKey)?.status).toBe('error')
      })
      expect(requestCountTo(fetchMock, '/api/me')).toBe(1)
      expect(screen.getByText('home page')).toBeTruthy()
      expect(screen.queryByRole('alert')).toBeNull()
      expect(screen.queryByRole('status')).toBeNull()
      expect(screen.queryByText('welcome page')).toBeNull()
    })

    it('goes to the welcome page when the background /api/me answers 401', async () => {
      stubFetch(problemResponse(401, 'AUTH_NOT_SIGNED_IN'))

      const { router } = await renderRoutesWithProviders(routeTable, routes.dashboard, {
        seedCache: seedRestoredMe(testMe),
      })

      expect(await screen.findByText('welcome page')).toBeTruthy()
      expect(router.state.location.pathname).toBe(routes.welcome)
      expect(screen.queryByText('home page')).toBeNull()
    })

    it('goes to the change-password page when the background /api/me says the password must change', async () => {
      stubFetch(Response.json(mustChangePasswordMe))

      await renderRoutesWithProviders(routeTable, routes.dashboard, {
        seedCache: seedRestoredMe(testMe),
      })

      expect(await screen.findByText('change password page')).toBeTruthy()
    })

    it('goes to the change-password page at once when the restored person must change the password', async () => {
      stubFetchThatNeverAnswers()

      await renderRoutesWithProviders(routeTable, routes.dashboard, {
        seedCache: seedRestoredMe(mustChangePasswordMe),
      })

      expect(screen.getByText('change password page')).toBeTruthy()
    })

    it('goes to the welcome page at once when the saved copy says nobody is signed in', async () => {
      stubFetchThatNeverAnswers()

      await renderRoutesWithProviders(routeTable, routes.dashboard, {
        seedCache: seedRestoredMe(null),
      })

      expect(screen.getByText('welcome page')).toBeTruthy()
    })
  })

  it('shows the loading spinner and no error when there is no me and the server has not answered', async () => {
    stubFetchThatNeverAnswers()

    await renderRoutesWithProviders(routeTable, routes.dashboard)

    expect(screen.getByRole('status')).toBeTruthy()
    expect(screen.queryByRole('alert')).toBeNull()
  })

  it('shows the error and not the screen when there is no me and /api/me returns 502', async () => {
    stubFetch(new Response(null, { status: 502 }))

    await renderRoutesWithProviders(routeTable, routes.dashboard)

    expect(await screen.findByRole('alert')).toBeTruthy()
    expect(screen.queryByRole('status')).toBeNull()
    expect(screen.queryByText('home page')).toBeNull()
  })
})
