import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { BudgetScreen } from './BudgetScreen'
import { personalEntriesQueryKey } from '@/features/budget/entries/personalEntryCache'
import { routes } from '@/core/router/routes'
import { stubFetchThatNeverAnswers } from '@/test/apiTestHelpers'
import {
  budgetNow,
  findTotalCard,
  monthAnswer,
  monthButton,
  personalEntriesGet,
  plusButton,
  renderBudget,
} from '@/test/budgetTestHelpers'
import { restoreRealRetryPolicy } from '@/test/expenseTestHelpers'
import { emptyMonth, octoberMonth } from '@/test/personalEntryTestData'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { networkFailureAnswer, neverAnswer, problemAnswer, requestCount, type AnswerFactory } from '@/test/requestTestHelpers'
import { loadingLook } from '@/test/skeletonTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'

function failingThenAnswering(failure: AnswerFactory, success: AnswerFactory): AnswerFactory {
  let calls = 0
  return () => {
    calls += 1
    return calls === 1 ? failure() : success()
  }
}

describe('BudgetScreen states', () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(budgetNow)
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('an empty month', () => {
    it.each(languages)('says nothing was spent and points to "+", with no total card, tiles or entries (%s)', async (language) => {
      await renderBudget({ language, months: [emptyMonth('2026-10')] })

      expect(await screen.findByText(translated(language, 'budget.screen.emptyTitle'))).toBeTruthy()
      expect(screen.getByText(translated(language, 'expenses.emptyHint'))).toBeTruthy()
      expect(screen.queryByRole('region', { name: translated(language, 'budget.screen.spentThisMonth') })).toBeNull()
      expect(screen.queryByRole('region', { name: translated(language, 'budget.screen.categoryTotals') })).toBeNull()
      expect(screen.queryByRole('list', { name: translated(language, 'budget.screen.entries') })).toBeNull()
      expect(plusButton(language)).toBeTruthy()
    })

    it('keeps the month strip, so another month can still be picked', async () => {
      const { fetchMock } = await renderBudget({ months: [emptyMonth('2026-10'), { ...octoberMonth, month: '2026-09' }] })
      await screen.findByText(translated('en', 'budget.screen.emptyTitle'))

      fireEvent.click(monthButton('Sep'))

      expect(await findTotalCard()).toBeTruthy()
      expect(requestCount(fetchMock, 'GET', '/api/personal/entries?month=2026-09')).toBe(1)
    })
  })

  describe('loading', () => {
    it('shows skeletons shaped like the card, the tiles and the entries while the month loads', async () => {
      stubFetchThatNeverAnswers()

      await renderElementWithProviders(<BudgetScreen />, routes.budget, { seedCache: seedMe(testMe) })

      const look = loadingLook()
      expect(look.hasSpinner).toBe(false)
      expect(look.skeletonCount).toBeGreaterThanOrEqual(6)
      expect(screen.getByRole('status').textContent).toContain(translated('en', 'common.loading'))
    })

    it('shows the strip and the "+" while the month loads', async () => {
      stubFetchThatNeverAnswers()

      await renderElementWithProviders(<BudgetScreen />, routes.budget, { seedCache: seedMe(testMe) })

      expect(monthButton('Oct').getAttribute('aria-pressed')).toBe('true')
      expect(plusButton()).toBeTruthy()
    })

    it('shows skeletons for a month that was never loaded, not the month that was on screen before', async () => {
      const { fetchMock } = await renderBudget({
        answers: { [personalEntriesGet('2026-09')]: neverAnswer() },
      })
      await findTotalCard()

      fireEvent.click(monthButton('Sep'))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', '/api/personal/entries?month=2026-09')).toBe(1)
      })
      expect(loadingLook().skeletonCount).toBeGreaterThan(0)
      expect(screen.queryByRole('region', { name: translated('en', 'budget.screen.spentThisMonth') })).toBeNull()
    })

    it('shows the saved month at once, without skeletons, while it is checked with the server', async () => {
      stubFetchThatNeverAnswers()

      await renderElementWithProviders(<BudgetScreen />, routes.budget, {
        seedCache: (queryClient) => {
          seedMe(testMe)(queryClient)
          queryClient.setQueryData(personalEntriesQueryKey('2026-10'), octoberMonth, { updatedAt: 0 })
        },
      })

      expect(await findTotalCard()).toBeTruthy()
      expect(screen.queryByRole('status')).toBeNull()
    })
  })

  describe('an error', () => {
    it.each(languages)('shows the translated message for a server that cannot be reached, with Try again (%s)', async (language) => {
      await renderBudget({ language, answers: { [personalEntriesGet('2026-10')]: networkFailureAnswer() } })

      const alert = await screen.findByRole('alert')

      expect(within(alert).getByText(translated(language, 'errors.network'))).toBeTruthy()
      expect(within(alert).getByRole('button', { name: translated(language, 'common.retry') })).toBeTruthy()
    })

    it('shows the translated message of the error code the server sends', async () => {
      await renderBudget({ answers: { [personalEntriesGet('2026-10')]: problemAnswer(400, 'PERSONAL_ENTRY_MONTH_INVALID') } })

      const alert = await screen.findByRole('alert')

      expect(within(alert).getByText(translated('en', 'errors.PERSONAL_ENTRY_MONTH_INVALID'))).toBeTruthy()
    })

    it('asks the server only once and shows the error at once when the month is refused, with the retry policy of the app', async () => {
      const { fetchMock } = await renderBudget({
        seedCache: restoreRealRetryPolicy,
        answers: { [personalEntriesGet('2026-10')]: problemAnswer(400, 'PERSONAL_ENTRY_MONTH_INVALID') },
      })

      const alert = await screen.findByRole('alert')

      expect(within(alert).getByText(translated('en', 'errors.PERSONAL_ENTRY_MONTH_INVALID'))).toBeTruthy()
      expect(requestCount(fetchMock, 'GET', '/api/personal/entries?month=2026-10')).toBe(1)
    })

    it('asks the server again and shows the month when Try again is pressed', async () => {
      const { fetchMock } = await renderBudget({
        answers: {
          [personalEntriesGet('2026-10')]: failingThenAnswering(networkFailureAnswer(), monthAnswer(octoberMonth)),
        },
      })
      const alert = await screen.findByRole('alert')

      fireEvent.click(within(alert).getByRole('button', { name: translated('en', 'common.retry') }))

      expect(await findTotalCard()).toBeTruthy()
      expect(requestCount(fetchMock, 'GET', '/api/personal/entries?month=2026-10')).toBe(2)
      expect(screen.queryByRole('alert')).toBeNull()
    })
  })
})
