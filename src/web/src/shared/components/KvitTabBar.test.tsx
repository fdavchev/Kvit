import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { KvitTabBar } from './KvitTabBar'

const items = [
  { to: '/', label: 'Home', icon: <svg data-testid="icon-home" /> },
  { to: '/groups', label: 'Groups', icon: <svg data-testid="icon-groups" /> },
  { to: '/settings', label: 'Settings', icon: <svg data-testid="icon-settings" /> },
]

async function renderBarAt(path: string) {
  return renderElementWithProviders(<KvitTabBar items={items} />, path)
}

function tab(label: string): HTMLElement {
  return screen.getByRole('link', { name: label })
}

describe('KvitTabBar', () => {
  it('is a navigation landmark', async () => {
    await renderBarAt('/groups')

    expect(screen.getByRole('navigation')).toBeTruthy()
  })

  it('shows one link per item inside the navigation, in the given order', async () => {
    await renderBarAt('/groups')

    const links = within(screen.getByRole('navigation')).getAllByRole('link')

    expect(links.map((link) => link.textContent)).toEqual(['Home', 'Groups', 'Settings'])
  })

  it.each([
    ['Home', '/'],
    ['Groups', '/groups'],
    ['Settings', '/settings'],
  ])('sends the %s link to %s', async (label, path) => {
    await renderBarAt('/groups')

    expect(tab(label).getAttribute('href')).toBe(path)
  })

  it.each([
    ['Home', 'icon-home'],
    ['Groups', 'icon-groups'],
    ['Settings', 'icon-settings'],
  ])('draws the icon of the %s item inside its link', async (label, iconId) => {
    await renderBarAt('/groups')

    expect(within(tab(label)).getByTestId(iconId)).toBeTruthy()
  })

  it.each([
    ['/', 'Home'],
    ['/groups', 'Groups'],
    ['/settings', 'Settings'],
  ])('marks only the link of the current page %s as current: %s', async (path, current) => {
    await renderBarAt(path)

    for (const item of items) {
      expect(tab(item.label).getAttribute('aria-current')).toBe(
        item.label === current ? 'page' : null,
      )
    }
  })

  it('keeps the Groups link current on a page below /groups', async () => {
    await renderBarAt('/groups/recently-deleted')

    expect(tab('Groups').getAttribute('aria-current')).toBe('page')
    expect(tab('Home').getAttribute('aria-current')).toBeNull()
  })

  it('does not mark the Home link current on a page below /', async () => {
    await renderBarAt('/groups/recently-deleted')

    expect(tab('Home').getAttribute('aria-current')).toBeNull()
  })

  it('goes to the page of a link when it is pressed', async () => {
    const { router } = await renderBarAt('/groups')

    fireEvent.click(tab('Settings'))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe('/settings')
    })
  })
})
