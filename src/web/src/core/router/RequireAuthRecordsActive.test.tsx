import { act, screen, waitFor } from '@testing-library/react'
import { toast } from 'sonner'
import type { RouteObject } from 'react-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { problemResponse } from '@/test/apiTestHelpers'
import { activePath } from '@/test/dashboardTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import {
  jsonAnswer,
  noContentAnswer,
  problemAnswer,
  requestCount,
  stubFetchByRequest,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { testMe } from '@/test/testMe'
import { RequireAuth } from './RequireAuth'
import { routes } from './routes'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const routeTable: RouteObject[] = [
  { path: routes.welcome, element: <p>welcome page</p> },
  {
    element: <RequireAuth />,
    children: [
      { path: routes.dashboard, element: <p>home page</p> },
      { path: routes.groups, element: <p>groups page</p> },
      { path: routes.group(':groupId'), element: <p>group page</p> },
    ],
  },
]

function stubApp(activeAnswer: AnswerFactory, meAnswer: AnswerFactory = jsonAnswer(testMe)) {
  return stubFetchByRequest({
    'GET /api/me': meAnswer,
    [`POST ${activePath}`]: activeAnswer,
  })
}

describe('RequireAuth records that the person is active', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('sends one POST /api/me/active with no body when a signed-in person opens the home page', async () => {
    const fetchMock = stubApp(noContentAnswer())

    await renderRoutesWithProviders(routeTable, routes.dashboard)

    expect(await screen.findByText('home page')).toBeTruthy()
    await waitFor(() => {
      expect(requestCount(fetchMock, 'POST', activePath)).toBe(1)
    })
  })

  it('sends one POST when a signed-in person opens a group straight from a link, because any signed-in page counts as using the app', async () => {
    const fetchMock = stubApp(noContentAnswer())

    await renderRoutesWithProviders(routeTable, routes.group('g-1'))

    expect(await screen.findByText('group page')).toBeTruthy()
    await waitFor(() => {
      expect(requestCount(fetchMock, 'POST', activePath)).toBe(1)
    })
  })

  it('does not send it again when the person moves to another page inside the app', async () => {
    const fetchMock = stubApp(noContentAnswer())
    const { router } = await renderRoutesWithProviders(routeTable, routes.dashboard)
    await waitFor(() => {
      expect(requestCount(fetchMock, 'POST', activePath)).toBe(1)
    })

    await act(() => router.navigate(routes.groups))
    await screen.findByText('groups page')
    await act(() => router.navigate(routes.dashboard))
    await screen.findByText('home page')

    expect(requestCount(fetchMock, 'POST', activePath)).toBe(1)
  })

  it('does not send it to a signed-out visitor, who is sent to the welcome page', async () => {
    const fetchMock = stubApp(noContentAnswer(), problemAnswer(401, 'AUTH_NOT_SIGNED_IN'))

    await renderRoutesWithProviders(routeTable, routes.dashboard)

    expect(await screen.findByText('welcome page')).toBeTruthy()
    expect(requestCount(fetchMock, 'POST', activePath)).toBe(0)
  })

  it('does not send it while the server has not yet said who is signed in', async () => {
    const fetchMock = vi.fn<typeof fetch>(() => new Promise<Response>(() => {}))
    vi.stubGlobal('fetch', fetchMock)

    await renderRoutesWithProviders(routeTable, routes.dashboard)

    expect(screen.getByRole('status')).toBeTruthy()
    expect(requestCount(fetchMock, 'POST', activePath)).toBe(0)
  })

  it('does not send it when asking who is signed in failed', async () => {
    const fetchMock = stubApp(noContentAnswer(), () => problemResponse(500, 'SOMETHING_BROKE'))

    await renderRoutesWithProviders(routeTable, routes.dashboard)

    expect(await screen.findByRole('alert')).toBeTruthy()
    expect(requestCount(fetchMock, 'POST', activePath)).toBe(0)
  })

  describe('when the server refuses it', () => {
    it('writes the error to the console instead of hiding it', async () => {
      const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
      stubApp(problemAnswer(500, 'SOMETHING_BROKE'))

      await renderRoutesWithProviders(routeTable, routes.dashboard)

      await waitFor(() => {
        expect(consoleError).toHaveBeenCalled()
      })
      const loggedValues = consoleError.mock.calls.flat()
      expect(loggedValues.some((value) => value instanceof ApiError)).toBe(true)
    })

    it('still shows the page and shows no toast', async () => {
      const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
      stubApp(problemAnswer(500, 'SOMETHING_BROKE'))

      await renderRoutesWithProviders(routeTable, routes.dashboard)

      await waitFor(() => {
        expect(consoleError).toHaveBeenCalled()
      })
      expect(screen.getByText('home page')).toBeTruthy()
      expect(vi.mocked(toast).mock.calls).toEqual([])
      expect(vi.mocked(toast.error).mock.calls).toEqual([])
    })

    it('writes the error to the console when the network fails, and does not try again by itself', async () => {
      const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
      const fetchMock = stubApp(() => new TypeError('Failed to fetch'))

      await renderRoutesWithProviders(routeTable, routes.dashboard)

      await waitFor(() => {
        expect(consoleError).toHaveBeenCalled()
      })
      expect(screen.getByText('home page')).toBeTruthy()
      expect(requestCount(fetchMock, 'POST', activePath)).toBe(1)
    })
  })
})
