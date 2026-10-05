import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import type { RouteObject } from 'react-router'
import { describe, expect, it } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { translated } from '@/test/translated'
import { BottomBarLayout } from './BottomBarLayout'
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
