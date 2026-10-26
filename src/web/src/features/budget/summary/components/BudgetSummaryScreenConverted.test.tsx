import { screen, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { money, readableText } from '@/test/budgetLeftCardTestHelpers'
import { budgetNow } from '@/test/budgetTestHelpers'
import { isBefore } from '@/test/expenseTestHelpers'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import {
  convertedToEurSummary,
  convertedToMkdSummary,
  eurSummary,
  mkdSummary,
  octoberSummary,
  octoberSummaryConvertedToEur,
  octoberSummaryConvertedToMkd,
  summaryOf,
} from '@/test/personalSummaryTestData'
import {
  amountReadout,
  categoryRows,
  changeOf,
  convertedCardName,
  differenceOf,
  directionOf,
  downText,
  findConvertedCard,
  findCurrencyCard,
  nameOf,
  previousAmountOf,
  queryCategoryList,
  queryConvertedCard,
  queryTotalRow,
  renderSummaryScreen,
  signedMoney,
  thisAmountOf,
  totalRow,
  upText,
} from '@/test/personalSummaryTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

describe('the monthly summary screen, the converted total', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.clearAllMocks()
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(budgetNow)
    stubDeviceColorScheme('light')
    stubGoogleSignIn()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('with the setting on in MKD', () => {
    it.each(languages)('shows one converted card named "in MKD" before the cards of the two currencies (%s)', async (language) => {
      await renderSummaryScreen({
        language,
        budgetTotalCurrency: 'MKD',
        summaries: [octoberSummaryConvertedToMkd],
      })

      const converted = await findConvertedCard('MKD', language)
      const mkdCard = await findCurrencyCard('MKD', language)
      const eurCard = await findCurrencyCard('EUR', language)

      expect(within(converted).getByRole('heading', { level: 2, name: convertedCardName('MKD', language) })).toBeTruthy()
      expect(isBefore(converted, mkdCard)).toBe(true)
      expect(isBefore(mkdCard, eurCard)).toBe(true)
      expect(screen.getAllByRole('region')).toHaveLength(3)
    })

    it('shows the converted Spent and Income, this month and the previous month, all in MKD', async () => {
      await renderSummaryScreen({ budgetTotalCurrency: 'MKD', summaries: [octoberSummaryConvertedToMkd] })
      const converted = await findConvertedCard('MKD')

      const spending = totalRow(converted, 'spending')
      const income = totalRow(converted, 'income')

      expect(within(spending).getByText(translated('en', 'budget.screen.spent'))).toBeTruthy()
      expect(amountReadout(thisAmountOf(spending))).toBe(money(convertedToMkdSummary.spendingThisMinor, 'MKD'))
      expect(amountReadout(previousAmountOf(spending))).toBe(money(convertedToMkdSummary.spendingPreviousMinor, 'MKD'))
      expect(within(income).getByText(translated('en', 'budget.screen.income'))).toBeTruthy()
      expect(amountReadout(thisAmountOf(income))).toBe(money(convertedToMkdSummary.incomeThisMinor, 'MKD'))
      expect(amountReadout(previousAmountOf(income))).toBe(money(convertedToMkdSummary.incomePreviousMinor, 'MKD'))
      expect(readableText(converted)).not.toContain('€')
    })

    it('shows the change of the converted Spent as up 56 percent in red, with the difference', async () => {
      await renderSummaryScreen({ budgetTotalCurrency: 'MKD', summaries: [octoberSummaryConvertedToMkd] })
      const converted = await findConvertedCard('MKD')
      const spending = totalRow(converted, 'spending')

      expect(directionOf(changeOf(spending))).toBe('up')
      expect(readableText(changeOf(spending))).toBe(upText(56))
      expect(Array.from(changeOf(spending).classList)).toContain('text-negative')
      expect(amountReadout(differenceOf(spending) as HTMLElement)).toBe(signedMoney(1326800, 'MKD'))
    })

    it('shows the change of the converted Income as down 20 percent in a neutral colour', async () => {
      await renderSummaryScreen({ budgetTotalCurrency: 'MKD', summaries: [octoberSummaryConvertedToMkd] })
      const converted = await findConvertedCard('MKD')
      const income = totalRow(converted, 'income')

      expect(directionOf(changeOf(income))).toBe('down')
      expect(readableText(changeOf(income))).toBe(downText(20))
      expect(Array.from(changeOf(income).classList)).not.toContain('text-negative')
      expect(Array.from(changeOf(income).classList)).not.toContain('text-positive')
      expect(amountReadout(differenceOf(income) as HTMLElement)).toBe(signedMoney(-1030000, 'MKD'))
    })

    it('keeps the category details in the cards of the currencies and none in the converted card', async () => {
      await renderSummaryScreen({ budgetTotalCurrency: 'MKD', summaries: [octoberSummaryConvertedToMkd] })
      const converted = await findConvertedCard('MKD')
      const mkdCard = await findCurrencyCard('MKD')
      const eurCard = await findCurrencyCard('EUR')

      expect(queryCategoryList(converted)).toBeNull()
      expect(categoryRows(mkdCard)).toHaveLength(5)
      expect(categoryRows(eurCard)).toHaveLength(1)
      expect(nameOf(categoryRows(mkdCard)[0]).textContent).toBe('Coffee')
    })

    it('keeps the totals of the cards of the currencies in their own currency, unconverted', async () => {
      await renderSummaryScreen({ budgetTotalCurrency: 'MKD', summaries: [octoberSummaryConvertedToMkd] })
      const mkdCard = await findCurrencyCard('MKD')
      const eurCard = await findCurrencyCard('EUR')

      expect(amountReadout(thisAmountOf(totalRow(mkdCard, 'spending')))).toBe(money(mkdSummary.spendingThisMinor, 'MKD'))
      expect(amountReadout(thisAmountOf(totalRow(eurCard, 'spending')))).toBe(money(eurSummary.spendingThisMinor, 'EUR'))
    })

    it('leaves out the Income row of the converted card when income is 0 in both months', async () => {
      const converted = { ...convertedToMkdSummary, incomeThisMinor: 0, incomePreviousMinor: 0 }
      await renderSummaryScreen({
        budgetTotalCurrency: 'MKD',
        summaries: [summaryOf('2026-10', '2026-09', [mkdSummary, eurSummary], converted)],
      })
      const card = await findConvertedCard('MKD')

      expect(totalRow(card, 'spending')).toBeTruthy()
      expect(queryTotalRow(card, 'income')).toBeNull()
    })

    it('shows the converted card with the cards of the currencies even when one currency has only denars', async () => {
      await renderSummaryScreen({
        budgetTotalCurrency: 'MKD',
        summaries: [summaryOf('2026-10', '2026-09', [mkdSummary], convertedToMkdSummary)],
      })

      expect(await findConvertedCard('MKD')).toBeTruthy()
      expect(await findCurrencyCard('MKD')).toBeTruthy()
      expect(screen.queryByRole('region', { name: 'EUR' })).toBeNull()
    })
  })

  describe('with the setting on in EUR', () => {
    it.each(languages)('shows one converted card named "in EUR" with every amount in euros (%s)', async (language) => {
      await renderSummaryScreen({
        language,
        budgetTotalCurrency: 'EUR',
        summaries: [octoberSummaryConvertedToEur],
      })

      const converted = await findConvertedCard('EUR', language)
      const spending = totalRow(converted, 'spending')
      const income = totalRow(converted, 'income')

      expect(amountReadout(thisAmountOf(spending))).toBe(money(convertedToEurSummary.spendingThisMinor, 'EUR', language))
      expect(amountReadout(previousAmountOf(spending))).toBe(money(convertedToEurSummary.spendingPreviousMinor, 'EUR', language))
      expect(amountReadout(thisAmountOf(income))).toBe(money(convertedToEurSummary.incomeThisMinor, 'EUR', language))
      expect(amountReadout(previousAmountOf(income))).toBe(money(convertedToEurSummary.incomePreviousMinor, 'EUR', language))
      expect(readableText(converted)).not.toContain('MKD')
      expect(queryConvertedCard('MKD', language)).toBeNull()
    })

    it('shows the changes of the euro card as up 56 percent and down 19 percent', async () => {
      await renderSummaryScreen({ budgetTotalCurrency: 'EUR', summaries: [octoberSummaryConvertedToEur] })
      const converted = await findConvertedCard('EUR')

      expect(readableText(changeOf(totalRow(converted, 'spending')))).toBe(upText(56))
      expect(readableText(changeOf(totalRow(converted, 'income')))).toBe(downText(19))
    })
  })

  describe('when the saved setting and the answer do not fit', () => {
    it('shows no converted card when the setting is off, even if the answer has converted totals', async () => {
      await renderSummaryScreen({ budgetTotalCurrency: null, summaries: [octoberSummaryConvertedToMkd] })
      await findCurrencyCard('MKD')

      expect(queryConvertedCard('MKD')).toBeNull()
      expect(queryConvertedCard('EUR')).toBeNull()
      expect(screen.getAllByRole('region')).toHaveLength(2)
    })

    it('shows no converted card when the setting is EUR and the answer is converted to MKD', async () => {
      await renderSummaryScreen({ budgetTotalCurrency: 'EUR', summaries: [octoberSummaryConvertedToMkd] })
      await findCurrencyCard('MKD')

      expect(queryConvertedCard('MKD')).toBeNull()
      expect(queryConvertedCard('EUR')).toBeNull()
    })

    it('shows no converted card when the setting is MKD and the answer is converted to EUR', async () => {
      await renderSummaryScreen({ budgetTotalCurrency: 'MKD', summaries: [octoberSummaryConvertedToEur] })
      await findCurrencyCard('MKD')

      expect(queryConvertedCard('MKD')).toBeNull()
      expect(queryConvertedCard('EUR')).toBeNull()
    })

    it('shows no converted card when the setting is on but the answer has null converted totals', async () => {
      await renderSummaryScreen({ budgetTotalCurrency: 'MKD', summaries: [octoberSummary] })
      await findCurrencyCard('MKD')

      expect(queryConvertedCard('MKD')).toBeNull()
      expect(screen.getAllByRole('region')).toHaveLength(2)
    })
  })
})
