import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { renderGroupApp } from '@/test/activityTestHelpers'
import { budgetNow, monthAnswers } from '@/test/budgetTestHelpers'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { octoberMonth } from '@/test/personalEntryTestData'
import { jsonAnswer, requestCount } from '@/test/requestTestHelpers'
import { testGuestMe } from '@/test/testMe'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { routes } from './routes'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const budgetAnswers = monthAnswers([octoberMonth])

describe('the Budget route followed through the real route table', () => {
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

  it('shows the Budget screen with the bottom bar, Budget marked as the current tab', async () => {
    const { router } = await renderGroupApp(routes.budget, { answers: budgetAnswers })

    expect(await screen.findByRole('heading', { level: 1, name: translated('en', 'nav.budget') })).toBeTruthy()
    expect(await screen.findByRole('region', { name: translated('en', 'budget.screen.spentThisMonth') })).toBeTruthy()
    const bar = screen.getByRole('navigation')
    const current = within(bar)
      .getAllByRole('link')
      .filter((link) => link.getAttribute('aria-current') === 'page')
    expect(current.map((link) => link.textContent)).toEqual([translated('en', 'nav.budget')])
    expect(router.state.location.pathname).toBe(routes.budget)
  })

  it('takes a person from the groups list to the Budget tab through the bottom bar', async () => {
    const { router } = await renderGroupApp(routes.groups, { answers: budgetAnswers })
    const bar = await screen.findByRole('navigation')

    fireEvent.click(within(bar).getByRole('link', { name: translated('en', 'nav.budget') }))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.budget)
    })
    expect(await screen.findByRole('region', { name: translated('en', 'budget.screen.spentThisMonth') })).toBeTruthy()
  })

  it('sends a guest who opens the Budget tab to Settings and never asks for personal entries', async () => {
    const { router, fetchMock } = await renderGroupApp(routes.budget, {
      answers: { ...budgetAnswers, 'GET /api/me': jsonAnswer(testGuestMe) },
    })

    expect(await screen.findByRole('heading', { name: translated('en', 'settings.guest.title') })).toBeTruthy()
    expect(router.state.location.pathname).toBe(routes.settings)
    expect(router.state.historyAction).toBe('REPLACE')
    expect(requestCount(fetchMock, 'GET', '/api/personal/entries?month=2026-10')).toBe(0)
  })

  it('shows a guest the bottom bar without the Budget tab', async () => {
    await renderGroupApp(routes.groups, { answers: { 'GET /api/me': jsonAnswer(testGuestMe) } })

    const bar = await screen.findByRole('navigation')

    expect(within(bar).queryByRole('link', { name: translated('en', 'nav.budget') })).toBeNull()
    expect(within(bar).getAllByRole('link')).toHaveLength(3)
  })
})
