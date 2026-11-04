import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { adminStatsNow, defaultStatsUrl } from '@/test/adminStatsTestData'
import {
  findStatsTitle,
  renderAdminStats,
  statsRequestCount,
  statsRequests,
} from '@/test/adminStatsTestHelpers'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { neverAnswer, problemAnswer } from '@/test/requestTestHelpers'
import { seedMe, testAdminMe, testGuestMe, testMe } from '@/test/testMe'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { routes } from './routes'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

function notFoundHeading(): HTMLElement {
  return screen.getByRole('heading', { name: translated('en', 'notFound.title') })
}

describe('the admin statistics route followed through the real route table', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.clearAllMocks()
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(adminStatsNow)
    stubDeviceColorScheme('light')
    stubGoogleSignIn()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
  })

  it('shows the Statistics screen to an admin, with the bottom bar', async () => {
    const { router } = await renderAdminStats()

    expect(await findStatsTitle()).toBeTruthy()
    expect(screen.getByRole('navigation')).toBeTruthy()
    expect(router.state.location.pathname).toBe(routes.admin)
  })

  it.each(languages)('shows the title of the screen in the language of the admin (%s)', async (language) => {
    await renderAdminStats({ language })

    expect(await findStatsTitle(language)).toBeTruthy()
  })

  it('asks for the statistics of this month by day, once, after the admin is known', async () => {
    const { fetchMock } = await renderAdminStats()
    await findStatsTitle()

    await waitFor(() => {
      expect(statsRequestCount(fetchMock, defaultStatsUrl)).toBe(1)
    })
    expect(statsRequests(fetchMock)).toEqual([defaultStatsUrl])
  })

  it('shows the normal not-found screen at the same address to a signed-in person who is not an admin', async () => {
    const { router, fetchMock } = await renderAdminStats({ me: testMe })

    expect(await screen.findByRole('heading', { name: translated('en', 'notFound.title') })).toBeTruthy()
    expect(screen.getByRole('button', { name: translated('en', 'notFound.goHome') })).toBeTruthy()
    expect(router.state.location.pathname).toBe(routes.admin)
    expect(router.state.historyAction).toBe('POP')
    expect(screen.queryByRole('heading', { name: translated('en', 'adminStats.title') })).toBeNull()
    expect(statsRequests(fetchMock)).toEqual([])
  })

  it('shows not-found at once, with no statistics screen first, when me is already known to be a non-admin', async () => {
    const { fetchMock } = await renderAdminStats({ me: testMe, seedCache: seedMe(testMe) })

    expect(notFoundHeading()).toBeTruthy()
    expect(screen.queryByRole('heading', { name: translated('en', 'adminStats.title') })).toBeNull()
    expect(statsRequests(fetchMock)).toEqual([])
  })

  it('shows the statistics screen at once when me is already known to be an admin', async () => {
    await renderAdminStats({ seedCache: seedMe(testAdminMe) })

    expect(screen.getByRole('heading', { level: 1, name: translated('en', 'adminStats.title') })).toBeTruthy()
    expect(screen.queryByRole('heading', { name: translated('en', 'notFound.title') })).toBeNull()
  })

  it('shows the not-found screen to a guest at the same address, not a redirect to Settings', async () => {
    const { router, fetchMock } = await renderAdminStats({ me: testGuestMe })

    expect(await screen.findByRole('heading', { name: translated('en', 'notFound.title') })).toBeTruthy()
    expect(router.state.location.pathname).toBe(routes.admin)
    expect(router.state.historyAction).toBe('POP')
    expect(screen.queryByRole('heading', { name: translated('en', 'settings.guest.title') })).toBeNull()
    expect(statsRequests(fetchMock)).toEqual([])
  })

  it.each(languages)('writes the not-found screen to a non-admin in the language of the person (%s)', async (language) => {
    await renderAdminStats({ me: testMe, language })

    expect(await screen.findByRole('heading', { name: translated(language, 'notFound.title') })).toBeTruthy()
  })

  it('requests nothing for the screen while me is still being asked', async () => {
    const { fetchMock } = await renderAdminStats({ answers: { 'GET /api/me': neverAnswer() } })

    expect(screen.getByRole('status')).toBeTruthy()
    expect(screen.queryByRole('heading', { name: translated('en', 'adminStats.title') })).toBeNull()
    expect(screen.queryByRole('heading', { name: translated('en', 'notFound.title') })).toBeNull()
    expect(statsRequests(fetchMock)).toEqual([])
  })

  it('sends a signed-out visitor to the welcome page and asks for no statistics', async () => {
    const { router, fetchMock } = await renderAdminStats({
      answers: { 'GET /api/me': problemAnswer(401, 'AUTH_NOT_SIGNED_IN') },
    })

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.welcome)
    })
    expect(statsRequests(fetchMock)).toEqual([])
  })

  it('opens the screen from the Statistics row in Settings with one tap', async () => {
    const { router, fetchMock } = await renderAdminStats({ path: routes.settings })
    let taps = 0

    fireEvent.click(await screen.findByRole('link', { name: translated('en', 'adminStats.title') }))
    taps += 1

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.admin)
    })
    expect(await findStatsTitle()).toBeTruthy()
    expect(taps).toBe(1)
    await waitFor(() => {
      expect(statsRequestCount(fetchMock, defaultStatsUrl)).toBe(1)
    })
  })
})
