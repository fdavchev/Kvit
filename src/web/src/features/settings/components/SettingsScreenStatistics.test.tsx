import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { Me } from '@/core/services/me/meService'
import { isBefore } from '@/test/expenseTestHelpers'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { seedMe, testAdminMe, testGuestMe, testMe } from '@/test/testMe'
import { resetThemeDocument } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { SettingsScreen } from './SettingsScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

async function renderSettings(me: Me, language: Language = 'en') {
  return renderRoutesWithProviders(
    [
      { path: routes.settings, element: <SettingsScreen /> },
      { path: routes.admin, element: <p>statistics page</p> },
    ],
    routes.settings,
    { language, seedCache: seedMe({ ...me, language }) },
  )
}

function statisticsLink(language: Language = 'en'): HTMLElement | null {
  return screen.queryByRole('link', { name: translated(language, 'adminStats.title') })
}

describe('SettingsScreen, the Statistics row', () => {
  beforeEach(() => {
    window.localStorage.clear()
    resetThemeDocument()
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('shows a Statistics row that links to the statistics address to an admin (%s)', async (language) => {
    await renderSettings(testAdminMe, language)

    expect(statisticsLink(language)?.getAttribute('href')).toBe(routes.admin)
  })

  it('names the row "Statistics" in English', async () => {
    await renderSettings(testAdminMe)

    expect(screen.getByRole('link', { name: 'Statistics' })).toBeTruthy()
  })

  it('opens the statistics screen with one tap on the row', async () => {
    const { router } = await renderSettings(testAdminMe)

    fireEvent.click(screen.getByRole('link', { name: translated('en', 'adminStats.title') }))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.admin)
    })
    expect(screen.getByText('statistics page')).toBeTruthy()
  })

  it.each(languages)('shows no Statistics row to a person who is not an admin (%s)', async (language) => {
    await renderSettings(testMe, language)

    expect(statisticsLink(language)).toBeNull()
  })

  it.each(languages)('shows no Statistics row to a guest (%s)', async (language) => {
    await renderSettings(testGuestMe, language)

    expect(statisticsLink(language)).toBeNull()
  })

  it('keeps the other rows of an admin: Change password, Privacy and Log out', async () => {
    await renderSettings(testAdminMe)

    expect(screen.getByRole('link', { name: translated('en', 'auth.changePassword.title') })).toBeTruthy()
    expect(screen.getByRole('link', { name: translated('en', 'common.privacy') })).toBeTruthy()
    expect(screen.getByRole('button', { name: translated('en', 'settings.logOut') })).toBeTruthy()
  })

  it('puts Log out after the Statistics row, like after every other link', async () => {
    await renderSettings(testAdminMe)
    const logOut = screen.getByRole('button', { name: translated('en', 'settings.logOut') })
    const statistics = screen.getByRole('link', { name: translated('en', 'adminStats.title') })

    expect(isBefore(statistics, logOut)).toBe(true)
  })
})
