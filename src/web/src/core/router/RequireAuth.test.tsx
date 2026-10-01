import { act, fireEvent, screen } from '@testing-library/react'
import type { RouteObject } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { meQueryKey } from '@/core/auth/useMe'
import en from '@/core/i18n/locales/en.json'
import mk from '@/core/i18n/locales/mk.json'
import {
  problemResponse,
  stubFetch,
  stubFetchThatNeverAnswers,
} from '@/test/apiTestHelpers'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { testMe } from '@/test/testMe'
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
})
