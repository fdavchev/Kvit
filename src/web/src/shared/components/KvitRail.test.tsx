import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { KvitRail, type KvitRailItem } from './KvitRail'

const items: KvitRailItem[] = [
  { to: '/', label: 'Home', icon: <svg data-testid="icon-home" /> },
  {
    to: '/groups',
    label: 'Groups',
    icon: <svg data-testid="icon-groups" />,
    subItems: [
      { to: '/groups/greece', label: 'Greece trip', emoji: '🏖️' },
      { to: '/groups/flat', label: 'Flat', emoji: '🏠' },
    ],
  },
  { to: '/settings', label: 'Settings', icon: <svg data-testid="icon-settings" /> },
]

async function renderRailAt(path: string, railItems: readonly KvitRailItem[] = items) {
  return renderRoutesWithProviders([{ path: '*', element: <KvitRail brand="Kvit" items={railItems} /> }], path)
}

function rail(): HTMLElement {
  return screen.getByRole('navigation')
}

function link(name: string): HTMLElement {
  return within(rail()).getByRole('link', { name })
}

function pill(): HTMLElement {
  const found = rail().querySelector<HTMLElement>('[data-rail-pill]')
  if (found === null) {
    throw new Error('The rail has no sliding pill')
  }
  return found
}

describe('KvitRail', () => {
  it('is a navigation landmark that starts with the brand name', async () => {
    await renderRailAt('/')

    expect(rail().firstElementChild?.textContent).toBe('Kvit')
  })

  it('shows the items in the given order, with each group under Groups', async () => {
    await renderRailAt('/')

    const names = within(rail())
      .getAllByRole('link')
      .map((element) => element.textContent)

    expect(names).toEqual(['Home', 'Groups', '🏖️Greece trip', '🏠Flat', 'Settings'])
  })

  it('puts the group links in the same list entry as the Groups link', async () => {
    await renderRailAt('/')

    const groupsEntry = link('Groups').closest('li')

    expect(groupsEntry?.contains(link('Greece trip'))).toBe(true)
    expect(groupsEntry?.contains(link('Settings'))).toBe(false)
  })

  it.each([
    ['Home', '/'],
    ['Groups', '/groups'],
    ['Settings', '/settings'],
    ['Greece trip', '/groups/greece'],
  ])('sends the %s link to %s', async (name, path) => {
    await renderRailAt('/')

    expect(link(name).getAttribute('href')).toBe(path)
  })

  it.each([
    ['Home', 'icon-home'],
    ['Groups', 'icon-groups'],
    ['Settings', 'icon-settings'],
  ])('draws the icon of the %s item inside its link', async (name, iconId) => {
    await renderRailAt('/')

    expect(within(link(name)).getByTestId(iconId)).toBeTruthy()
  })

  it.each([
    ['/', 'Home'],
    ['/groups', 'Groups'],
    ['/groups/recently-deleted', 'Groups'],
    ['/groups/greece/balances', 'Groups'],
    ['/settings', 'Settings'],
  ])('marks only one main item as the current page at %s: %s', async (path, currentName) => {
    await renderRailAt(path)

    for (const name of ['Home', 'Groups', 'Settings']) {
      expect(link(name).getAttribute('aria-current')).toBe(name === currentName ? 'page' : null)
    }
  })

  it('marks the open group, and only that one, as the current group', async () => {
    await renderRailAt('/groups/greece/activity')

    expect(link('Greece trip').getAttribute('aria-current')).toBe('true')
    expect(link('Flat').getAttribute('aria-current')).toBeNull()
  })

  it('marks no group on the Groups list itself', async () => {
    await renderRailAt('/groups')

    expect(link('Greece trip').getAttribute('aria-current')).toBeNull()
    expect(link('Flat').getAttribute('aria-current')).toBeNull()
  })

  it('shows no group list under Groups when there are no groups', async () => {
    await renderRailAt(
      '/',
      items.map((item) => ({ ...item, subItems: [] })),
    )

    expect(within(rail()).getAllByRole('link')).toHaveLength(3)
    expect(within(rail()).getAllByRole('list')).toHaveLength(1)
  })

  it('draws the current item in the tab colours of the tab bar', async () => {
    await renderRailAt('/groups')

    expect(pill().className).toContain('bg-tab-active')
    expect(link('Groups').className).toContain('aria-[current=page]:text-tab-active-foreground')
  })

  it('keeps the same pill when another item is pressed, so it slides instead of jumping', async () => {
    const { router } = await renderRailAt('/groups')
    const pillBefore = pill()

    fireEvent.click(link('Settings'))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe('/settings')
    })
    expect(pillBefore.isConnected).toBe(true)
    expect(pill()).toBe(pillBefore)
    expect(link('Settings').getAttribute('aria-current')).toBe('page')
  })

  it('places the pill before it lets it slide, so it does not fly in when the rail first appears', async () => {
    await renderRailAt('/settings')

    expect(pill().style.translate).toBe('0 0px')
    expect(pill().hasAttribute('data-sliding')).toBe(true)
  })

  it('slides the pill only for people who allow motion', async () => {
    await renderRailAt('/groups')

    const classes = pill().className.split(/\s+/)
    const slidingClasses = classes.filter((name) => name.includes('transition'))

    expect(slidingClasses).toEqual(['motion-safe:data-sliding:transition-[translate,height]'])
  })

  it('hides the pill when no item matches the page', async () => {
    await renderRailAt('/change-password')

    expect(rail().querySelector('[data-rail-pill]')).toBeNull()
    for (const name of ['Home', 'Groups', 'Settings']) {
      expect(link(name).getAttribute('aria-current')).toBeNull()
    }
  })

  it('stays on screen while the page scrolls and scrolls by itself when the group list is long', async () => {
    await renderRailAt('/')

    const classes = rail().className.split(/\s+/)

    expect(classes).toEqual(expect.arrayContaining(['sticky', 'top-0', 'h-dvh', 'overflow-y-auto']))
  })
})
