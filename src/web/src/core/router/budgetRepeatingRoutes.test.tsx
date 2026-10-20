import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { renderGroupApp } from '@/test/activityTestHelpers'
import { budgetNow, monthAnswers } from '@/test/budgetTestHelpers'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { octoberMonth } from '@/test/personalEntryTestData'
import { findRepeatingRow, renderRepeatingScreen } from '@/test/recurringIncomeTestHelpers'
import {
  flatRentRecurringIncome,
  recurringIncomesGet,
  recurringListAnswer,
  salaryRecurringIncome,
  tutoringRecurringIncome,
} from '@/test/recurringIncomeTestData'
import { jsonAnswer, requestCount } from '@/test/requestTestHelpers'
import { testGuestMe } from '@/test/testMe'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { routes } from './routes'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const listUrl = '/api/personal/recurring-incomes'
const catchUpUrl = '/api/personal/recurring-incomes/catch-up'
const three = [salaryRecurringIncome, flatRentRecurringIncome, tutoringRecurringIncome]

describe('the repeating income route followed through the real route table', () => {
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

  it('has the route at /budget/repeating', () => {
    expect(routes.budgetRepeating).toBe('/budget/repeating')
  })

  it('shows the repeating income screen to a person with an account', async () => {
    const { router } = await renderRepeatingScreen({ recurringIncomes: three })

    expect(await screen.findByRole('heading', { level: 1, name: translated('en', 'budget.recurring.title') })).toBeTruthy()
    expect(router.state.location.pathname).toBe(routes.budgetRepeating)
  })

  it('sends a guest to Settings and never asks for the list or the catch-up', async () => {
    const { router, fetchMock } = await renderGroupApp(routes.budgetRepeating, {
      answers: { 'GET /api/me': jsonAnswer(testGuestMe), [recurringIncomesGet]: recurringListAnswer(three) },
    })

    expect(await screen.findByRole('heading', { name: translated('en', 'settings.guest.title') })).toBeTruthy()
    expect(router.state.location.pathname).toBe(routes.settings)
    expect(router.state.historyAction).toBe('REPLACE')
    expect(requestCount(fetchMock, 'GET', listUrl)).toBe(0)
    expect(requestCount(fetchMock, 'POST', catchUpUrl)).toBe(0)
  })

  it('sends a guest who opens the Budget tab to Settings and makes no repeating income request either', async () => {
    const { fetchMock } = await renderGroupApp(routes.budget, {
      answers: { ...monthAnswers([octoberMonth]), 'GET /api/me': jsonAnswer(testGuestMe) },
    })
    await screen.findByRole('heading', { name: translated('en', 'settings.guest.title') })

    expect(requestCount(fetchMock, 'GET', listUrl)).toBe(0)
    expect(requestCount(fetchMock, 'POST', catchUpUrl)).toBe(0)
  })

  it('takes a person from the row with three incomes to the screen and back to the Budget tab', async () => {
    const { router, fetchMock } = await renderGroupApp(routes.budget, {
      answers: { ...monthAnswers([octoberMonth]), [recurringIncomesGet]: recurringListAnswer(three) },
    })

    fireEvent.click(await findRepeatingRow())
    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.budgetRepeating)
    })
    expect(await screen.findByRole('heading', { level: 1, name: translated('en', 'budget.recurring.title') })).toBeTruthy()
    fireEvent.click(screen.getByRole('link', { name: translated('en', 'common.back') }))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.budget)
    })
    expect(await screen.findByRole('region', { name: translated('en', 'budget.screen.spentThisMonth') })).toBeTruthy()
    expect(requestCount(fetchMock, 'POST', catchUpUrl)).toBe(2)
  })

  it('sends the catch-up each time the Budget tab is opened again from the bottom bar', async () => {
    const { router, fetchMock } = await renderGroupApp(routes.budget, { answers: monthAnswers([octoberMonth]) })
    await screen.findByRole('region', { name: translated('en', 'budget.screen.spentThisMonth') })
    await waitFor(() => {
      expect(requestCount(fetchMock, 'POST', catchUpUrl)).toBe(1)
    })

    const bar = screen.getByRole('navigation')
    fireEvent.click(within(bar).getByRole('link', { name: translated('en', 'nav.groups') }))
    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.groups)
    })
    fireEvent.click(await within(screen.getByRole('navigation')).findByRole('link', { name: translated('en', 'nav.budget') }))

    await waitFor(() => {
      expect(requestCount(fetchMock, 'POST', catchUpUrl)).toBe(2)
    })
  })

  it('does not send the catch-up on the Groups tab', async () => {
    const { fetchMock } = await renderGroupApp(routes.groups)
    await screen.findByRole('navigation')

    expect(requestCount(fetchMock, 'POST', catchUpUrl)).toBe(0)
    expect(requestCount(fetchMock, 'GET', listUrl)).toBe(0)
  })
})
