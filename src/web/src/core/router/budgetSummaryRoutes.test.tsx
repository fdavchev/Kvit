import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { renderGroupApp } from '@/test/activityTestHelpers'
import { budgetNow, monthAnswers } from '@/test/budgetTestHelpers'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { personalCategoriesWithArchived } from '@/test/personalCategoryTestData'
import { categoryListAnswer, personalCategoriesWithArchivedGet } from '@/test/personalCategoryTestHelpers'
import { octoberMonth, septemberWithArchivedCategory } from '@/test/personalEntryTestData'
import { findCurrencyCard, findSummaryHeading, summaryAnswers, summaryPath, summaryTitle } from '@/test/personalSummaryTestHelpers'
import { octoberSummary } from '@/test/personalSummaryTestData'
import { jsonAnswer, problemAnswer, requestCount, stubFetchByRequest } from '@/test/requestTestHelpers'
import { testGuestMe } from '@/test/testMe'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { routeObjects } from './router'
import { routes } from './routes'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const summaryUrl = '/api/personal/summary?month=2026-10'
const septemberSummaryUrl = '/api/personal/summary?month=2026-09'
const categoriesUrl = '/api/personal/categories?includeArchived=true'
const categoryAnswers = { [personalCategoriesWithArchivedGet]: categoryListAnswer(() => personalCategoriesWithArchived) }

describe('the monthly summary route followed through the real route table', () => {
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

  it('shows the summary to a person with an account, with the bottom bar and Budget marked as the current tab', async () => {
    const { router } = await renderGroupApp(summaryPath('2026-10'), {
      answers: {
        ...summaryAnswers([octoberSummary]),
        ...categoryAnswers,
      },
    })

    expect(await findSummaryHeading(summaryTitle('2026-10', '2026-09', '2026-10'))).toBeTruthy()
    const bar = screen.getByRole('navigation')
    const current = within(bar)
      .getAllByRole('link')
      .filter((link) => link.getAttribute('aria-current') === 'page')
    expect(current.map((link) => link.textContent)).toEqual([translated('en', 'nav.budget')])
    expect(router.state.location.pathname).toBe(routes.budgetSummary)
  })

  it('sends a guest to Settings and never asks for the summary or the categories', async () => {
    const { router, fetchMock } = await renderGroupApp(summaryPath('2026-10'), {
      answers: { ...summaryAnswers([octoberSummary]), 'GET /api/me': jsonAnswer(testGuestMe) },
    })

    expect(await screen.findByRole('heading', { name: translated('en', 'settings.guest.title') })).toBeTruthy()
    expect(router.state.location.pathname).toBe(routes.settings)
    expect(router.state.historyAction).toBe('REPLACE')
    expect(requestCount(fetchMock, 'GET', summaryUrl)).toBe(0)
    expect(requestCount(fetchMock, 'GET', categoriesUrl)).toBe(0)
  })

  it('sends a guest who opens the address with no month to Settings as well', async () => {
    const { router, fetchMock } = await renderGroupApp(routes.budgetSummary, {
      answers: { ...summaryAnswers([octoberSummary]), 'GET /api/me': jsonAnswer(testGuestMe) },
    })

    expect(await screen.findByRole('heading', { name: translated('en', 'settings.guest.title') })).toBeTruthy()
    expect(router.state.location.pathname).toBe(routes.settings)
    expect(requestCount(fetchMock, 'GET', summaryUrl)).toBe(0)
  })

  it('sends a guest who opens the Budget tab to Settings and makes no summary request either', async () => {
    const { fetchMock } = await renderGroupApp(routes.budget, {
      answers: { ...monthAnswers([octoberMonth]), 'GET /api/me': jsonAnswer(testGuestMe) },
    })
    await screen.findByRole('heading', { name: translated('en', 'settings.guest.title') })

    expect(screen.queryByRole('link', { name: translated('en', 'budget.summary.row') })).toBeNull()
    expect(requestCount(fetchMock, 'GET', summaryUrl)).toBe(0)
  })

  it('sends a signed-out visitor to the welcome page and asks for no summary', async () => {
    const fetchMock = stubFetchByRequest({ 'GET /api/me': problemAnswer(401, 'AUTH_NOT_SIGNED_IN') })

    const { router } = await renderRoutesWithProviders(routeObjects, summaryPath('2026-10'))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.welcome)
    })
    expect(requestCount(fetchMock, 'GET', summaryUrl)).toBe(0)
  })

  it('takes a person from the Groups tab to the summary in 2 taps: Budget in the bottom bar, then Summary', async () => {
    const { router } = await renderGroupApp(routes.groups, {
      answers: {
        ...monthAnswers([octoberMonth]),
        ...summaryAnswers([octoberSummary]),
        ...categoryAnswers,
      },
    })
    let taps = 0

    fireEvent.click(within(await screen.findByRole('navigation')).getByRole('link', { name: translated('en', 'nav.budget') }))
    taps += 1
    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.budget)
    })
    fireEvent.click(await screen.findByRole('link', { name: translated('en', 'budget.summary.row') }))
    taps += 1

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.budgetSummary)
    })
    expect(taps).toBe(2)
    expect(router.state.location.search).toBe('?month=2026-10')
  })

  it('opens the summary of the month that is selected in the month strip, not of the current month', async () => {
    const { router, fetchMock } = await renderGroupApp(routes.budget, {
      answers: {
        ...monthAnswers([octoberMonth, septemberWithArchivedCategory]),
        ...summaryAnswers([{ ...octoberSummary, month: '2026-09', previousMonth: '2026-08', currencies: [] }]),
      },
    })
    await screen.findByRole('region', { name: translated('en', 'budget.screen.spentThisMonth') })
    fireEvent.click(screen.getByRole('button', { name: 'Sep' }))
    await waitFor(() => {
      expect(screen.getByRole('link', { name: translated('en', 'budget.summary.row') }).getAttribute('href')).toBe(
        routes.budgetSummaryOf('2026-09'),
      )
    })

    fireEvent.click(screen.getByRole('link', { name: translated('en', 'budget.summary.row') }))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.budgetSummary)
    })
    expect(router.state.location.search).toBe('?month=2026-09')
    expect(await findSummaryHeading(summaryTitle('2026-09', '2026-08', '2026-10'))).toBeTruthy()
    expect(requestCount(fetchMock, 'GET', septemberSummaryUrl)).toBe(1)
    expect(requestCount(fetchMock, 'GET', summaryUrl)).toBe(0)
  })

  it('goes back to the Budget tab with the Back link', async () => {
    const { router } = await renderGroupApp(summaryPath('2026-10'), {
      answers: {
        ...monthAnswers([octoberMonth]),
        ...summaryAnswers([octoberSummary]),
        ...categoryAnswers,
      },
    })
    await findSummaryHeading(summaryTitle('2026-10', '2026-09', '2026-10'))

    fireEvent.click(screen.getByRole('link', { name: translated('en', 'common.back') }))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.budget)
    })
    expect(await screen.findByRole('region', { name: translated('en', 'budget.screen.spentThisMonth') })).toBeTruthy()
  })

  it('shows the cards after a direct visit to the address, with the month in it', async () => {
    await renderGroupApp(summaryPath('2026-10'), { answers: { ...summaryAnswers([octoberSummary]), ...categoryAnswers } })

    expect(await findCurrencyCard('MKD')).toBeTruthy()
    expect(await findCurrencyCard('EUR')).toBeTruthy()
  })
})
