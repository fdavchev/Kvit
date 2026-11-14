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
      { path: routes.adminTools, element: <p>owner tools page</p> },
    ],
    routes.settings,
    { language, seedCache: seedMe({ ...me, language }) },
  )
}

function ownerToolsLink(language: Language = 'en'): HTMLElement | null {
  return screen.queryByRole('link', { name: translated(language, 'settings.ownerTools') })
}

function linkNames(): string[] {
  return screen.getAllByRole('link').map((link) => (link.textContent ?? '').trim())
}

describe('SettingsScreen, the Owner tools row', () => {
  beforeEach(() => {
    window.localStorage.clear()
    resetThemeDocument()
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('has the address /admin/tools in the table of routes', () => {
    expect(routes.adminTools).toBe('/admin/tools')
  })

  it.each(languages)('shows an Owner tools row that links to the owner tools address to an admin (%s)', async (language) => {
    await renderSettings(testAdminMe, language)

    expect(ownerToolsLink(language)?.getAttribute('href')).toBe(routes.adminTools)
  })

  it('names the row "Owner tools" in English', async () => {
    await renderSettings(testAdminMe)

    expect(screen.getByRole('link', { name: 'Owner tools' })).toBeTruthy()
  })

  it('opens the owner tools with one tap on the row', async () => {
    const { router } = await renderSettings(testAdminMe)

    fireEvent.click(screen.getByRole('link', { name: translated('en', 'settings.ownerTools') }))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.adminTools)
    })
    expect(screen.getByText('owner tools page')).toBeTruthy()
  })

  it('puts the row directly under the Statistics row, with no other row between them', async () => {
    await renderSettings(testAdminMe)

    const names = linkNames()
    const statistics = names.indexOf(translated('en', 'adminStats.title'))

    expect(statistics).toBeGreaterThanOrEqual(0)
    expect(names[statistics + 1]).toBe(translated('en', 'settings.ownerTools'))
  })

  it.each(languages)('keeps the Owner tools row right after the Statistics row in %s too', async (language) => {
    await renderSettings(testAdminMe, language)

    const names = linkNames()
    const statistics = names.indexOf(translated(language, 'adminStats.title'))

    expect(names[statistics + 1]).toBe(translated(language, 'settings.ownerTools'))
  })

  it('puts Log out after the Owner tools row', async () => {
    await renderSettings(testAdminMe)
    const ownerTools = screen.getByRole('link', { name: translated('en', 'settings.ownerTools') })
    const logOut = screen.getByRole('button', { name: translated('en', 'settings.logOut') })

    expect(isBefore(ownerTools, logOut)).toBe(true)
  })

  it('keeps the Statistics row of an admin', async () => {
    await renderSettings(testAdminMe)

    expect(screen.getByRole('link', { name: translated('en', 'adminStats.title') }).getAttribute('href')).toBe(routes.admin)
  })

  it.each(languages)('shows no Owner tools row to a person who is not an admin (%s)', async (language) => {
    await renderSettings(testMe, language)

    expect(ownerToolsLink(language)).toBeNull()
  })

  it.each(languages)('shows no Owner tools row to a guest (%s)', async (language) => {
    await renderSettings(testGuestMe, language)

    expect(ownerToolsLink(language)).toBeNull()
  })
})
