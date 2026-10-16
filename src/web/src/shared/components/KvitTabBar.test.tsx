import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { contrastBetween } from '@/test/indexCssColors'
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

function pill(): HTMLElement {
  const found = screen.getByRole('navigation').querySelector<HTMLElement>('[data-tab-pill]')
  if (found === null) {
    throw new Error('The tab bar has no sliding pill')
  }
  return found
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

  describe('the sliding pill', () => {
    it.each([
      ['/', 'translateX(0%)'],
      ['/groups', 'translateX(100%)'],
      ['/settings', 'translateX(200%)'],
      ['/groups/recently-deleted', 'translateX(100%)'],
    ])('sits under the current tab at %s', async (path, transform) => {
      await renderBarAt(path)

      expect(pill().style.transform).toBe(transform)
    })

    it('is as wide as one of the three tabs inside the padding', async () => {
      await renderBarAt('/groups')
      const oneThirdInsidePadding = document.createElement('span').style
      oneThirdInsidePadding.width = 'calc((100% - 0.5rem) / 3)'

      expect(pill().style.width).toBe(oneThirdInsidePadding.width)
    })

    it('keeps the active looks, the fill and the shadow, on the pill and not on the link', async () => {
      await renderBarAt('/groups')

      expect(pill().classList.contains('bg-tab-active')).toBe(true)
      expect(pill().classList.contains('shadow-[0_1px_3px_var(--track-shadow)]')).toBe(true)
      expect(tab('Groups').className).not.toContain('bg-tab-active')
    })

    it.each(['light', 'dark'] as const)('keeps the text of the current tab at 4.5:1 or more on the pill in %s mode', (theme) => {
      expect(contrastBetween('tab-active-foreground', 'tab-active', theme)).toBeGreaterThanOrEqual(4.5)
    })

    it('makes the dark pill stand out from the bar behind it at 3:1 or more', () => {
      expect(contrastBetween('tab-active', 'card', 'dark')).toBeGreaterThanOrEqual(3)
    })

    it('is hidden from screen readers and lets taps through to the links', async () => {
      await renderBarAt('/groups')

      expect(pill().getAttribute('aria-hidden')).toBe('true')
      expect(pill().classList.contains('pointer-events-none')).toBe(true)
    })

    it('slides by its transform only, in 200 ms with ease-out', async () => {
      await renderBarAt('/groups')

      expect(pill().classList.contains('transition-transform')).toBe(true)
      expect(pill().classList.contains('duration-200')).toBe(true)
      expect(pill().classList.contains('ease-out')).toBe(true)
    })

    it('does not slide for people who prefer reduced motion', async () => {
      await renderBarAt('/groups')

      expect(pill().classList.contains('motion-reduce:transition-none')).toBe(true)
    })

    it('draws the links above the pill, so their text stays readable', async () => {
      await renderBarAt('/groups')

      for (const item of items) {
        expect(tab(item.label).classList.contains('relative')).toBe(true)
      }
    })

    it('shows no pill and no current link on a page that is not one of the tabs', async () => {
      await renderBarAt('/somewhere-else')

      expect(screen.getByRole('navigation').querySelector('[data-tab-pill]')).toBeNull()
      for (const item of items) {
        expect(tab(item.label).getAttribute('aria-current')).toBeNull()
      }
    })
  })

  it('goes to the page of a link when it is pressed', async () => {
    const { router } = await renderBarAt('/groups')

    fireEvent.click(tab('Settings'))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe('/settings')
    })
  })
})
