import { act, fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { renderGroupApp } from '@/test/activityTestHelpers'
import { budgetNow, monthAnswers, pressedMonthLabels } from '@/test/budgetTestHelpers'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { personalCategoriesWithArchived } from '@/test/personalCategoryTestData'
import { categoryListAnswer, personalCategoriesWithArchivedGet } from '@/test/personalCategoryTestHelpers'
import { emptyMonth, octoberMonth, septemberWithArchivedCategory } from '@/test/personalEntryTestData'
import { findSummaryHeading, summaryAnswers, summaryTitle } from '@/test/personalSummaryTestHelpers'
import { emptyOctoberSummary, octoberSummary } from '@/test/personalSummaryTestData'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { routes } from './routes'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const augustSummary = { ...emptyOctoberSummary, month: '2026-08', previousMonth: '2026-07' }
const septemberSummary = { ...emptyOctoberSummary, month: '2026-09', previousMonth: '2026-08' }

const answers = {
  ...monthAnswers([octoberMonth, septemberWithArchivedCategory, emptyMonth('2026-08'), emptyMonth('2026-07')]),
  ...summaryAnswers([octoberSummary, septemberSummary, augustSummary]),
  [personalCategoriesWithArchivedGet]: categoryListAnswer(() => personalCategoriesWithArchived),
}

function summaryRow(): HTMLElement {
  return screen.getByRole('link', { name: translated('en', 'budget.summary.row') })
}

function backLink(): HTMLElement {
  return screen.getByRole('link', { name: translated('en', 'common.back') })
}

function budgetTabInBottomBar(): HTMLElement {
  return within(screen.getByRole('navigation')).getByRole('link', { name: translated('en', 'nav.budget') })
}

describe('the month of the Budget tab in the address, followed through the real route table', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(budgetNow)
    stubDeviceColorScheme('light')
    stubGoogleSignIn()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
  })

  describe('Back from the Summary', () => {
    it('returns to August after a tap on Aug in the strip, the Summary row and the Back link', async () => {
      const { router } = await renderGroupApp(routes.budget, { answers })
      await screen.findByRole('region', { name: translated('en', 'budget.screen.spentThisMonth') })
      fireEvent.click(screen.getByRole('button', { name: 'Aug' }))
      await waitFor(() => {
        expect(summaryRow().getAttribute('href')).toBe(routes.budgetSummaryOf('2026-08'))
      })
      fireEvent.click(summaryRow())
      await findSummaryHeading(summaryTitle('2026-08', '2026-07', '2026-10'))

      fireEvent.click(backLink())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.budget)
      })
      expect(router.state.location.search).toBe('?month=2026-08')
      await waitFor(() => {
        expect(pressedMonthLabels()).toEqual(['Aug'])
      })
    })

    it('returns to August when the Budget tab was opened by an address with that month', async () => {
      const { router } = await renderGroupApp(routes.budgetOf('2026-08'), { answers })
      await waitFor(() => {
        expect(pressedMonthLabels()).toEqual(['Aug'])
      })
      fireEvent.click(await screen.findByRole('link', { name: translated('en', 'budget.summary.row') }))
      await findSummaryHeading(summaryTitle('2026-08', '2026-07', '2026-10'))

      fireEvent.click(backLink())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.budget)
      })
      await waitFor(() => {
        expect(pressedMonthLabels()).toEqual(['Aug'])
      })
    })

    it('returns to August when the Summary was opened by an address with that month', async () => {
      const { router } = await renderGroupApp(routes.budgetSummaryOf('2026-08'), { answers })
      await findSummaryHeading(summaryTitle('2026-08', '2026-07', '2026-10'))

      fireEvent.click(backLink())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.budget)
      })
      expect(router.state.location.search).toBe('?month=2026-08')
      await waitFor(() => {
        expect(pressedMonthLabels()).toEqual(['Aug'])
      })
    })

    it('points the Back link at the Budget tab with the month of the summary on screen', async () => {
      await renderGroupApp(routes.budgetSummaryOf('2026-08'), { answers })
      await findSummaryHeading(summaryTitle('2026-08', '2026-07', '2026-10'))

      expect(backLink().getAttribute('href')).toBe(routes.budgetOf('2026-08'))
    })

    it('goes to the current month when the Summary was opened by an address with no month', async () => {
      const { router } = await renderGroupApp(routes.budgetSummary, { answers })
      await findSummaryHeading(summaryTitle('2026-10', '2026-09', '2026-10'))

      fireEvent.click(backLink())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.budget)
      })
      await waitFor(() => {
        expect(pressedMonthLabels()).toEqual(['Oct'])
      })
    })

    it('returns to the same month a second time after another round', async () => {
      const { router } = await renderGroupApp(routes.budgetOf('2026-09'), { answers })
      fireEvent.click(await screen.findByRole('link', { name: translated('en', 'budget.summary.row') }))
      await findSummaryHeading(summaryTitle('2026-09', '2026-08', '2026-10'))
      fireEvent.click(backLink())
      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.budget)
      })

      fireEvent.click(await screen.findByRole('link', { name: translated('en', 'budget.summary.row') }))
      await findSummaryHeading(summaryTitle('2026-09', '2026-08', '2026-10'))
      fireEvent.click(backLink())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.budget)
      })
      await waitFor(() => {
        expect(pressedMonthLabels()).toEqual(['Sep'])
      })
    })
  })

  describe('the bottom bar', () => {
    it('keeps the Budget tab pointing at /budget with no month', async () => {
      await renderGroupApp(routes.budgetOf('2026-08'), { answers })
      await waitFor(() => {
        expect(pressedMonthLabels()).toEqual(['Aug'])
      })

      expect(budgetTabInBottomBar().getAttribute('href')).toBe(routes.budget)
    })

    it('keeps Budget marked as the current tab while a month is in the address', async () => {
      await renderGroupApp(routes.budgetOf('2026-08'), { answers })
      await waitFor(() => {
        expect(pressedMonthLabels()).toEqual(['Aug'])
      })

      expect(budgetTabInBottomBar().getAttribute('aria-current')).toBe('page')
    })

    it('opens the current month when the person taps the Budget tab while looking at August', async () => {
      const { router } = await renderGroupApp(routes.budgetOf('2026-08'), { answers })
      await waitFor(() => {
        expect(pressedMonthLabels()).toEqual(['Aug'])
      })

      fireEvent.click(budgetTabInBottomBar())

      await waitFor(() => {
        expect(pressedMonthLabels()).toEqual(['Oct'])
      })
      expect(router.state.location.pathname).toBe(routes.budget)
      expect(router.state.location.search).toBe('')
    })

    it('opens the current month when the person comes from another tab to the Budget tab', async () => {
      const { router } = await renderGroupApp(routes.groups, { answers })
      await screen.findByRole('navigation')

      fireEvent.click(budgetTabInBottomBar())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.budget)
      })
      await waitFor(() => {
        expect(pressedMonthLabels()).toEqual(['Oct'])
      })
    })
  })

  describe('the browser Back', () => {
    it('leaves the Budget tab at once after several month taps, because each tap replaces the entry', async () => {
      const { router } = await renderGroupApp(routes.groups, { answers })
      await screen.findByRole('navigation')
      fireEvent.click(budgetTabInBottomBar())
      await screen.findByRole('region', { name: translated('en', 'budget.screen.spentThisMonth') })

      fireEvent.click(screen.getByRole('button', { name: 'Sep' }))
      await waitFor(() => {
        expect(router.state.location.search).toBe('?month=2026-09')
      })
      fireEvent.click(screen.getByRole('button', { name: 'Aug' }))
      await waitFor(() => {
        expect(router.state.location.search).toBe('?month=2026-08')
      })
      fireEvent.click(screen.getByRole('button', { name: 'Jul' }))
      await waitFor(() => {
        expect(router.state.location.search).toBe('?month=2026-07')
      })

      await act(() => router.navigate(-1))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groups)
      })
    })

    it('comes back from the Summary to the month of the Budget tab with one step back', async () => {
      const { router } = await renderGroupApp(routes.budget, { answers })
      await screen.findByRole('region', { name: translated('en', 'budget.screen.spentThisMonth') })
      fireEvent.click(screen.getByRole('button', { name: 'Sep' }))
      await waitFor(() => {
        expect(router.state.location.search).toBe('?month=2026-09')
      })
      fireEvent.click(await screen.findByRole('link', { name: translated('en', 'budget.summary.row') }))
      await findSummaryHeading(summaryTitle('2026-09', '2026-08', '2026-10'))

      await act(() => router.navigate(-1))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.budget)
      })
      expect(router.state.location.search).toBe('?month=2026-09')
      await waitFor(() => {
        expect(pressedMonthLabels()).toEqual(['Sep'])
      })
    })
  })
})
