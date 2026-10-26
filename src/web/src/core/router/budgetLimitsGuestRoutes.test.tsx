import { screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { renderGroupApp } from '@/test/activityTestHelpers'
import { queryLimitsCard, querySetLimitButton } from '@/test/budgetLimitTestHelpers'
import { octoberWithLimits } from '@/test/budgetLimitTestData'
import { budgetNow, monthAnswers } from '@/test/budgetTestHelpers'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { jsonAnswer } from '@/test/requestTestHelpers'
import { testGuestMe } from '@/test/testMe'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { routes } from './routes'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

describe('the Limits card for a guest', () => {
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

  it('is never shown: a guest who opens the Budget tab lands on Settings and sends no request about limits', async () => {
    const { router, fetchMock } = await renderGroupApp(routes.budget, {
      answers: { ...monthAnswers([octoberWithLimits]), 'GET /api/me': jsonAnswer(testGuestMe) },
    })

    expect(await screen.findByRole('heading', { name: translated('en', 'settings.guest.title') })).toBeTruthy()

    expect(router.state.location.pathname).toBe(routes.settings)
    expect(queryLimitsCard()).toBeNull()
    expect(querySetLimitButton()).toBeNull()
    const limitRequests = fetchMock.mock.calls.filter(([input]) => String(input).includes('/api/personal/'))
    expect(limitRequests).toHaveLength(0)
  })
})
