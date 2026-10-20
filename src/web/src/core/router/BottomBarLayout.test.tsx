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
import { stubDesktopScreen } from '@/test/matchMediaTestHelpers'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { seedMe, testGuestMe, testMe } from '@/test/testMe'
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
      { path: routes.budget, element: <p>budget page</p> },
      { path: routes.settings, element: <p>settings page</p> },
    ],
  },
  { path: routes.newGroup, element: <p>new group page</p> },
]

const tabs: [string, string, string][] = [
  ['nav.home', routes.dashboard, 'home page'],
  ['nav.groups', routes.groups, 'groups page'],
  ['nav.budget', routes.budget, 'budget page'],
  ['nav.settings', routes.settings, 'settings page'],
]

function renderLayoutAt(path: string, options: { language?: Language; me?: Me } = {}) {
  return renderRoutesWithProviders(routeTable, path, {
    language: options.language,
    seedCache: seedMe(options.me ?? testMe),
  })
}

function pillWidthFor(tabCount: number): string {
  const probe = document.createElement('span')
  probe.style.width = `calc((100% - 0.5rem) / ${tabCount})`
  return probe.style.width
}

function bar(): HTMLElement {
  return screen.getByRole('navigation')
}

function tab(language: Language, key: string): HTMLElement {
  return within(bar()).getByRole('link', { name: translated(language, key) })
}

describe('BottomBarLayout', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(tabs)('shows the page of the route and the bar at %s', async (_key, path, pageText) => {
    await renderLayoutAt(path)

    expect(screen.getByText(pageText)).toBeTruthy()
    expect(bar()).toBeTruthy()
  })

  it('shows the bar and the page of Recently deleted', async () => {
    await renderLayoutAt(routes.recentlyDeletedGroups)

    expect(screen.getByText('recently deleted page')).toBeTruthy()
    expect(bar()).toBeTruthy()
  })

  it('shows no bar on a route outside the layout', async () => {
    await renderLayoutAt(routes.newGroup)

    expect(screen.getByText('new group page')).toBeTruthy()
    expect(screen.queryByRole('navigation')).toBeNull()
  })

  it.each(languages)('has exactly four tabs, Home, Groups, Budget and Settings, in the translated names (%s)', async (language) => {
    await renderLayoutAt(routes.groups, { language })

    const names = within(bar())
      .getAllByRole('link')
      .map((link) => link.textContent)

    expect(names).toEqual([
      translated(language, 'nav.home'),
      translated(language, 'nav.groups'),
      translated(language, 'nav.budget'),
      translated(language, 'nav.settings'),
    ])
  })

  it.each(languages)('has no Budget tab for a guest, only Home, Groups and Settings (%s)', async (language) => {
    await renderLayoutAt(routes.groups, { language, me: { ...testGuestMe, language } })

    const names = within(bar())
      .getAllByRole('link')
      .map((link) => link.textContent)

    expect(names).toEqual([
      translated(language, 'nav.home'),
      translated(language, 'nav.groups'),
      translated(language, 'nav.settings'),
    ])
  })

  it('sizes the pill to a third of the bar for a guest, who has three tabs', async () => {
    await renderLayoutAt(routes.settings, { me: testGuestMe })

    const pill = bar().querySelector<HTMLElement>('[data-tab-pill]')

    expect(pill?.style.width).toBe(pillWidthFor(3))
    expect(pill?.style.transform).toBe('translateX(200%)')
  })

  it('sizes the pill to a quarter of the bar for a person with an account, who has four tabs', async () => {
    await renderLayoutAt(routes.budget)

    const pill = bar().querySelector<HTMLElement>('[data-tab-pill]')

    expect(pill?.style.width).toBe(pillWidthFor(4))
    expect(pill?.style.transform).toBe('translateX(200%)')
  })

  it.each([
    ['nav.home', routes.dashboard],
    ['nav.groups', routes.groups],
    ['nav.budget', routes.budget],
    ['nav.settings', routes.settings],
  ])('sends the %s tab to %s', async (key, path) => {
    await renderLayoutAt(routes.groups)

    expect(tab('en', key).getAttribute('href')).toBe(path)
  })

  it.each([
    [routes.dashboard, 'nav.home'],
    [routes.groups, 'nav.groups'],
    [routes.recentlyDeletedGroups, 'nav.groups'],
    [routes.budget, 'nav.budget'],
    [routes.settings, 'nav.settings'],
  ])('marks only one tab as the current page at %s: %s', async (path, currentKey) => {
    await renderLayoutAt(path)

    for (const [key] of tabs) {
      expect(tab('en', key).getAttribute('aria-current')).toBe(key === currentKey ? 'page' : null)
    }
  })

  it('draws the bar after the page so it sits under it', async () => {
    await renderLayoutAt(routes.groups)

    const page = screen.getByText('groups page')

    expect(page.compareDocumentPosition(bar()) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
  })

  it('keeps the same pill and moves it under the pressed tab, so it slides instead of jumping', async () => {
    const { router } = await renderLayoutAt(routes.groups)
    const pillBefore = bar().querySelector<HTMLElement>('[data-tab-pill]')

    fireEvent.click(tab('en', 'nav.settings'))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.settings)
    })
    expect(pillBefore?.isConnected).toBe(true)
    expect(bar().querySelector('[data-tab-pill]')).toBe(pillBefore)
    expect(pillBefore?.style.transform).toBe('translateX(300%)')
  })

  it('shows no bottom bar on a desktop-sized window, only the page', async () => {
    stubDesktopScreen()

    await renderLayoutAt(routes.groups)

    expect(screen.getByText('groups page')).toBeTruthy()
    expect(screen.queryByRole('navigation')).toBeNull()
    expect(document.querySelector('[data-bottom-bar]')).toBeNull()
  })

  it('keeps the bar and changes the page when a tab is pressed', async () => {
    const { router } = await renderLayoutAt(routes.groups)

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

  it('shows the note at the top of the screen, before the page and its title, not in the bottom bar', async () => {
    stubFetchThatNeverAnswers()

    await renderRoutesWithProviders(signedInRouteTable, routes.groups, { seedCache: restoredMe(testMe) })

    const note = await screen.findByRole('status')
    const page = screen.getByText('groups page')
    expect(note.compareDocumentPosition(page) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
    expect(note.closest('[data-bottom-bar]')).toBeNull()
  })

  it('lays the note over the top padding, centred and out of the flow, so nothing jumps when it comes and goes', async () => {
    stubFetchThatNeverAnswers()

    await renderRoutesWithProviders(signedInRouteTable, routes.groups, { seedCache: restoredMe(testMe) })

    const slot = (await screen.findByRole('status')).parentElement
    const classes = slot?.className.split(/\s+/)
    expect(slot?.hasAttribute('data-updating-note-slot')).toBe(true)
    expect(classes).toEqual(
      expect.arrayContaining([
        'absolute',
        'inset-x-0',
        'top-[max(0.125rem,env(safe-area-inset-top))]',
        'flex',
        'justify-center',
        'pointer-events-none',
      ]),
    )
  })

  it('keeps the empty top slot when no note is shown, so the page keeps its place in the tree', async () => {
    await renderLayoutAt(routes.groups)

    const slot = document.querySelector('[data-updating-note-slot]')
    if (slot === null) {
      throw new Error('The layout has no slot for the Updating… note')
    }
    expect(slot.childElementCount).toBe(0)
    expect(slot.compareDocumentPosition(screen.getByText('groups page')) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
  })

  it('shows the note at the top on a desktop-sized window too, where there is no bottom bar', async () => {
    stubDesktopScreen()
    stubFetchThatNeverAnswers()

    await renderRoutesWithProviders(signedInRouteTable, routes.groups, { seedCache: restoredMe(testMe) })

    const note = await screen.findByRole('status')
    expect(note.textContent).toBe(translated('en', 'common.updating'))
    expect(note.parentElement?.hasAttribute('data-updating-note-slot')).toBe(true)
    expect(document.querySelector('[data-bottom-bar]')).toBeNull()
  })

  it('has no animation of its own, so it respects reduced motion', async () => {
    stubFetchThatNeverAnswers()

    await renderRoutesWithProviders(signedInRouteTable, routes.groups, { seedCache: restoredMe(testMe) })

    const note = await screen.findByRole('status')
    const classes = `${note.className} ${note.parentElement?.className ?? ''}`
    expect(classes).not.toMatch(/animate|transition/)
  })
})
