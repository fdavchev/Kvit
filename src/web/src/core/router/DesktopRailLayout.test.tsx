import type { QueryClient } from '@tanstack/react-query'
import { screen, within } from '@testing-library/react'
import type { RouteObject } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { dashboardKey } from '@/features/home/shared/dashboardCache'
import { dashboardPath, fullDashboard, greeceGroupId } from '@/test/dashboardTestData'
import { stubDesktopScreen } from '@/test/matchMediaTestHelpers'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { jsonAnswer, noContentAnswer, requestCount, stubFetchByRequest } from '@/test/requestTestHelpers'
import { seedMe, testGuestMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { BottomBarLayout } from './BottomBarLayout'
import { RequireAuth } from './RequireAuth'
import { routes } from './routes'

const routeTable: RouteObject[] = [
  {
    element: <RequireAuth />,
    children: [
      {
        element: <BottomBarLayout />,
        children: [
          { path: routes.dashboard, element: <p>home page</p> },
          { path: routes.groups, element: <p>groups page</p> },
          { path: routes.budget, element: <p>budget page</p> },
          { path: routes.settings, element: <p>settings page</p> },
        ],
      },
      { path: routes.newGroup, element: <p>new group page</p> },
      { path: routes.group(':groupId'), element: <p>group page</p> },
    ],
  },
]

function stubSignedInServer() {
  return stubFetchByRequest({
    'GET /api/me': jsonAnswer(testMe),
    'POST /api/me/active': noContentAnswer(),
  })
}

function seedDashboard(queryClient: QueryClient): void {
  seedMe(testMe)(queryClient)
  queryClient.setQueryData(dashboardKey, fullDashboard)
}

async function renderAt(path: string, seedCache: (queryClient: QueryClient) => void = seedMe(testMe)) {
  const fetchMock = stubSignedInServer()
  const rendered = await renderRoutesWithProviders(routeTable, path, { seedCache })
  return { fetchMock, ...rendered }
}

function rail(): HTMLElement {
  return screen.getByRole('navigation')
}

function railLinkNames(): (string | null)[] {
  return within(rail())
    .getAllByRole('link')
    .map((link) => link.textContent)
}

describe('DesktopRailLayout on a desktop-sized window', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('shows the left menu with the brand, Home, Groups, Budget and Settings, and the page next to it', async () => {
    stubDesktopScreen()

    await renderAt(routes.groups)

    expect(await screen.findByText('groups page')).toBeTruthy()
    expect(within(rail()).getByText(translated('en', 'app.name'))).toBeTruthy()
    expect(railLinkNames()).toEqual([
      translated('en', 'nav.home'),
      translated('en', 'nav.groups'),
      translated('en', 'nav.budget'),
      translated('en', 'nav.settings'),
    ])
  })

  it('shows no Budget in the left menu of a guest', async () => {
    stubDesktopScreen()

    await renderAt(routes.groups, seedMe(testGuestMe))

    await screen.findByText('groups page')
    expect(railLinkNames()).toEqual([
      translated('en', 'nav.home'),
      translated('en', 'nav.groups'),
      translated('en', 'nav.settings'),
    ])
  })

  it('marks Budget as the current page in the left menu at the Budget tab', async () => {
    stubDesktopScreen()

    await renderAt(routes.budget)

    await screen.findByText('budget page')
    const current = within(rail())
      .getAllByRole('link')
      .filter((link) => link.getAttribute('aria-current') === 'page')
    expect(current.map((link) => link.textContent)).toEqual([translated('en', 'nav.budget')])
  })

  it('shows no bottom tab bar, so no phone space is kept for it at the bottom', async () => {
    stubDesktopScreen()

    await renderAt(routes.groups)

    await screen.findByText('groups page')
    expect(document.querySelector('[data-bottom-bar]')).toBeNull()
    expect(screen.getAllByRole('navigation')).toHaveLength(1)
  })

  it('marks the current page in the left menu', async () => {
    stubDesktopScreen()

    await renderAt(routes.settings)

    await screen.findByText('settings page')
    expect(within(rail()).getByRole('link', { name: translated('en', 'nav.settings') }).getAttribute('aria-current')).toBe('page')
  })

  it.each([
    [routes.newGroup, 'new group page'],
    [routes.group(greeceGroupId), 'group page'],
  ])('shows the left menu also at %s, a page without the bottom bar', async (path, pageText) => {
    stubDesktopScreen()

    await renderAt(path)

    expect(await screen.findByText(pageText)).toBeTruthy()
    expect(rail()).toBeTruthy()
  })

  it('lists the groups of the saved dashboard under Groups without asking the server for the dashboard', async () => {
    stubDesktopScreen()

    const { fetchMock } = await renderAt(routes.groups, seedDashboard)

    await screen.findByText('groups page')
    const greece = within(rail()).getByRole('link', { name: 'Greece trip' })
    expect(greece.getAttribute('href')).toBe(routes.group(greeceGroupId))
    expect(railLinkNames()).toHaveLength(4 + fullDashboard.groups.length)
    expect(requestCount(fetchMock, 'GET', dashboardPath)).toBe(0)
  })

  it('lists no groups and asks for nothing when the dashboard has not been loaded yet', async () => {
    stubDesktopScreen()

    const { fetchMock } = await renderAt(routes.groups)

    await screen.findByText('groups page')
    expect(railLinkNames()).toHaveLength(4)
    expect(requestCount(fetchMock, 'GET', dashboardPath)).toBe(0)
  })

  it('lays the menu and the page out as two columns from the lg breakpoint', async () => {
    stubDesktopScreen()

    await renderAt(routes.groups)

    const page = await screen.findByText('groups page')
    const column = page.parentElement
    const frame = column?.parentElement
    expect(frame?.className).toContain('lg:grid')
    expect(frame?.className).toContain('lg:grid-cols-[clamp(11rem,17vw,16rem)_minmax(0,1fr)]')
    expect(column?.className).toContain('lg:relative')
    expect(frame?.contains(rail())).toBe(true)
  })
})

describe('DesktopRailLayout on a phone-sized window', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('shows the bottom tab bar and no left menu', async () => {
    await renderAt(routes.groups)

    await screen.findByText('groups page')
    expect(document.querySelector('[data-bottom-bar]')).not.toBeNull()
    expect(document.querySelector('[data-rail-pill]')).toBeNull()
    expect(screen.queryByText(translated('en', 'app.name'))).toBeNull()
    expect(screen.getAllByRole('navigation')).toHaveLength(1)
  })

  it('shows no menu at all on a page without the bottom bar', async () => {
    await renderAt(routes.newGroup)

    await screen.findByText('new group page')
    expect(screen.queryByRole('navigation')).toBeNull()
  })

  it('wraps the page only in boxless elements, so the phone layout does not move', async () => {
    await renderAt(routes.newGroup)

    const page = await screen.findByText('new group page')
    const column = page.parentElement
    const frame = column?.parentElement

    expect(column?.className.split(/\s+/)[0]).toBe('contents')
    expect(frame?.className.split(/\s+/)[0]).toBe('contents')
    expect(column?.className).not.toMatch(/(^|\s)(relative|grid|block)(\s|$)/)
    expect(frame?.className).not.toMatch(/(^|\s)(relative|grid|block)(\s|$)/)
  })
})
