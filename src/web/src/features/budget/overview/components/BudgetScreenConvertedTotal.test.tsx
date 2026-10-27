import { screen, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { currencyLabel, type Currency } from '@/shared/utils/formatMoney'
import {
  amountAt,
  amountElements,
  amountLinesInOrder,
  amountTexts,
  classesOf,
  findLeftCard,
  incomeMoney,
  leftAttribute,
  money,
  readableText,
} from '@/test/budgetLeftCardTestHelpers'
import { budgetNow, categoryTotalsRegion, findEntryList, findTotalCard, renderBudget } from '@/test/budgetTestHelpers'
import { findGroupSharesBlock, shareRowText, shareRows } from '@/test/budgetGroupShareTestHelpers'
import {
  convertedToEur,
  convertedToMkd,
  dinnerShare,
  euroOnlyOctoberWithShareConvertedToEur,
  euroOnlyOctoberWithShareConvertedToMkd,
  euroTaxiShare,
  hotelShare,
  groupSharesOnlyOctober,
  mkdOnlyWithSharesConvertedToEur,
  mkdOnlyWithSharesConvertedToMkd,
  octoberWithEuroIncomeOnlyOtherCurrency,
  octoberWithGroupShares,
  octoberWithGroupSharesConvertedToEur,
  octoberWithGroupSharesConvertedToMkd,
  octoberWithSalaryAndGroupSharesConvertedToMkd,
  octoberWithSalaryAndGroupSharesOverspentInMkd,
} from '@/test/groupShareTestData'
import { testMe } from '@/test/testMe'
import { translated } from '@/test/translated'

function meWithTotalIn(budgetTotalCurrency: Currency | null) {
  return { ...testMe, budgetTotalCurrency }
}

describe('BudgetScreen, the converted total', () => {
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

  describe('a month without income', () => {
    it.each(languages)('shows one amount in MKD, the spending plus the group shares, with the label "in MKD" (%s)', async (language) => {
      await renderBudget({ language, me: meWithTotalIn('MKD'), months: [octoberWithGroupSharesConvertedToMkd] })

      const card = await findTotalCard(language)

      expect(amountTexts(card, leftAttribute)).toEqual([
        money(convertedToMkd.spendingMinor + convertedToMkd.groupSharesMinor, 'MKD', language),
      ])
      expect(within(card).getByText(translated(language, 'budget.card.inMkd'))).toBeTruthy()
    })

    it.each(languages)('shows one amount in EUR with the label "in EUR" when the setting is EUR (%s)', async (language) => {
      await renderBudget({ language, me: meWithTotalIn('EUR'), months: [octoberWithGroupSharesConvertedToEur] })

      const card = await findTotalCard(language)

      expect(amountTexts(card, leftAttribute)).toEqual([
        money(convertedToEur.spendingMinor + convertedToEur.groupSharesMinor, 'EUR', language),
      ])
      expect(within(card).getByText(translated(language, 'budget.card.inEur'))).toBeTruthy()
      expect(within(card).queryByText(translated(language, 'budget.card.inMkd'))).toBeNull()
    })

    it('shows the amount as the one big amount and no second line', async () => {
      await renderBudget({ me: meWithTotalIn('MKD'), months: [octoberWithGroupSharesConvertedToMkd] })

      const card = await findTotalCard()

      expect(card.querySelectorAll('[data-total-amount]')).toHaveLength(1)
      expect(card.querySelector('[data-total-amount="main"]')).not.toBeNull()
      expect(card.querySelector('[data-total-amount="second"]')).toBeNull()
    })

    it('shows no amount in euro and none of the per-currency totals of the unconverted card', async () => {
      await renderBudget({ me: meWithTotalIn('MKD'), months: [octoberWithGroupSharesConvertedToMkd] })

      const card = await findTotalCard()

      expect(readableText(card)).not.toContain('€')
      expect(readableText(card)).not.toContain(money(629000, 'MKD'))
    })

    it('shows the converted total of a month with only group shares', async () => {
      const month = {
        ...groupSharesOnlyOctober,
        convertedTotals: { ...convertedToMkd, spendingMinor: 0, groupSharesMinor: 624200, leftMinor: -624200 },
      }
      await renderBudget({ me: meWithTotalIn('MKD'), months: [month] })

      const card = await findTotalCard()

      expect(amountTexts(card, leftAttribute)).toEqual([money(624200, 'MKD')])
      expect(screen.queryByText(translated('en', 'budget.screen.emptyTitle'))).toBeNull()
    })
  })

  describe('a month with income', () => {
    it.each(languages)('shows Left, Income and Spent from the converted totals, all in one currency (%s)', async (language) => {
      await renderBudget({
        language,
        me: meWithTotalIn('MKD'),
        months: [octoberWithSalaryAndGroupSharesConvertedToMkd],
      })

      const card = await findLeftCard(language)

      expect(amountLinesInOrder(card)).toEqual([
        `left:${money(5744900, 'MKD', language)}`,
        `income:${incomeMoney(6500000, 'MKD', language)}`,
        `spent:${money(88900 + 666200, 'MKD', language)}`,
      ])
      expect(within(card).getByText(translated(language, 'budget.card.inMkd'))).toBeTruthy()
    })

    it('shows a negative Left in the negative look', async () => {
      await renderBudget({
        me: meWithTotalIn('MKD'),
        months: [octoberWithSalaryAndGroupSharesOverspentInMkd],
      })

      const card = await findLeftCard()
      const left = amountAt(card, leftAttribute, 0)

      expect(readableText(left)).toBe(money(-255100, 'MKD'))
      expect(classesOf(left)).toContain('text-negative')
    })

    it('shows no second Left, Income or Spent block for the other currency', async () => {
      await renderBudget({
        me: meWithTotalIn('MKD'),
        months: [octoberWithSalaryAndGroupSharesConvertedToMkd],
      })

      const card = await findLeftCard()

      expect(amountElements(card, leftAttribute)).toHaveLength(1)
      expect(amountElements(card, 'data-income-amount')).toHaveLength(1)
      expect(amountElements(card, 'data-spent-amount')).toHaveLength(1)
    })
  })

  describe('the line "includes converted amounts"', () => {
    it.each(languages)('says "includes converted euro amounts" for an MKD total when the month has an item in euro (%s)', async (language) => {
      await renderBudget({ language, me: meWithTotalIn('MKD'), months: [octoberWithGroupSharesConvertedToMkd] })

      const card = await findTotalCard(language)

      expect(within(card).getByText(translated(language, 'budget.card.includesConvertedEuro'))).toBeTruthy()
    })

    it.each(languages)('says "includes converted denar amounts" for a EUR total when the month has an item in denars (%s)', async (language) => {
      await renderBudget({ language, me: meWithTotalIn('EUR'), months: [octoberWithGroupSharesConvertedToEur] })

      const card = await findTotalCard(language)

      expect(within(card).getByText(translated(language, 'budget.card.includesConvertedDenars'))).toBeTruthy()
    })

    it('is on the card with income too', async () => {
      await renderBudget({
        me: meWithTotalIn('MKD'),
        months: [octoberWithSalaryAndGroupSharesConvertedToMkd],
      })

      const card = await findLeftCard()

      expect(within(card).getByText(translated('en', 'budget.card.includesConvertedEuro'))).toBeTruthy()
    })

    it('counts a group share in euro as an item in euro, even when the personal entries are all in denars', async () => {
      const month = { ...mkdOnlyWithSharesConvertedToMkd, groupShares: [hotelShare, dinnerShare, euroTaxiShare] }
      await renderBudget({ me: meWithTotalIn('MKD'), months: [month] })

      const card = await findTotalCard()

      expect(within(card).getByText(translated('en', 'budget.card.includesConvertedEuro'))).toBeTruthy()
    })

    it('counts an income in the other currency as an item in the other currency', async () => {
      await renderBudget({ me: meWithTotalIn('MKD'), months: [octoberWithEuroIncomeOnlyOtherCurrency] })

      const card = await findLeftCard()

      expect(within(card).getByText(translated('en', 'budget.card.includesConvertedEuro'))).toBeTruthy()
    })

    it('is left out of an MKD total when every item of the month is in denars', async () => {
      await renderBudget({ me: meWithTotalIn('MKD'), months: [mkdOnlyWithSharesConvertedToMkd] })

      const card = await findTotalCard()

      expect(within(card).getByText(translated('en', 'budget.card.inMkd'))).toBeTruthy()
      expect(within(card).queryByText(translated('en', 'budget.card.includesConvertedEuro'))).toBeNull()
      expect(within(card).queryByText(translated('en', 'budget.card.includesConvertedDenars'))).toBeNull()
    })

    it('is left out of a EUR total when every item of the month is in euro', async () => {
      await renderBudget({ me: meWithTotalIn('EUR'), months: [euroOnlyOctoberWithShareConvertedToEur] })

      const card = await findTotalCard()

      expect(within(card).getByText(translated('en', 'budget.card.inEur'))).toBeTruthy()
      expect(within(card).queryByText(translated('en', 'budget.card.includesConvertedEuro'))).toBeNull()
      expect(within(card).queryByText(translated('en', 'budget.card.includesConvertedDenars'))).toBeNull()
    })

    it('says "includes converted euro amounts" for an MKD total of a month in euro only', async () => {
      await renderBudget({ me: meWithTotalIn('MKD'), months: [euroOnlyOctoberWithShareConvertedToMkd] })

      const card = await findTotalCard()

      expect(amountTexts(card, leftAttribute)).toEqual([money(76900 + 49200, 'MKD')])
      expect(within(card).getByText(translated('en', 'budget.card.includesConvertedEuro'))).toBeTruthy()
    })

    it('says "includes converted denar amounts" for a EUR total of a month in denars only', async () => {
      await renderBudget({ me: meWithTotalIn('EUR'), months: [mkdOnlyWithSharesConvertedToEur] })

      const card = await findTotalCard()

      expect(amountTexts(card, leftAttribute)).toEqual([money(2446 + 9350, 'EUR')])
      expect(within(card).getByText(translated('en', 'budget.card.includesConvertedDenars'))).toBeTruthy()
    })
  })

  describe('the rows and the tiles', () => {
    it('keep every amount in its own currency', async () => {
      await renderBudget({ me: meWithTotalIn('MKD'), months: [octoberWithGroupSharesConvertedToMkd] })
      await findTotalCard()

      const entryRows = within(await findEntryList()).getAllByRole('listitem')
      const shares = shareRows(await findGroupSharesBlock())

      expect(entryRows.map((row) => readableText(row))).toEqual([
        `CoffeeToday${money(12000, 'MKD')}`,
        `Food & drinksYesterday · Lunch in Ohrid${money(1250, 'EUR')}`,
      ])
      expect(shares.map((row) => shareRowText(row).amount)).toEqual([
        money(30000, 'MKD'),
        money(95000, 'MKD'),
        money(800, 'EUR'),
        money(480000, 'MKD'),
        money(12000, 'MKD'),
      ])
    })

    it('still show one grid of tiles per currency with its label', async () => {
      await renderBudget({ me: meWithTotalIn('EUR'), months: [octoberWithGroupSharesConvertedToEur] })
      await findTotalCard()

      const region = categoryTotalsRegion()

      expect(within(region).getByRole('group', { name: currencyLabel('MKD', 'en') })).toBeTruthy()
      expect(within(region).getByRole('group', { name: currencyLabel('EUR', 'en') })).toBeTruthy()
      expect(within(region).getAllByRole('listitem').map(readableText)).toContain(`Food & drinks${money(2050, 'EUR')}`)
    })
  })

  describe('when the converted total is not on', () => {
    it('keeps the card per currency when the setting is off and the answer has no converted totals', async () => {
      await renderBudget({ me: meWithTotalIn(null), months: [octoberWithGroupShares] })

      const card = await findTotalCard()

      expect(amountTexts(card, leftAttribute)).toEqual([money(629000, 'MKD'), money(2050, 'EUR')])
      expect(within(card).queryByText(translated('en', 'budget.card.inMkd'))).toBeNull()
      expect(within(card).queryByText(translated('en', 'budget.card.inEur'))).toBeNull()
      expect(within(card).queryByText(translated('en', 'budget.card.includesConvertedEuro'))).toBeNull()
    })

    it('keeps the card per currency when the setting is off but a saved month still has converted totals', async () => {
      await renderBudget({ me: meWithTotalIn(null), months: [octoberWithGroupSharesConvertedToMkd] })

      const card = await findTotalCard()

      expect(amountTexts(card, leftAttribute)).toEqual([money(629000, 'MKD'), money(2050, 'EUR')])
      expect(within(card).queryByText(translated('en', 'budget.card.inMkd'))).toBeNull()
    })

    it('keeps the card per currency when the setting is on but the answer has no converted totals yet', async () => {
      await renderBudget({ me: meWithTotalIn('MKD'), months: [octoberWithGroupShares] })

      const card = await findTotalCard()

      expect(amountTexts(card, leftAttribute)).toEqual([money(629000, 'MKD'), money(2050, 'EUR')])
      expect(within(card).queryByText(translated('en', 'budget.card.inMkd'))).toBeNull()
    })

    it('keeps the card per currency when the converted totals of a saved month are in the other currency than the setting', async () => {
      await renderBudget({ me: meWithTotalIn('EUR'), months: [octoberWithGroupSharesConvertedToMkd] })

      const card = await findTotalCard()

      expect(amountTexts(card, leftAttribute)).toEqual([money(629000, 'MKD'), money(2050, 'EUR')])
      expect(within(card).queryByText(translated('en', 'budget.card.inEur'))).toBeNull()
      expect(within(card).queryByText(translated('en', 'budget.card.inMkd'))).toBeNull()
    })

    it('keeps the card per currency when the converted totals of a saved month are in euro and the setting is MKD', async () => {
      await renderBudget({ me: meWithTotalIn('MKD'), months: [octoberWithGroupSharesConvertedToEur] })

      const card = await findTotalCard()

      expect(amountTexts(card, leftAttribute)).toEqual([money(629000, 'MKD'), money(2050, 'EUR')])
      expect(within(card).queryByText(translated('en', 'budget.card.inMkd'))).toBeNull()
      expect(within(card).queryByText(translated('en', 'budget.card.inEur'))).toBeNull()
    })

    it('keeps the Left card per currency when the setting is off', async () => {
      await renderBudget({ me: meWithTotalIn(null), months: [{ ...octoberWithSalaryAndGroupSharesConvertedToMkd, convertedTotals: null }] })

      const card = await findLeftCard()

      expect(amountLinesInOrder(card)).toEqual([
        `left:${money(6500000 - 629000, 'MKD')}`,
        `income:${incomeMoney(6500000, 'MKD')}`,
        `spent:${money(629000, 'MKD')}`,
        `spent:${money(2050, 'EUR')}`,
      ])
    })
  })
})
