import { screen, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import type { PersonalCategory } from '@/core/services/personalCategories/personalCategoriesService'
import {
  amountAt,
  amountLinesInOrder,
  amountTexts,
  classesOf,
  findLeftCard,
  incomeMoney,
  leftAttribute,
  money,
  readableText,
} from '@/test/budgetLeftCardTestHelpers'
import { octoberIncomeAndTwoSpendings, octoberOverspent } from '@/test/budgetLeftTestData'
import { budgetNow, categoryTotalsRegion, findTotalCard, renderBudget } from '@/test/budgetTestHelpers'
import {
  dinnerShare,
  euroTaxiShare,
  groupSharesOnlyOctober,
  hotelShare,
  octoberWithGroupShares,
  untitledGroceriesShare,
} from '@/test/groupShareTestData'
import { coffeeCategory, personalFoodCategory, personalGroceriesCategory } from '@/test/personalCategoryTestData'
import { personalCategoryLabel } from '@/test/personalCategoryTestHelpers'
import {
  euroIncomeOnlyOctober,
  incomeOnlyOctober,
  mkdOnlyMonth,
} from '@/test/personalEntryTestData'
import { currencyLabel } from '@/shared/utils/formatMoney'
import { translated } from '@/test/translated'

function tilesOf(group: HTMLElement): string[] {
  return within(group).getAllByRole('listitem').map(readableText)
}

function tile(category: PersonalCategory, amount: string, language: Language): string {
  return `${personalCategoryLabel(language, category)}${amount}`
}

describe('BudgetScreen, the totals with group shares', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(budgetNow)
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('the "Spent this month" card', () => {
    it.each(languages)('adds the group shares of each currency to the spending of that currency, MKD and EUR apart (%s)', async (language) => {
      await renderBudget({ language, months: [octoberWithGroupShares] })

      const card = await findTotalCard(language)

      expect(amountTexts(card, leftAttribute)).toEqual([money(629000, 'MKD', language), money(2050, 'EUR', language)])
    })

    it('does not count a share twice: the personal totals of the answer do not include group shares', async () => {
      await renderBudget({ months: [{ ...mkdOnlyMonth, groupShares: [hotelShare] }] })

      const card = await findTotalCard()

      expect(amountTexts(card, leftAttribute)).toEqual([money(150400 + 480000, 'MKD')])
    })

    it('shows a currency that has only group shares, after the currencies that have personal spending in order MKD then EUR', async () => {
      await renderBudget({ months: [{ ...mkdOnlyMonth, groupShares: [euroTaxiShare] }] })

      const card = await findTotalCard()

      expect(amountTexts(card, leftAttribute)).toEqual([money(150400, 'MKD'), money(800, 'EUR')])
    })

    it('shows the group shares of a month without personal spending, MKD first', async () => {
      await renderBudget({ months: [groupSharesOnlyOctober] })

      const card = await findTotalCard()

      expect(amountTexts(card, leftAttribute)).toEqual([money(575000, 'MKD'), money(800, 'EUR')])
    })

    it('keeps the first amount big and the second a smaller line', async () => {
      await renderBudget({ months: [groupSharesOnlyOctober] })

      const card = await findTotalCard()

      expect(card.querySelector('[data-total-amount="main"]')?.textContent).toContain('5,750')
      expect(card.querySelector('[data-total-amount="second"]')?.textContent).toContain('8.00')
    })

    it('adds a share of a currency to the spending of the same currency only', async () => {
      await renderBudget({ months: [{ ...mkdOnlyMonth, groupShares: [dinnerShare, euroTaxiShare] }] })

      const card = await findTotalCard()

      expect(amountTexts(card, leftAttribute)).toEqual([money(150400 + 95000, 'MKD'), money(800, 'EUR')])
    })
  })

  describe('the "Left this month" card', () => {
    it.each(languages)('takes the group shares of a currency away from the income of that currency, and counts them in Spent (%s)', async (language) => {
      await renderBudget({ language, months: [{ ...octoberIncomeAndTwoSpendings, groupShares: [dinnerShare] }] })

      const card = await findLeftCard(language)

      expect(amountLinesInOrder(card)).toEqual([
        `left:${money(4500000 - 1240000 - 95000, 'MKD', language)}`,
        `income:${incomeMoney(4500000, 'MKD', language)}`,
        `spent:${money(1240000 + 95000, 'MKD', language)}`,
        `spent:${money(5000, 'EUR', language)}`,
      ])
    })

    it('counts a share in a currency without income as a plain Spent line, and a share in the currency of the income as Spent there', async () => {
      await renderBudget({ months: [{ ...incomeOnlyOctober, groupShares: [dinnerShare, euroTaxiShare] }] })

      const card = await findLeftCard()

      expect(amountLinesInOrder(card)).toEqual([
        `left:${money(6500000 - 95000, 'MKD')}`,
        `income:${incomeMoney(6500000, 'MKD')}`,
        `spent:${money(95000, 'MKD')}`,
        `spent:${money(800, 'EUR')}`,
      ])
    })

    it('shows the card with the Left of the income when the only spending is a group share in the other currency', async () => {
      await renderBudget({ months: [{ ...euroIncomeOnlyOctober, groupShares: [dinnerShare] }] })

      const card = await findLeftCard()

      expect(amountLinesInOrder(card)).toEqual([
        `left:${money(20000, 'EUR')}`,
        `income:${incomeMoney(20000, 'EUR')}`,
        `spent:${money(0, 'EUR')}`,
        `spent:${money(95000, 'MKD')}`,
      ])
    })

    it('can go negative because of group shares, in the negative look', async () => {
      await renderBudget({ months: [{ ...octoberOverspent, groupShares: [dinnerShare] }] })

      const card = await findLeftCard()
      const left = amountAt(card, leftAttribute, 0)

      expect(readableText(left)).toBe(money(1000000 - 1240000 - 95000, 'MKD'))
      expect(classesOf(left)).toContain('text-negative')
    })
  })

  describe('the category tiles', () => {
    it.each(languages)('adds the shares to the tile of their category and gives a category with shares only its own tile, in the order of the categories (%s)', async (language) => {
      await renderBudget({ language, months: [octoberWithGroupShares] })
      await findTotalCard(language)

      const region = categoryTotalsRegion(language)
      const mkdGroup = within(region).getByRole('group', { name: currencyLabel('MKD', language) })
      const eurGroup = within(region).getByRole('group', { name: currencyLabel('EUR', language) })

      expect(tilesOf(mkdGroup)).toEqual([
        tile(personalFoodCategory, money(95000, 'MKD', language), language),
        tile(personalGroceriesCategory, money(30000, 'MKD', language), language),
        tile(coffeeCategory, money(12000, 'MKD', language), language),
      ])
      expect(tilesOf(eurGroup)).toEqual([tile(personalFoodCategory, money(2050, 'EUR', language), language)])
    })

    it('adds a share to the personal spending of the same category and currency', async () => {
      await renderBudget({ months: [{ ...mkdOnlyMonth, groupShares: [untitledGroceriesShare] }] })
      await findTotalCard()

      const region = categoryTotalsRegion()

      expect(within(region).getAllByRole('listitem').map(readableText)).toEqual([
        tile(personalGroceriesCategory, money(123400 + 30000, 'MKD'), 'en'),
        tile(coffeeCategory, money(27000, 'MKD'), 'en'),
      ])
    })

    it('counts a share with no category only in the card, not in any tile', async () => {
      await renderBudget({ months: [{ ...mkdOnlyMonth, groupShares: [hotelShare] }] })
      const card = await findTotalCard()

      const region = categoryTotalsRegion()

      expect(amountTexts(card, leftAttribute)).toEqual([money(150400 + 480000, 'MKD')])
      expect(within(region).getAllByRole('listitem').map(readableText)).toEqual([
        tile(personalGroceriesCategory, money(123400, 'MKD'), 'en'),
        tile(coffeeCategory, money(27000, 'MKD'), 'en'),
      ])
    })

    it('shows a grid for each currency, with its label, once a share brings the second currency', async () => {
      await renderBudget({ months: [{ ...mkdOnlyMonth, groupShares: [euroTaxiShare] }] })
      await findTotalCard()

      const region = categoryTotalsRegion()
      const eurGroup = within(region).getByRole('group', { name: currencyLabel('EUR', 'en') })

      expect(within(region).getAllByRole('group')).toHaveLength(2)
      expect(tilesOf(eurGroup)).toEqual([tile(personalFoodCategory, money(800, 'EUR'), 'en')])
    })

    it('shows one grid without a currency label when shares and spending are all in one currency', async () => {
      await renderBudget({ months: [{ ...mkdOnlyMonth, groupShares: [dinnerShare] }] })
      await findTotalCard()

      const region = categoryTotalsRegion()

      expect(within(region).queryByRole('group')).toBeNull()
      expect(within(region).getAllByRole('listitem')).toHaveLength(3)
    })

    it('shows the tiles of a month with only group shares', async () => {
      await renderBudget({ months: [groupSharesOnlyOctober] })
      await findTotalCard()

      const region = categoryTotalsRegion()
      const mkdGroup = within(region).getByRole('group', { name: currencyLabel('MKD', 'en') })
      const eurGroup = within(region).getByRole('group', { name: currencyLabel('EUR', 'en') })

      expect(tilesOf(mkdGroup)).toEqual([tile(personalFoodCategory, money(95000, 'MKD'), 'en')])
      expect(tilesOf(eurGroup)).toEqual([tile(personalFoodCategory, money(800, 'EUR'), 'en')])
    })

    it('shows no tile for a month whose only shares have no category', async () => {
      await renderBudget({ months: [{ ...groupSharesOnlyOctober, groupShares: [hotelShare] }] })

      const card = await findTotalCard()

      expect(amountTexts(card, leftAttribute)).toEqual([money(480000, 'MKD')])
      expect(screen.queryByRole('region', { name: translated('en', 'budget.screen.categoryTotals') })).toBeNull()
    })

    it('does not make a tile a link or a button, with or without shares', async () => {
      await renderBudget({ months: [octoberWithGroupShares] })
      await findTotalCard()

      const region = categoryTotalsRegion()

      expect(within(region).queryAllByRole('link')).toHaveLength(0)
      expect(within(region).queryAllByRole('button')).toHaveLength(0)
    })
  })
})
