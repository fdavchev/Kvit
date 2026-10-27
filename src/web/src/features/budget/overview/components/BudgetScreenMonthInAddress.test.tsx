import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { endpoints } from '@/core/api/endpoints'
import { routes } from '@/core/router/routes'
import {
  budgetNow,
  findTotalCard,
  monthButton,
  pressedMonthLabels,
  renderBudget,
} from '@/test/budgetTestHelpers'
import { emptyMonth, octoberMonth, septemberWithArchivedCategory } from '@/test/personalEntryTestData'
import { requestCount } from '@/test/requestTestHelpers'
import { testMe } from '@/test/testMe'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const augustMonth = emptyMonth('2026-08')

function budgetAt(month: string): string {
  return `${routes.budget}?month=${month}`
}

function entryRequestCount(fetchMock: Parameters<typeof requestCount>[0]): number {
  return fetchMock.mock.calls.filter(([input]) => String(input).startsWith('/api/personal/entries')).length
}

describe('BudgetScreen, the month in the address', () => {
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

  describe('reading the month', () => {
    it('shows August as the selected month when the address is /budget?month=2026-08', async () => {
      await renderBudget({ startAt: budgetAt('2026-08'), months: [octoberMonth, augustMonth] })

      await waitFor(() => {
        expect(pressedMonthLabels()).toEqual(['Aug'])
      })
    })

    it('asks only for the month in the address, not for the current month', async () => {
      const { fetchMock } = await renderBudget({ startAt: budgetAt('2026-08'), months: [octoberMonth, augustMonth] })
      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', endpoints.personalEntriesOfMonth('2026-08'))).toBe(1)
      })

      expect(requestCount(fetchMock, 'GET', endpoints.personalEntriesOfMonth('2026-10'))).toBe(0)
    })

    it('shows the current month when the address has no month', async () => {
      await renderBudget({ startAt: routes.budget, months: [octoberMonth] })
      await findTotalCard()

      expect(pressedMonthLabels()).toEqual(['Oct'])
    })

    it('takes the current month in the time zone of the person when the address has no month', async () => {
      vi.setSystemTime(new Date('2026-10-31T23:30:00Z'))

      const { fetchMock } = await renderBudget({
        startAt: routes.budget,
        me: { ...testMe, timeZone: 'Europe/Skopje' },
        months: [emptyMonth('2026-11'), octoberMonth],
      })

      await waitFor(() => {
        expect(pressedMonthLabels()).toEqual(['Nov'])
      })
      expect(requestCount(fetchMock, 'GET', endpoints.personalEntriesOfMonth('2026-11'))).toBe(1)
      expect(requestCount(fetchMock, 'GET', endpoints.personalEntriesOfMonth('2026-10'))).toBe(0)
    })

    it('accepts the oldest month of the strip', async () => {
      await renderBudget({ startAt: budgetAt('2024-11'), months: [octoberMonth, emptyMonth('2024-11')] })

      await waitFor(() => {
        expect(pressedMonthLabels()).toEqual(['Nov 2024'])
      })
    })

    it('opens the summary of the month in the address from the Summary row', async () => {
      await renderBudget({ startAt: budgetAt('2026-08'), months: [octoberMonth, augustMonth] })

      const link = await screen.findByRole('link', { name: translated('en', 'budget.summary.row') })

      expect(link.getAttribute('href')).toBe(routes.budgetSummaryOf('2026-08'))
    })
  })

  describe('a month in the address that the strip cannot show', () => {
    it.each([
      ['text', 'october'],
      ['a month number above 12', '2026-13'],
      ['a month number of 0', '2026-00'],
      ['a month number with one digit', '2026-8'],
      ['a whole day', '2026-08-05'],
      ['a month after the current one', '2026-11'],
      ['the month before the oldest one of the strip', '2024-10'],
      ['an empty month', ''],
    ])('falls back to the current month, with no error screen and no request for it: %s', async (_name, month) => {
      const { fetchMock } = await renderBudget({ startAt: budgetAt(month), months: [octoberMonth] })

      await findTotalCard()

      expect(pressedMonthLabels()).toEqual(['Oct'])
      expect(screen.queryByRole('alert')).toBeNull()
      expect(requestCount(fetchMock, 'GET', endpoints.personalEntriesOfMonth('2026-10'))).toBe(1)
      expect(entryRequestCount(fetchMock)).toBe(1)
    })

    it('opens the summary of the current month from the Summary row after the fallback', async () => {
      await renderBudget({ startAt: budgetAt('october'), months: [octoberMonth] })

      const link = await screen.findByRole('link', { name: translated('en', 'budget.summary.row') })

      expect(link.getAttribute('href')).toBe(routes.budgetSummaryOf('2026-10'))
    })

    it('still lets the person pick another month in the strip after the fallback', async () => {
      const { router } = await renderBudget({
        startAt: budgetAt('october'),
        months: [octoberMonth, septemberWithArchivedCategory],
      })
      await findTotalCard()

      fireEvent.click(monthButton('Sep'))

      await waitFor(() => {
        expect(pressedMonthLabels()).toEqual(['Sep'])
      })
      expect(router.state.location.search).toBe('?month=2026-09')
    })
  })

  describe('picking a month in the strip', () => {
    it('writes the picked month into the address', async () => {
      const { router } = await renderBudget({ months: [octoberMonth, septemberWithArchivedCategory] })
      await findTotalCard()

      fireEvent.click(monthButton('Sep'))

      await waitFor(() => {
        expect(router.state.location.search).toBe('?month=2026-09')
      })
      expect(router.state.location.pathname).toBe(routes.budget)
      expect(pressedMonthLabels()).toEqual(['Sep'])
    })

    it('writes the current month into the address too when the person picks it again', async () => {
      const { router } = await renderBudget({ months: [octoberMonth, septemberWithArchivedCategory] })
      await findTotalCard()
      fireEvent.click(monthButton('Sep'))
      await waitFor(() => {
        expect(router.state.location.search).toBe('?month=2026-09')
      })

      fireEvent.click(monthButton('Oct'))

      await waitFor(() => {
        expect(router.state.location.search).toBe('?month=2026-10')
      })
      expect(pressedMonthLabels()).toEqual(['Oct'])
    })

    it('moves from the month in the address to the picked month', async () => {
      const { router } = await renderBudget({
        startAt: budgetAt('2026-08'),
        months: [octoberMonth, septemberWithArchivedCategory, augustMonth],
      })
      await waitFor(() => {
        expect(pressedMonthLabels()).toEqual(['Aug'])
      })

      fireEvent.click(monthButton('Sep'))

      await waitFor(() => {
        expect(router.state.location.search).toBe('?month=2026-09')
      })
      expect(pressedMonthLabels()).toEqual(['Sep'])
    })

    it('replaces the history entry instead of adding one, so Back does not walk through every month', async () => {
      const { router } = await renderBudget({ months: [octoberMonth, septemberWithArchivedCategory, augustMonth] })
      await findTotalCard()

      fireEvent.click(monthButton('Sep'))
      await waitFor(() => {
        expect(router.state.location.search).toBe('?month=2026-09')
      })
      fireEvent.click(monthButton('Aug'))
      await waitFor(() => {
        expect(router.state.location.search).toBe('?month=2026-08')
      })

      expect(router.state.historyAction).toBe('REPLACE')
    })
  })
})
