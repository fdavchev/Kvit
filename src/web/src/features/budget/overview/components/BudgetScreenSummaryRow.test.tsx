import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import { findLimitsCard, renderBudgetWithLimits, setLimitButton } from '@/test/budgetLimitTestHelpers'
import { octoberWithLimits } from '@/test/budgetLimitTestData'
import {
  budgetNow,
  categoryTotalsRegion,
  findEntryList,
  findTotalCard,
  monthButton,
  personalEntriesGet,
  renderBudget,
} from '@/test/budgetTestHelpers'
import { isBefore } from '@/test/expenseTestHelpers'
import { emptyMonth, octoberMonth, septemberWithArchivedCategory } from '@/test/personalEntryTestData'
import { findRepeatingRow } from '@/test/recurringIncomeTestHelpers'
import { recurringIncomesGet } from '@/test/recurringIncomeTestData'
import { networkFailureAnswer, neverAnswer, requestCount } from '@/test/requestTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

function summaryLink(language: 'en' | 'mk' = 'en'): HTMLElement {
  return screen.getByRole('link', { name: translated(language, 'budget.summary.row') })
}

function findSummaryLink(language: 'en' | 'mk' = 'en'): Promise<HTMLElement> {
  return screen.findByRole('link', { name: translated(language, 'budget.summary.row') })
}

function querySummaryLink(language: 'en' | 'mk' = 'en'): HTMLElement | null {
  return screen.queryByRole('link', { name: translated(language, 'budget.summary.row') })
}

describe('BudgetScreen, the "Summary" row', () => {
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

  describe('what it is', () => {
    it.each(languages)('is a link named "Summary" that opens the summary of the month on screen (%s)', async (language) => {
      await renderBudget({ language, months: [octoberMonth] })

      const link = await findSummaryLink(language)

      expect(link.getAttribute('href')).toBe(routes.budgetSummaryOf('2026-10'))
    })

    it('shows exactly one Summary link', async () => {
      await renderBudget({ months: [octoberMonth] })
      await findSummaryLink()

      expect(screen.getAllByRole('link', { name: translated('en', 'budget.summary.row') })).toHaveLength(1)
    })

    it('hides its emoji and its arrow from a screen reader, so the link is named only "Summary"', async () => {
      await renderBudget({ months: [octoberMonth] })
      const link = await findSummaryLink()

      expect(link.textContent).toContain(translated('en', 'budget.summary.row'))
      expect(link.querySelectorAll('[aria-hidden="true"]').length).toBeGreaterThanOrEqual(2)
    })

    it('is at least 44 px tall, like the Repeating income row', async () => {
      await renderBudget({ months: [octoberMonth] })
      const link = await findSummaryLink()

      const classes = Array.from(link.classList)

      expect(classes.includes('min-h-11') || classes.includes('min-h-16')).toBe(true)
      expect(classes).toContain('w-full')
    })

    it('does not ask for the summary on the Budget tab, only the screen of the summary does', async () => {
      const { fetchMock } = await renderBudget({ months: [octoberMonth] })
      await findSummaryLink()

      expect(requestCount(fetchMock, 'GET', '/api/personal/summary?month=2026-10')).toBe(0)
    })
  })

  describe('the month it opens', () => {
    it('opens the month that the month strip selects: September after a tap on Sep', async () => {
      await renderBudget({ months: [octoberMonth, septemberWithArchivedCategory] })
      await findTotalCard()

      fireEvent.click(monthButton('Sep'))

      await waitFor(() => {
        expect(summaryLink().getAttribute('href')).toBe(routes.budgetSummaryOf('2026-09'))
      })
    })

    it('goes back to October when the strip goes back to Oct', async () => {
      await renderBudget({ months: [octoberMonth, septemberWithArchivedCategory] })
      await findTotalCard()
      fireEvent.click(monthButton('Sep'))
      await waitFor(() => {
        expect(summaryLink().getAttribute('href')).toBe(routes.budgetSummaryOf('2026-09'))
      })

      fireEvent.click(monthButton('Oct'))

      await waitFor(() => {
        expect(summaryLink().getAttribute('href')).toBe(routes.budgetSummaryOf('2026-10'))
      })
    })
  })

  describe('when it shows', () => {
    it('shows in a month with no entries at all, because the previous month may have some', async () => {
      await renderBudget({ months: [emptyMonth('2026-10')] })

      expect(await findSummaryLink()).toBeTruthy()
    })

    it('does not show while the month is loading', async () => {
      await renderBudget({ answers: { [personalEntriesGet('2026-10')]: neverAnswer() } })

      await waitFor(() => {
        expect(screen.getByRole('status')).toBeTruthy()
      })
      expect(querySummaryLink()).toBeNull()
    })

    it('does not show when the month cannot be loaded', async () => {
      await renderBudget({ answers: { [personalEntriesGet('2026-10')]: networkFailureAnswer() } })

      expect(await screen.findByRole('alert')).toBeTruthy()
      expect(querySummaryLink()).toBeNull()
    })

    it('still shows when the list of repeating incomes cannot be loaded', async () => {
      await renderBudget({ months: [octoberMonth], answers: { [recurringIncomesGet]: networkFailureAnswer() } })

      expect(await findSummaryLink()).toBeTruthy()
    })

    it('still shows while the list of repeating incomes is on its way', async () => {
      await renderBudget({ months: [octoberMonth], answers: { [recurringIncomesGet]: neverAnswer() } })

      expect(await findSummaryLink()).toBeTruthy()
    })
  })

  describe('where it sits', () => {
    it('sits under the total card and the Repeating income row, above the category tiles and the entries', async () => {
      await renderBudget({ months: [octoberMonth] })
      const total = await findTotalCard()
      const repeating = await findRepeatingRow()
      const list = await findEntryList()

      const link = summaryLink()

      expect(isBefore(total, link)).toBe(true)
      expect(isBefore(repeating, link)).toBe(true)
      expect(isBefore(link, categoryTotalsRegion())).toBe(true)
      expect(isBefore(link, list)).toBe(true)
    })

    it('sits above the Limits card', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const limits = await findLimitsCard()

      expect(isBefore(summaryLink(), limits)).toBe(true)
    })

    it('sits under the Repeating income row and above the one-line "Set a limit" row in an empty month', async () => {
      await renderBudget({ months: [emptyMonth('2026-10')] })
      const repeating = await findRepeatingRow()

      const link = await findSummaryLink()

      expect(isBefore(repeating, link)).toBe(true)
      expect(isBefore(link, setLimitButton())).toBe(true)
    })
  })
})
