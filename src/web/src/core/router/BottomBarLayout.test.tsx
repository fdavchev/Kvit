import type { QueryClient } from '@tanstack/react-query'
import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import type { RouteObject } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { endpoints } from '@/core/api/endpoints'
import { checkRestoredMeWithServer } from '@/core/api/savedQueryCache'
import { meQueryKey } from '@/core/auth/useMe'
import { languages, type Language } from '@/core/i18n/language'
import type { Me } from '@/core/services/me/meService'
import { requestCountTo, stubFetch, stubFetchThatNeverAnswers } from '@/test/apiTestHelpers'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { BottomBarLayout } from './BottomBarLayout'
import { RequireAuth } from './RequireAuth'
import { routes } from './routes'

const routeTable: RouteObject[] = [
  {
    element: <BottomBarLayout />,
    children: [
      { path: routes.dashboard, element: <p>home page</p> },
      { path: routes.groups, element: <p>groups page</p> },
      { path: routes.recentlyDeletedGroups, element: <p>recently deleted page</p> },
      { path: routes.settings, element: <p>settings page</p> },
    ],
  },
  { path: routes.newGroup, element: <p>new group page</p> },
]

const tabs: [string, string, string][] = [
  ['nav.home', routes.dashboard, 'home page'],
  ['nav.groups', routes.groups, 'groups page'],
  ['nav.settings', routes.settings, 'settings page'],
]

function bar(): HTMLElement {
  return screen.getByRole('navigation')
}

function tab(language: Language, key: string): HTMLElement {
  return within(bar()).getByRole('link', { name: translated(language, key) })
}

describe('BottomBarLayout', () => {
  it.each(tabs)('shows the page of the route and the bar at %s', async (_key, path, pageText) => {
    await renderRoutesWithProviders(routeTable, path)

    expect(screen.getByText(pageText)).toBeTruthy()
    expect(bar()).toBeTruthy()
  })

  it('shows the bar and the page of Recently deleted', async () => {
    await renderRoutesWithProviders(routeTable, routes.recentlyDeletedGroups)

    expect(screen.getByText('recently deleted page')).toBeTruthy()
    expect(bar()).toBeTruthy()
  })

  it('shows no bar on a route outside the layout', async () => {
    await renderRoutesWithProviders(routeTable, routes.newGroup)

    expect(screen.getByText('new group page')).toBeTruthy()
    expect(screen.queryByRole('navigation')).toBeNull()
  })

  it.each(languages)('has exactly three tabs, Home, Groups and Settings, in the translated names (%s)', async (language) => {
    await renderRoutesWithProviders(routeTable, routes.groups, { language })

    const names = within(bar())
      .getAllByRole('link')
      .map((link) => link.textContent)

    expect(names).toEqual([
      translated(language, 'nav.home'),
      translated(language, 'nav.groups'),
      translated(language, 'nav.settings'),
    ])
  })

  it.each([
    ['nav.home', routes.dashboard],
    ['nav.groups', routes.groups],
    ['nav.settings', routes.settings],
  ])('sends the %s tab to %s', async (key, path) => {
    await renderRoutesWithProviders(routeTable, routes.groups)

    expect(tab('en', key).getAttribute('href')).toBe(path)
  })

  it.each([
    [routes.dashboard, 'nav.home'],
    [routes.groups, 'nav.groups'],
    [routes.recentlyDeletedGroups, 'nav.groups'],
    [routes.settings, 'nav.settings'],
  ])('marks only one tab as the current page at %s: %s', async (path, currentKey) => {
    await renderRoutesWithProviders(routeTable, path)

    for (const [key] of tabs) {
      expect(tab('en', key).getAttribute('aria-current')).toBe(key === currentKey ? 'page' : null)
    }
  })

  it('draws the bar after the page so it sits under it', async () => {
    await renderRoutesWithProviders(routeTable, routes.groups)

    const page = screen.getByText('groups page')

    expect(page.compareDocumentPosition(bar()) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
  })

  it('keeps the bar and changes the page when a tab is pressed', async () => {
    const { router } = await renderRoutesWithProviders(routeTable, routes.groups)

    fireEvent.click(tab('en', 'nav.settings'))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.settings)
    })
    expect(screen.getByText('settings page')).toBeTruthy()
    expect(tab('en', 'nav.settings').getAttribute('aria-current')).toBe('page')
  })
})

const signedInRouteTable: RouteObject[] = [
  { path: routes.welcome, element: <p>welcome page</p> },
  { element: <RequireAuth />, children: routeTable },
]

const beforeThePageOpened = performance.timeOrigin - 60 * 60 * 1000

function restoredMe(me: Me | null): (queryClient: QueryClient) => void {
  return (queryClient) => {
    queryClient.setQueryData<Me | null>(meQueryKey, me, { updatedAt: beforeThePageOpened })
    void checkRestoredMeWithServer(queryClient)
  }
}

function meFetchedInThisPage(queryClient: QueryClient): void {
  queryClient.setQueryData<Me | null>(meQueryKey, testMe)
  void checkRestoredMeWithServer(queryClient)
}

describe('BottomBarLayout Updating… note', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('shows the Updating… note in %s while the saved person is checked with the server', async (language) => {
    stubFetchThatNeverAnswers()

    await renderRoutesWithProviders(signedInRouteTable, routes.groups, {
      language,
      seedCache: restoredMe({ ...testMe, language }),
    })

    expect(screen.getByText('groups page')).toBeTruthy()
    expect((await screen.findByRole('status')).textContent).toBe(translated(language, 'common.updating'))
  })

  it('shows no note when the person was already fetched in this page and is checked again', async () => {
    const fetchMock = vi.fn<typeof fetch>(() => new Promise<Response>(() => {}))
    vi.stubGlobal('fetch', fetchMock)

    await renderRoutesWithProviders(signedInRouteTable, routes.groups, { seedCache: meFetchedInThisPage })

    await waitFor(() => {
      expect(requestCountTo(fetchMock, endpoints.me)).toBe(1)
    })
    expect(screen.getByText('groups page')).toBeTruthy()
    expect(screen.queryByText(translated('en', 'common.updating'))).toBeNull()
  })

  it('shows no note on a later check after the first check of the saved person failed', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 503 }))
    const { queryClient } = await renderRoutesWithProviders(signedInRouteTable, routes.groups, {
      seedCache: restoredMe(testMe),
    })
    await waitFor(() => {
      expect(queryClient.getQueryState(meQueryKey)?.status).toBe('error')
    })
    stubFetchThatNeverAnswers()

    void queryClient.refetchQueries({ queryKey: meQueryKey })

    await waitFor(() => {
      expect(queryClient.getQueryState(meQueryKey)?.fetchStatus).toBe('fetching')
    })
    expect(requestCountTo(fetchMock, endpoints.me)).toBe(1)
    expect(screen.queryByText(translated('en', 'common.updating'))).toBeNull()
  })

  it('shows no note and the Welcome page when the saved copy says nobody is signed in', async () => {
    stubFetchThatNeverAnswers()

    await renderRoutesWithProviders(signedInRouteTable, routes.groups, { seedCache: restoredMe(null) })

    expect(await screen.findByText('welcome page')).toBeTruthy()
    expect(screen.queryByText(translated('en', 'common.updating'))).toBeNull()
  })
})
