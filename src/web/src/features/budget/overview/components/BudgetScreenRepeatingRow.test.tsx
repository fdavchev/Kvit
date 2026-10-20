import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { RecurringIncome } from '@/core/services/recurringIncomes/recurringIncomesService'
import {
  budgetNow,
  findEntryList,
  findTotalCard,
  monthButton,
  personalEntriesGet,
  renderBudget,
} from '@/test/budgetTestHelpers'
import { isBefore } from '@/test/expenseTestHelpers'
import { emptyMonth, incomeOnlyOctober, octoberMonth } from '@/test/personalEntryTestData'
import {
  findRepeatingRow,
  findRepeatingSheet,
  repeatingRowName,
} from '@/test/recurringIncomeTestHelpers'
import {
  flatRentRecurringIncome,
  recurringIncomesGet,
  recurringListAnswer,
  salaryRecurringIncome,
  tutoringRecurringIncome,
  untitledRecurringIncome,
} from '@/test/recurringIncomeTestData'
import { networkFailureAnswer, neverAnswer, requestCount, type AnswerFactory } from '@/test/requestTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const listUrl = '/api/personal/recurring-incomes'
const four: RecurringIncome[] = [salaryRecurringIncome, flatRentRecurringIncome, tutoringRecurringIncome, untitledRecurringIncome]

function failingThenAnswering(failure: AnswerFactory, success: AnswerFactory): AnswerFactory {
  let calls = 0
  return () => {
    calls += 1
    return calls === 1 ? failure() : success()
  }
}

describe('BudgetScreen, the "Repeating income" row', () => {
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

  describe('what it says', () => {
    it.each(languages)('says "none yet" when there is no repeating income (%s)', async (language) => {
      await renderBudget({ language, recurringIncomes: [] })

      const row = await findRepeatingRow(language)

      expect(within(row).getByText(translated(language, 'budget.recurring.rowNone'))).toBeTruthy()
      expect(within(row).queryByText(/·\s*\d/)).toBeNull()
    })

    it.each(languages)('says the count and the names of one repeating income (%s)', async (language) => {
      await renderBudget({ language, recurringIncomes: [salaryRecurringIncome] })

      const row = await findRepeatingRow(language)

      expect(within(row).getByText(translated(language, 'budget.recurring.rowCount', { count: '1' }))).toBeTruthy()
      expect(within(row).getByText('Salary')).toBeTruthy()
    })

    it.each(languages)('says the count and both names of two repeating incomes (%s)', async (language) => {
      await renderBudget({ language, recurringIncomes: [salaryRecurringIncome, flatRentRecurringIncome] })

      const row = await findRepeatingRow(language)

      expect(within(row).getByText(translated(language, 'budget.recurring.rowCount', { count: '2' }))).toBeTruthy()
      expect(within(row).getByText('Salary, Flat rent')).toBeTruthy()
    })

    it('says "Salary, Flat rent +1 more" for three: the first two as the server sent them', async () => {
      await renderBudget({ recurringIncomes: [salaryRecurringIncome, flatRentRecurringIncome, tutoringRecurringIncome] })

      const row = await findRepeatingRow()

      expect(within(row).getByText('Repeating income · 3')).toBeTruthy()
      expect(within(row).getByText('Salary, Flat rent +1 more')).toBeTruthy()
    })

    it('says "+2 more" for four, and names the first two only', async () => {
      await renderBudget({ recurringIncomes: four })

      const row = await findRepeatingRow()

      expect(within(row).getByText('Repeating income · 4')).toBeTruthy()
      expect(within(row).getByText('Salary, Flat rent +2 more')).toBeTruthy()
    })

    it.each(languages)('writes the "more" part of the hint in %s with the plural form of the count', async (language) => {
      await renderBudget({ language, recurringIncomes: [salaryRecurringIncome, flatRentRecurringIncome, tutoringRecurringIncome] })

      const row = await findRepeatingRow(language)

      expect(
        within(row).getByText(`Salary, Flat rent ${translated(language, 'budget.recurring.more_one', { count: '1' })}`),
      ).toBeTruthy()
    })

    it('uses the text for an income with no name, "Income", in the hint', async () => {
      await renderBudget({ recurringIncomes: [untitledRecurringIncome, salaryRecurringIncome] })

      const row = await findRepeatingRow()

      expect(within(row).getByText(`${translated('en', 'budget.income.listTitle')}, Salary`)).toBeTruthy()
    })

    it('is one button, at least 44 px high', async () => {
      await renderBudget({ recurringIncomes: [salaryRecurringIncome] })

      const row = await findRepeatingRow()

      expect(row.getAttribute('type')).toBe('button')
      expect(row.className.split(/\s+/)).toEqual(expect.arrayContaining([expect.stringMatching(/^min-h-(1[1-9]|20)$/)]))
    })
  })

  describe('where it is', () => {
    it('sits under the month strip and above the entries', async () => {
      await renderBudget({ recurringIncomes: [salaryRecurringIncome] })

      const row = await findRepeatingRow()
      const list = await findEntryList()

      expect(isBefore(screen.getByRole('group', { name: translated('en', 'budget.screen.months') }), row)).toBe(true)
      expect(isBefore(row, list)).toBe(true)
    })

    it('shows in a month with nothing in it', async () => {
      await renderBudget({ months: [emptyMonth('2026-10')], recurringIncomes: [salaryRecurringIncome] })

      expect(await findRepeatingRow()).toBeTruthy()
      expect(screen.getByText(translated('en', 'budget.screen.emptyTitle'))).toBeTruthy()
    })

    it('shows "none yet" in a month with nothing in it', async () => {
      await renderBudget({ months: [emptyMonth('2026-10')] })

      expect(within(await findRepeatingRow()).getByText(translated('en', 'budget.recurring.rowNone'))).toBeTruthy()
    })

    it('shows in a month with income and no spending', async () => {
      await renderBudget({ months: [incomeOnlyOctober], recurringIncomes: [salaryRecurringIncome] })

      expect(await findRepeatingRow()).toBeTruthy()
      expect(await findEntryList()).toBeTruthy()
    })

    it('does not depend on the selected month: the same row and no new request after another month is picked', async () => {
      const { fetchMock } = await renderBudget({
        months: [octoberMonth, { ...octoberMonth, month: '2026-09' }],
        recurringIncomes: [salaryRecurringIncome, flatRentRecurringIncome],
      })
      await findTotalCard()
      await findRepeatingRow()

      fireEvent.click(monthButton('Sep'))
      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', '/api/personal/entries?month=2026-09')).toBe(1)
      })

      expect(within(await findRepeatingRow()).getByText('Repeating income · 2')).toBeTruthy()
      expect(requestCount(fetchMock, 'GET', listUrl)).toBe(1)
    })

    it('shows in a month that failed to load', async () => {
      await renderBudget({
        recurringIncomes: [salaryRecurringIncome],
        answers: { [personalEntriesGet('2026-10')]: networkFailureAnswer() },
      })

      expect(await findRepeatingRow()).toBeTruthy()
    })
  })

  describe('loading the list', () => {
    it('asks for the list once when the Budget tab opens', async () => {
      const { fetchMock } = await renderBudget({ recurringIncomes: [salaryRecurringIncome] })
      await findRepeatingRow()

      expect(requestCount(fetchMock, 'GET', listUrl)).toBe(1)
    })

    it('shows the month at once while the list is still on its way, with no loading or error notice of its own and no "none yet"', async () => {
      await renderBudget({ answers: { [recurringIncomesGet]: neverAnswer() } })

      expect(await findTotalCard()).toBeTruthy()
      expect(await findEntryList()).toBeTruthy()
      expect(screen.queryByRole('status')).toBeNull()
      expect(screen.queryByRole('alert')).toBeNull()
      expect(screen.queryByText(translated('en', 'budget.recurring.rowNone'))).toBeNull()
    })

    it.each(languages)('keeps the month and shows a quiet "Try again", not an alert, when the list cannot be loaded (%s)', async (language) => {
      await renderBudget({ language, answers: { [recurringIncomesGet]: networkFailureAnswer() } })

      expect(await findTotalCard(language)).toBeTruthy()
      expect(await screen.findByRole('button', { name: translated(language, 'common.retry') })).toBeTruthy()
      expect(screen.queryByRole('alert')).toBeNull()
    })

    it('asks again and shows the row when the quiet "Try again" is pressed', async () => {
      const { fetchMock } = await renderBudget({
        answers: {
          [recurringIncomesGet]: failingThenAnswering(networkFailureAnswer(), recurringListAnswer([salaryRecurringIncome])),
        },
      })

      fireEvent.click(await screen.findByRole('button', { name: translated('en', 'common.retry') }))

      expect(within(await findRepeatingRow()).getByText('Repeating income · 1')).toBeTruthy()
      expect(requestCount(fetchMock, 'GET', listUrl)).toBe(2)
      expect(screen.queryByRole('button', { name: translated('en', 'common.retry') })).toBeNull()
    })
  })

  describe('tapping it', () => {
    it.each([
      ['one', [salaryRecurringIncome]],
      ['two', [salaryRecurringIncome, flatRentRecurringIncome]],
    ])('opens the list sheet when there are %s', async (_name, incomes) => {
      const { router } = await renderBudget({ recurringIncomes: incomes })

      fireEvent.click(await findRepeatingRow())

      expect(await findRepeatingSheet()).toBeTruthy()
      expect(router.state.location.pathname).toBe(routes.budget)
    })

    it.each([
      ['three', [salaryRecurringIncome, flatRentRecurringIncome, tutoringRecurringIncome]],
      ['four', four],
    ])('goes to the repeating income screen when there are %s', async (_name, incomes) => {
      const { router } = await renderBudget({ recurringIncomes: incomes })

      fireEvent.click(await findRepeatingRow())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.budgetRepeating)
      })
      expect(screen.queryByRole('dialog')).toBeNull()
    })

    it('goes to the repeating income screen, not to a sheet, when there is none yet', async () => {
      const { router } = await renderBudget({ recurringIncomes: [] })

      fireEvent.click(await findRepeatingRow())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.budgetRepeating)
      })
      expect(screen.queryByRole('dialog')).toBeNull()
    })

    it('opens nothing before the row is tapped', async () => {
      await renderBudget({ recurringIncomes: [salaryRecurringIncome] })
      await findRepeatingRow()

      expect(screen.queryByRole('dialog')).toBeNull()
    })

    it('finds the row by its name in Macedonian too', async () => {
      await renderBudget({ language: 'mk', recurringIncomes: [salaryRecurringIncome] })

      expect(await screen.findByRole('button', { name: repeatingRowName('mk') })).toBeTruthy()
    })
  })
})
