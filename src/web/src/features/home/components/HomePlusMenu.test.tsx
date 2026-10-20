import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import { greeceGroupId } from '@/test/dashboardTestData'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { seedMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { HomePlusMenu } from './HomePlusMenu'

const target = { groupId: greeceGroupId, groupName: 'Greece trip' }

async function renderMenu(language: Language = 'en', menuTarget: typeof target | null = target) {
  return renderElementWithProviders(
    <div>
      <p>outside the menu</p>
      <HomePlusMenu target={menuTarget} />
    </div>,
    routes.dashboard,
    { language, seedCache: seedMe({ ...testMe, language }) },
  )
}

function plusButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'home.plus') })
}

function openMenu(language: Language = 'en'): HTMLElement {
  fireEvent.click(plusButton(language))
  return screen.getByRole('menu')
}

function expectItemsInOrder(menu: HTMLElement, names: string[]): void {
  const items = within(menu).getAllByRole('menuitem')
  expect(items).toHaveLength(names.length)
  names.forEach((name, index) => {
    expect(within(menu).getByRole('menuitem', { name })).toBe(items[index])
  })
}

function expectedItems(language: Language, withTarget: boolean): string[] {
  const items = [
    translated(language, 'home.plusNewOneBill'),
    translated(language, 'home.plusAnotherGroup'),
    translated(language, 'budget.entries.homeItem'),
  ]
  return withTarget ? [translated(language, 'home.plusAddTo', { group: 'Greece trip' }), ...items] : items
}

function tapOutside(): void {
  const outside = screen.getByText('outside the menu')
  fireEvent.pointerDown(outside)
  fireEvent.mouseDown(outside)
  fireEvent.pointerUp(outside)
  fireEvent.mouseUp(outside)
  fireEvent.click(outside)
}

describe('HomePlusMenu', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  describe('the button', () => {
    it.each(languages)('is named by the text Add in %s and shows a plus', async (language) => {
      await renderMenu(language)

      expect(plusButton(language).textContent).toContain('+')
    })

    it('starts closed with aria-expanded false and says it opens a menu', async () => {
      await renderMenu()

      expect(plusButton().getAttribute('aria-expanded')).toBe('false')
      expect(plusButton().getAttribute('aria-haspopup')).toBe('menu')
    })

    it('shows no menu before it is tapped', async () => {
      await renderMenu()

      expect(screen.queryByRole('menu')).toBeNull()
      expect(screen.queryByRole('menuitem')).toBeNull()
    })

    it('shows a × instead of the + and aria-expanded true when the menu is open', async () => {
      await renderMenu()

      openMenu()

      expect(plusButton().getAttribute('aria-expanded')).toBe('true')
      expect(plusButton().textContent).toContain('×')
      expect(plusButton().textContent).not.toContain('+')
    })

    it('closes the menu when it is tapped a second time and shows the + again', async () => {
      await renderMenu()
      openMenu()

      fireEvent.click(plusButton())

      expect(screen.queryByRole('menu')).toBeNull()
      expect(plusButton().textContent).toContain('+')
      expect(plusButton().getAttribute('aria-expanded')).toBe('false')
    })
  })

  describe('the list', () => {
    it.each(languages)('has exactly four items in the order Add to the group, New One bill, Another group, Personal spending in %s', async (language) => {
      await renderMenu(language)

      const menu = openMenu(language)

      expectItemsInOrder(menu, expectedItems(language, true))
    })

    it.each(languages)('has no Add to item, and so three items, when there is no target in %s', async (language) => {
      await renderMenu(language, null)

      const menu = openMenu(language)

      expectItemsInOrder(menu, expectedItems(language, false))
    })

    it.each(languages)('has no New group item, because a group is made only in the Groups section, in %s', async (language) => {
      await renderMenu(language)

      const menu = openMenu(language)

      expect(within(menu).queryByRole('menuitem', { name: translated(language, 'groups.newButton') })).toBeNull()
    })

    it('has no item that links to the New group screens', async () => {
      await renderMenu()

      const menu = openMenu()

      const addresses = within(menu)
        .getAllByRole('menuitem')
        .map((item) => item.getAttribute('href'))
      expect(addresses).not.toContain(routes.newGroup)
      expect(addresses).not.toContain(routes.newGroupGroup)
    })

    it('names the group of the target in the first item', async () => {
      await renderMenu('en', { groupId: greeceGroupId, groupName: 'Flat 4B' })

      const menu = openMenu()

      expect(within(menu).getAllByRole('menuitem')[0]).toBe(
        within(menu).getByRole('menuitem', { name: translated('en', 'home.plusAddTo', { group: 'Flat 4B' }) }),
      )
    })

    it('gives every item a tap height of at least 44 px', async () => {
      await renderMenu()
      const menu = openMenu()

      for (const item of within(menu).getAllByRole('menuitem')) {
        const tallEnough = item.classList.contains('min-h-11') || item.classList.contains('min-h-12')
        expect(tallEnough, `the item "${item.textContent}" needs min-h-11 or min-h-12, its classes are: ${item.className}`).toBe(true)
      }
    })

    it('is placed above the bottom bar, using the bottom bar height variable, so it never covers the bar', async () => {
      await renderMenu()
      const menu = openMenu()

      const wrappers: Element[] = []
      for (let node: Element | null = menu; node !== null && node !== document.body; node = node.parentElement) {
        wrappers.push(node)
      }

      expect(wrappers.some((element) => element.className.includes('--bottom-bar-height'))).toBe(true)
    })
  })

  describe('choosing an item', () => {
    it('opens Add expense of the target group with Add to', async () => {
      const { router } = await renderMenu()
      const menu = openMenu()

      fireEvent.click(within(menu).getByRole('menuitem', { name: translated('en', 'home.plusAddTo', { group: 'Greece trip' }) }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groupExpenseNew(greeceGroupId))
      })
    })

    it('opens the One bill screen with New One bill, because that address already exists', async () => {
      const { router } = await renderMenu()
      const menu = openMenu()

      fireEvent.click(within(menu).getByRole('menuitem', { name: translated('en', 'home.plusNewOneBill') }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.newBill)
      })
    })

    it.each([0, 1, 2])('never opens a New group screen when item number %i is tapped', async (index) => {
      const { router } = await renderMenu()
      const menu = openMenu()

      fireEvent.click(within(menu).getAllByRole('menuitem')[index])

      await waitFor(() => {
        expect(router.state.location.pathname).not.toBe(routes.dashboard)
      })
      expect(router.state.location.pathname).not.toBe(routes.newGroup)
      expect(router.state.location.pathname).not.toBe(routes.newGroupGroup)
    })

    it('opens the Groups list with Another group', async () => {
      const { router } = await renderMenu()
      const menu = openMenu()

      fireEvent.click(within(menu).getByRole('menuitem', { name: translated('en', 'home.plusAnotherGroup') }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groups)
      })
    })

    it('opens New One bill without a target too', async () => {
      const { router } = await renderMenu('en', null)
      const menu = openMenu()

      fireEvent.click(within(menu).getByRole('menuitem', { name: translated('en', 'home.plusNewOneBill') }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.newBill)
      })
    })
  })

  describe('closing without choosing', () => {
    it('closes when Escape is pressed', async () => {
      await renderMenu()
      openMenu()

      fireEvent.keyDown(plusButton(), { key: 'Escape' })

      expect(screen.queryByRole('menu')).toBeNull()
      expect(plusButton().getAttribute('aria-expanded')).toBe('false')
      expect(plusButton().textContent).toContain('+')
    })

    it('closes when something outside the menu is tapped', async () => {
      await renderMenu()
      openMenu()

      tapOutside()

      expect(screen.queryByRole('menu')).toBeNull()
      expect(plusButton().getAttribute('aria-expanded')).toBe('false')
    })

    it('stays open when an item of the menu is only focused or hovered', async () => {
      await renderMenu()
      const menu = openMenu()

      fireEvent.pointerEnter(within(menu).getAllByRole('menuitem')[1])
      fireEvent.focus(within(menu).getAllByRole('menuitem')[1])

      expect(screen.getByRole('menu')).toBeTruthy()
    })

    it('can be opened again after it was closed with Escape', async () => {
      await renderMenu()
      openMenu()
      fireEvent.keyDown(plusButton(), { key: 'Escape' })

      const menu = openMenu()

      expectItemsInOrder(menu, expectedItems('en', true))
    })

    it('does nothing when Escape is pressed while the menu is closed', async () => {
      await renderMenu()

      fireEvent.keyDown(plusButton(), { key: 'Escape' })

      expect(screen.queryByRole('menu')).toBeNull()
      expect(plusButton().getAttribute('aria-expanded')).toBe('false')
    })
  })
})
