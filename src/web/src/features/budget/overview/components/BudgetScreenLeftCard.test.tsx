import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import {
  budgetNow,
  categoryTotalsRegion,
  findEntryList,
  findTotalCard,
  monthButton,
  personalEntriesGet,
  renderBudget,
} from '@/test/budgetTestHelpers'
import {
  amountAt,
  amountElements,
  amountLinesInOrder,
  classesOf,
  findLeftCard,
  incomeAttribute,
  incomeMoney,
  leftAttribute,
  money,
  readableText,
  spentAttribute,
} from '@/test/budgetLeftCardTestHelpers'
import {
  octoberEuroIncomeAndDenarSpending,
  octoberIncomeAndTwoSpendings,
  octoberIncomeInBothCurrenciesSpendingInBoth,
  octoberOverspent,
  octoberWithLargestSafeIncome,
  octoberWithLargestSafeIncomeAndSpending,
  largestSafeMinor,
  septemberWithIncome,
} from '@/test/budgetLeftTestData'
import { isBefore } from '@/test/expenseTestHelpers'
import {
  emptyMonth,
  euroIncomeOnlyOctober,
  incomeOnlyOctober,
  octoberMonth,
  octoberWithSalary,
  octoberWithTwoIncomes,
  septemberWithArchivedCategory,
} from '@/test/personalEntryTestData'
import { findRepeatingRow } from '@/test/recurringIncomeTestHelpers'
import { salaryRecurringIncome } from '@/test/recurringIncomeTestData'
import { neverAnswer } from '@/test/requestTestHelpers'
import { loadingLook } from '@/test/skeletonTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const bigLook = 'text-[2.25rem]'
const smallLook = 'text-lg'

describe('BudgetScreen, the "Left this month" card', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(budgetNow)
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.clearAllMocks()
    vi.restoreAllMocks()
  })

  describe('a month with no income', () => {
    it.each(languages)('looks as before: "Spent this month", the spending lines, no Left, no Income and no Spent columns (%s)', async (language) => {
      await renderBudget({ language, months: [octoberMonth] })

      const card = await findTotalCard(language)

      expect(within(card).getByText(translated(language, 'budget.screen.spentThisMonth'))).toBeTruthy()
      expect(amountElements(card, leftAttribute)).toHaveLength(2)
      expect(amountElements(card, incomeAttribute)).toHaveLength(0)
      expect(amountElements(card, spentAttribute)).toHaveLength(0)
      expect(within(card).queryByText(translated(language, 'budget.screen.income'))).toBeNull()
      expect(within(card).queryByText(translated(language, 'budget.screen.spent'))).toBeNull()
      expect(screen.queryByRole('region', { name: translated(language, 'budget.screen.leftThisMonth') })).toBeNull()
    })
  })

  describe('a month with income and spending in one currency', () => {
    it.each(languages)('shows Left as the big amount, then Income with a plus and Spent, in order, with the EUR spending as a plain Spent line (%s)', async (language) => {
      await renderBudget({ language, months: [octoberIncomeAndTwoSpendings] })

      const card = await findLeftCard(language)

      expect(amountLinesInOrder(card)).toEqual([
        `left:${money(3260000, 'MKD', language)}`,
        `income:${incomeMoney(4500000, 'MKD', language)}`,
        `spent:${money(1240000, 'MKD', language)}`,
        `spent:${money(5000, 'EUR', language)}`,
      ])
    })

    it('does not show the "Spent this month" card when it shows the "Left this month" card', async () => {
      await renderBudget({ months: [octoberIncomeAndTwoSpendings] })
      await findLeftCard()

      expect(screen.queryByRole('region', { name: translated('en', 'budget.screen.spentThisMonth') })).toBeNull()
    })

    it('computes Left in integer minor units: income minus spending in the same currency only', async () => {
      await renderBudget({ months: [octoberIncomeAndTwoSpendings] })

      const card = await findLeftCard()

      expect(amountElements(card, leftAttribute).map(readableText)).toEqual([money(4500000 - 1240000, 'MKD')])
    })

    it('writes Left with no plus sign and in the normal text colour when it is positive', async () => {
      await renderBudget({ months: [octoberIncomeAndTwoSpendings] })

      const card = await findLeftCard()
      const left = amountElements(card, leftAttribute)[0]

      expect(readableText(left).startsWith('+')).toBe(false)
      expect(classesOf(left)).not.toContain('text-negative')
      expect(classesOf(left)).not.toContain('text-positive')
    })

    it('writes the Income amount in the positive look with a plus, and the Spent amount in the normal look', async () => {
      await renderBudget({ months: [octoberIncomeAndTwoSpendings] })

      const card = await findLeftCard()
      const income = amountElements(card, incomeAttribute)[0]
      const spent = amountElements(card, spentAttribute)[0]

      expect(classesOf(income)).toContain('text-positive')
      expect(readableText(income).startsWith('+')).toBe(true)
      expect(classesOf(spent)).not.toContain('text-positive')
      expect(classesOf(spent)).not.toContain('text-negative')
      expect(readableText(spent).startsWith('+')).toBe(false)
    })

    it('labels the columns with the words Income and Spent, the label before its amount', async () => {
      await renderBudget({ months: [octoberIncomeAndTwoSpendings] })

      const card = await findLeftCard()
      const text = readableText(card)
      const incomeLabelAt = text.indexOf('Income')
      const incomeAt = text.indexOf(incomeMoney(4500000, 'MKD'))
      const spentLabelAt = text.indexOf('Spent')
      const spentAt = text.indexOf(money(1240000, 'MKD'), spentLabelAt)

      expect(text.indexOf('Left this month')).toBe(0)
      expect(text.indexOf(money(3260000, 'MKD'))).toBeGreaterThan(0)
      expect(incomeLabelAt).toBeGreaterThan(text.indexOf(money(3260000, 'MKD')))
      expect(incomeAt).toBeGreaterThan(incomeLabelAt)
      expect(spentLabelAt).toBeGreaterThan(incomeAt)
      expect(spentAt).toBeGreaterThan(spentLabelAt)
    })

    it.each(languages)('has the labels Income and Spent as texts of their own, one Income and two Spent (MKD block and EUR line) (%s)', async (language) => {
      await renderBudget({ language, months: [octoberIncomeAndTwoSpendings] })

      const card = await findLeftCard(language)

      expect(within(card).getAllByText(translated(language, 'budget.screen.income'))).toHaveLength(1)
      expect(within(card).getAllByText(translated(language, 'budget.screen.spent'))).toHaveLength(2)
    })

    it('marks the Left amount as the main one, in the big look', async () => {
      await renderBudget({ months: [octoberIncomeAndTwoSpendings] })

      const card = await findLeftCard()
      const left = amountAt(card, leftAttribute, 0)

      expect(left.getAttribute(leftAttribute)).toBe('main')
      expect(classesOf(left)).toContain(bigLook)
    })
  })

  describe('a currency with spending and no income', () => {
    it('shows only a plain Spent line in the smaller look, with no Left and no Income, below the blocks', async () => {
      await renderBudget({ months: [octoberIncomeAndTwoSpendings] })

      const card = await findLeftCard()
      const euroLine = amountElements(card, spentAttribute)[1]

      expect(readableText(euroLine)).toBe(money(5000, 'EUR'))
      expect(classesOf(euroLine)).toContain(smallLook)
      expect(classesOf(euroLine)).not.toContain(bigLook)
      expect(amountElements(card, leftAttribute)).toHaveLength(1)
      expect(amountElements(card, incomeAttribute)).toHaveLength(1)
    })

    it('puts the Spent line after the last block, and labels it with the word Spent', async () => {
      await renderBudget({ months: [octoberIncomeAndTwoSpendings] })

      const card = await findLeftCard()
      const euroLine = amountAt(card, spentAttribute, 1)
      const lastBlockAmount = amountAt(card, spentAttribute, 0)

      expect(isBefore(lastBlockAmount, euroLine)).toBe(true)
      const text = readableText(card)
      expect(text.lastIndexOf('Spent')).toBeLessThan(text.indexOf(money(5000, 'EUR')))
    })

    it('shows the EUR block as the big one and the denar spending as a plain Spent line when only EUR has income', async () => {
      await renderBudget({ months: [octoberEuroIncomeAndDenarSpending] })

      const card = await findLeftCard()

      expect(amountLinesInOrder(card)).toEqual([
        `left:${money(20000, 'EUR')}`,
        `income:${incomeMoney(20000, 'EUR')}`,
        `spent:${money(0, 'EUR')}`,
        `spent:${money(12000, 'MKD')}`,
      ])
      expect(classesOf(amountAt(card, leftAttribute, 0))).toContain(bigLook)
      expect(classesOf(amountAt(card, spentAttribute, 1))).toContain(smallLook)
    })
  })

  describe('income in two currencies', () => {
    it('shows one block per currency, MKD then EUR, each with its own Income and Spent columns', async () => {
      await renderBudget({ months: [octoberIncomeInBothCurrenciesSpendingInBoth] })

      const card = await findLeftCard()

      expect(amountLinesInOrder(card)).toEqual([
        `left:${money(6488000, 'MKD')}`,
        `income:${incomeMoney(6500000, 'MKD')}`,
        `spent:${money(12000, 'MKD')}`,
        `left:${money(15000, 'EUR')}`,
        `income:${incomeMoney(20000, 'EUR')}`,
        `spent:${money(5000, 'EUR')}`,
      ])
      expect(within(card).getAllByText(translated('en', 'budget.screen.income'))).toHaveLength(2)
      expect(within(card).getAllByText(translated('en', 'budget.screen.spent'))).toHaveLength(2)
    })

    it('marks the first Left as main in the big look and the later one as second in the smaller look', async () => {
      await renderBudget({ months: [octoberIncomeInBothCurrenciesSpendingInBoth] })

      const card = await findLeftCard()
      const lefts = amountElements(card, leftAttribute)

      expect(lefts.map((left) => left.getAttribute(leftAttribute))).toEqual(['main', 'second'])
      expect(classesOf(lefts[0])).toContain(bigLook)
      expect(classesOf(lefts[1])).toContain(smallLook)
      expect(classesOf(lefts[1])).not.toContain(bigLook)
    })

    it('shows spending 0 in the currency that has income and no spending, never adding the other currency', async () => {
      await renderBudget({ months: [octoberWithTwoIncomes] })

      const card = await findLeftCard()

      expect(amountLinesInOrder(card)).toEqual([
        `left:${money(6488000, 'MKD')}`,
        `income:${incomeMoney(6500000, 'MKD')}`,
        `spent:${money(12000, 'MKD')}`,
        `left:${money(20000, 'EUR')}`,
        `income:${incomeMoney(20000, 'EUR')}`,
        `spent:${money(0, 'EUR')}`,
      ])
    })

    it('shows the denar income of a month with EUR spending as MKD Left plus a EUR Spent line', async () => {
      await renderBudget({ months: [octoberWithSalary] })

      const card = await findLeftCard()

      expect(amountLinesInOrder(card)).toEqual([
        `left:${money(6488000, 'MKD')}`,
        `income:${incomeMoney(6500000, 'MKD')}`,
        `spent:${money(12000, 'MKD')}`,
        `spent:${money(1250, 'EUR')}`,
      ])
    })
  })

  describe('spending more than income', () => {
    it.each(languages)('shows a negative Left with the minus sign in the negative look (%s)', async (language) => {
      await renderBudget({ language, months: [octoberOverspent] })

      const card = await findLeftCard(language)
      const left = amountAt(card, leftAttribute, 0)

      expect(readableText(left)).toBe(money(-240000, 'MKD', language))
      expect(readableText(left).startsWith('-')).toBe(true)
      expect(classesOf(left)).toContain('text-negative')
    })

    it('keeps Income in the positive look and Spent in the normal look next to a negative Left', async () => {
      await renderBudget({ months: [octoberOverspent] })

      const card = await findLeftCard()

      expect(amountLinesInOrder(card)).toEqual([
        `left:${money(-240000, 'MKD')}`,
        `income:${incomeMoney(1000000, 'MKD')}`,
        `spent:${money(1240000, 'MKD')}`,
      ])
      expect(classesOf(amountAt(card, incomeAttribute, 0))).toContain('text-positive')
      expect(classesOf(amountAt(card, spentAttribute, 0))).not.toContain('text-negative')
    })
  })

  describe('a month with income and no spending', () => {
    it.each(languages)('shows the card with Left equal to the income and Spent 0, not the calm "Nothing spent" text (%s)', async (language) => {
      await renderBudget({ language, months: [incomeOnlyOctober] })

      const card = await findLeftCard(language)

      expect(amountLinesInOrder(card)).toEqual([
        `left:${money(6500000, 'MKD', language)}`,
        `income:${incomeMoney(6500000, 'MKD', language)}`,
        `spent:${money(0, 'MKD', language)}`,
      ])
      expect(screen.queryByText(translated(language, 'budget.screen.emptyTitle'))).toBeNull()
      expect(within(await findEntryList(language)).getAllByRole('listitem')).toHaveLength(1)
    })

    it('shows the same for an income in EUR only', async () => {
      await renderBudget({ months: [euroIncomeOnlyOctober] })

      const card = await findLeftCard()

      expect(amountLinesInOrder(card)).toEqual([
        `left:${money(20000, 'EUR')}`,
        `income:${incomeMoney(20000, 'EUR')}`,
        `spent:${money(0, 'EUR')}`,
      ])
      expect(screen.queryByText(translated('en', 'budget.screen.emptyTitle'))).toBeNull()
    })

    it('shows no category tiles, since nothing was spent', async () => {
      await renderBudget({ months: [incomeOnlyOctober] })
      await findLeftCard()

      expect(screen.queryByRole('region', { name: translated('en', 'budget.screen.categoryTotals') })).toBeNull()
    })
  })

  describe('a month with no entries', () => {
    it('still shows the empty state and no card', async () => {
      await renderBudget({ months: [emptyMonth('2026-10')] })

      expect(await screen.findByText(translated('en', 'budget.screen.emptyTitle'))).toBeTruthy()
      expect(screen.queryByRole('region', { name: translated('en', 'budget.screen.leftThisMonth') })).toBeNull()
      expect(screen.queryByRole('region', { name: translated('en', 'budget.screen.spentThisMonth') })).toBeNull()
    })
  })

  describe('big numbers', () => {
    it('does not crash and computes Left with integers when the income is the largest safe number of minor units', async () => {
      await renderBudget({ months: [octoberWithLargestSafeIncome] })

      const card = await findLeftCard()

      expect(amountLinesInOrder(card)).toEqual([
        `left:${money(largestSafeMinor - 1, 'EUR')}`,
        `income:${incomeMoney(largestSafeMinor, 'EUR')}`,
        `spent:${money(1, 'EUR')}`,
      ])
    })

    it('shows Left 0 with no sign when the income and the spending are both the largest safe number', async () => {
      await renderBudget({ months: [octoberWithLargestSafeIncomeAndSpending] })

      const card = await findLeftCard()
      const left = amountAt(card, leftAttribute, 0)

      expect(readableText(left)).toBe(money(0, 'EUR'))
      expect(classesOf(left)).not.toContain('text-negative')
    })
  })

  describe('accessibility', () => {
    it.each(languages)('is a section named by its label text, "Left this month" (%s)', async (language) => {
      await renderBudget({ language, months: [octoberIncomeAndTwoSpendings] })

      const card = await findLeftCard(language)
      const labelId = card.getAttribute('aria-labelledby')

      expect(card.tagName).toBe('SECTION')
      expect(labelId).not.toBeNull()
      expect(document.getElementById(labelId ?? '')?.textContent).toBe(translated(language, 'budget.screen.leftThisMonth'))
    })

    it('says Income and Spent in words, not only in colour, so the amounts can be told apart without seeing them', async () => {
      await renderBudget({ months: [octoberIncomeAndTwoSpendings] })

      const card = await findLeftCard()

      expect(within(card).getByText('Income')).toBeTruthy()
      expect(within(card).getAllByText('Spent').length).toBeGreaterThan(0)
      expect(readableText(amountAt(card, incomeAttribute, 0))).toContain('+')
    })
  })

  describe('following the month', () => {
    it('replaces the numbers when another month with income is picked', async () => {
      await renderBudget({ months: [octoberWithSalary, septemberWithIncome] })
      await findLeftCard()

      fireEvent.click(monthButton('Sep'))

      await waitFor(() => {
        expect(amountLinesInOrder(screen.getByRole('region', { name: translated('en', 'budget.screen.leftThisMonth') }))).toEqual([
          `left:${money(2850000, 'MKD')}`,
          `income:${incomeMoney(3000000, 'MKD')}`,
          `spent:${money(150000, 'MKD')}`,
        ])
      })
    })

    it('changes the label back to "Spent this month" when a month with no income is picked', async () => {
      await renderBudget({ months: [octoberWithSalary, septemberWithArchivedCategory] })
      await findLeftCard()

      fireEvent.click(monthButton('Sep'))

      expect(await findTotalCard()).toBeTruthy()
      expect(screen.queryByRole('region', { name: translated('en', 'budget.screen.leftThisMonth') })).toBeNull()
    })

    it('changes the label to "Left this month" when a month with income is picked after one with none', async () => {
      await renderBudget({ months: [octoberMonth, septemberWithIncome] })
      await findTotalCard()

      fireEvent.click(monthButton('Sep'))

      expect(await findLeftCard()).toBeTruthy()
      expect(screen.queryByRole('region', { name: translated('en', 'budget.screen.spentThisMonth') })).toBeNull()
    })

    it('shows skeletons and no card for a month that has not loaded yet, not the month that was on screen', async () => {
      await renderBudget({
        months: [octoberWithSalary],
        answers: { [personalEntriesGet('2026-09')]: neverAnswer() },
      })
      await findLeftCard()

      fireEvent.click(monthButton('Sep'))

      await waitFor(() => {
        expect(loadingLook().skeletonCount).toBeGreaterThan(0)
      })
      expect(screen.queryByRole('region', { name: translated('en', 'budget.screen.leftThisMonth') })).toBeNull()
    })
  })

  describe('what sits around the card', () => {
    it('keeps the Repeating income row right under the card, before the category tiles', async () => {
      await renderBudget({ months: [octoberIncomeAndTwoSpendings], recurringIncomes: [salaryRecurringIncome] })

      const card = await findLeftCard()
      const row = await findRepeatingRow()

      expect(card.nextElementSibling).toBe(row)
      expect(isBefore(row, categoryTotalsRegion())).toBe(true)
    })

    it('keeps the Repeating income row right under the card in a month with income and no spending', async () => {
      await renderBudget({ months: [incomeOnlyOctober], recurringIncomes: [salaryRecurringIncome] })

      const card = await findLeftCard()

      expect(card.nextElementSibling).toBe(await findRepeatingRow())
    })

    it('lists only spending in the category tiles, never the income', async () => {
      await renderBudget({ months: [octoberIncomeAndTwoSpendings] })
      await findLeftCard()

      const tiles = within(categoryTotalsRegion()).getAllByRole('listitem')

      expect(tiles.map(readableText)).toEqual([`Coffee${money(1240000, 'MKD')}`, `Food & drinks${money(5000, 'EUR')}`])
      expect(within(categoryTotalsRegion()).queryByText(translated('en', 'budget.income.listTitle'))).toBeNull()
    })

    it('keeps the entries list after the card, with the income rows in it', async () => {
      await renderBudget({ months: [octoberIncomeAndTwoSpendings] })

      const card = await findLeftCard()
      const list = await findEntryList()

      expect(isBefore(card, list)).toBe(true)
      expect(within(list).getAllByRole('listitem')).toHaveLength(3)
    })
  })
})
