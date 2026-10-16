import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import {
  dinnerBillDashboardGroup,
  dinnerBillGroupId,
  flatDashboardGroup,
  flatGroupId,
  greeceDashboardGroup,
  greeceGroupId,
  longMacedonianGroupName,
  longNameGroupId,
} from '@/test/dashboardTestData'
import { renderHome, textMatching } from '@/test/homeTestHelpers'
import { translated } from '@/test/translated'
import { formatSignedMoney } from '@/shared/utils/formatSignedMoney'

async function findGrid(language: Language = 'en'): Promise<HTMLElement> {
  await screen.findByRole('heading', { name: translated(language, 'home.groups') })
  const grid = document.querySelector<HTMLElement>('ul.grid-cols-2')
  if (grid === null) {
    throw new Error('Found no list with the class grid-cols-2 for the group cards')
  }
  return grid
}

function cardIn(grid: HTMLElement, name: string): HTMLElement {
  const nameElement = within(grid).getByText(textMatching(name))
  const card = nameElement.closest('a')
  if (card === null) {
    throw new Error(`The group name "${name}" is not inside a link`)
  }
  return card
}

describe('the group cards of the home screen', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('has the heading "Your groups" in %s', async (language) => {
    await renderHome({ language })

    expect(await screen.findByRole('heading', { name: translated(language, 'home.groups') })).toBeTruthy()
  })

  it('lays the cards out in two columns', async () => {
    await renderHome()
    const grid = await findGrid()

    expect(grid.classList.contains('grid')).toBe(true)
    expect(grid.classList.contains('grid-cols-2')).toBe(true)
  })

  it('shows one card per group, in the order the server sent them', async () => {
    await renderHome()
    const grid = await findGrid()

    const cardTexts = within(grid)
      .getAllByRole('link')
      .map((link) => link.textContent ?? '')

    expect(cardTexts).toHaveLength(4)
    expect(cardTexts[0]).toContain('Greece trip')
    expect(cardTexts[1]).toContain('Flat')
    expect(cardTexts[2]).toContain(longMacedonianGroupName)
    expect(cardTexts[3]).toContain('Dinner')
  })

  it.each([
    [greeceDashboardGroup.name, greeceDashboardGroup.emoji],
    [flatDashboardGroup.name, flatDashboardGroup.emoji],
    [dinnerBillDashboardGroup.name, dinnerBillDashboardGroup.emoji],
  ])('shows the emoji of %s on its card', async (name, emoji) => {
    await renderHome()
    const grid = await findGrid()

    expect(cardIn(grid, name).textContent).toContain(emoji)
  })

  it.each([
    ['Greece trip', greeceGroupId],
    ['Flat', flatGroupId],
    ['Dinner', dinnerBillGroupId],
    [longMacedonianGroupName, longNameGroupId],
  ])('links the card of %s to its group, a One bill like any group', async (name, groupId) => {
    await renderHome()
    const grid = await findGrid()

    expect(cardIn(grid, name).getAttribute('href')).toBe(routes.group(groupId))
  })

  it.each([
    ['Greece trip', greeceGroupId],
    ['Dinner', dinnerBillGroupId],
  ])('opens %s when its card is tapped', async (name, groupId) => {
    const { router } = await renderHome()
    const grid = await findGrid()

    fireEvent.click(cardIn(grid, name))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.group(groupId))
    })
  })

  describe('balances', () => {
    it.each(languages)('shows a balance the person is owed with a plus sign in the positive colour in %s', async (language) => {
      await renderHome({ language })
      const card = cardIn(await findGrid(language), 'Greece trip')

      const mkd = within(card).getByText(textMatching(formatSignedMoney(185000, 'MKD', language)))
      const eur = within(card).getByText(textMatching(formatSignedMoney(2000, 'EUR', language)))

      expect(mkd.classList.contains('text-positive')).toBe(true)
      expect(eur.classList.contains('text-positive')).toBe(true)
    })

    it.each(languages)('shows a balance the person owes with a minus sign in the negative colour in %s', async (language) => {
      await renderHome({ language })
      const card = cardIn(await findGrid(language), 'Flat')

      const owes = within(card).getByText(textMatching(formatSignedMoney(-60000, 'MKD', language)))

      expect(owes.classList.contains('text-negative')).toBe(true)
      expect(owes.classList.contains('text-positive')).toBe(false)
    })

    it('keeps MKD and EUR in separate elements inside one card', async () => {
      await renderHome()
      const card = cardIn(await findGrid(), 'Greece trip')

      const mkd = within(card).getByText(textMatching(formatSignedMoney(185000, 'MKD', 'en')))
      const eur = within(card).getByText(textMatching(formatSignedMoney(2000, 'EUR', 'en')))

      expect(mkd).not.toBe(eur)
      expect(mkd.contains(eur)).toBe(false)
      expect(eur.contains(mkd)).toBe(false)
    })

    it.each(languages)('shows a kvit chip instead of amounts for a group with no balance in %s', async (language) => {
      await renderHome({ language })
      const grid = await findGrid(language)

      expect(within(cardIn(grid, 'Dinner')).getByText(translated(language, 'home.kvit'))).toBeTruthy()
      expect(within(cardIn(grid, longMacedonianGroupName)).getByText(translated(language, 'home.kvit'))).toBeTruthy()
    })

    it('shows no kvit chip on a card that has a balance', async () => {
      await renderHome()
      const grid = await findGrid()

      expect(within(cardIn(grid, 'Greece trip')).queryByText(translated('en', 'home.kvit'))).toBeNull()
      expect(within(cardIn(grid, 'Flat')).queryByText(translated('en', 'home.kvit'))).toBeNull()
    })

    it('shows no amount at all on the card of a group with no balance', async () => {
      await renderHome()
      const grid = await findGrid()

      expect(cardIn(grid, 'Dinner').textContent).not.toMatch(/\d/)
    })
  })

  describe('long names', () => {
    it('lets a long Macedonian name break inside the word instead of pushing the card wider', async () => {
      await renderHome({ language: 'mk' })
      const grid = await findGrid('mk')
      const nameElement = within(grid).getByText(textMatching(longMacedonianGroupName))

      const wraps = nameElement.classList.contains('break-words') || nameElement.classList.contains('wrap-anywhere')

      expect(wraps, `the name element must carry break-words or wrap-anywhere, its classes are: ${nameElement.className}`).toBe(true)
    })

    it('lets the card shrink inside its grid cell with min-w-0', async () => {
      await renderHome()
      const grid = await findGrid()
      const card = cardIn(grid, longMacedonianGroupName)
      const cardAndCell = [card, card.parentElement as HTMLElement]

      expect(cardAndCell.some((element) => element.classList.contains('min-w-0'))).toBe(true)
    })

    it('does not cut the name with an ellipsis, so the whole name stays readable', async () => {
      await renderHome()
      const grid = await findGrid()
      const nameElement = within(grid).getByText(textMatching(longMacedonianGroupName))

      expect(nameElement.classList.contains('truncate')).toBe(false)
      expect(nameElement.textContent).toBe(longMacedonianGroupName)
    })
  })
})
