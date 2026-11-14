import { screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { findToolsTitle, renderOwnerTools, toolsRequests } from '@/test/ownerToolsTestHelpers'
import { neverAnswer, problemAnswer } from '@/test/requestTestHelpers'
import { seedMe, testAdminMe, testGuestMe, testMe } from '@/test/testMe'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { routes } from './routes'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

function notFoundHeading(): HTMLElement {
  return screen.getByRole('heading', { name: translated('en', 'notFound.title') })
}

describe('the owner tools route followed through the real route table', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.clearAllMocks()
    stubDeviceColorScheme('light')
    stubGoogleSignIn()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('answers to /admin/tools', () => {
    expect(routes.adminTools).toBe('/admin/tools')
  })

  it('shows the Owner tools screen to an admin, with the bottom bar', async () => {
    const { router } = await renderOwnerTools()

    expect(await findToolsTitle()).toBeTruthy()
    expect(screen.getByRole('navigation')).toBeTruthy()
    expect(router.state.location.pathname).toBe(routes.adminTools)
  })

  it.each(languages)('shows the title of the screen in the language of the admin (%s)', async (language) => {
    await renderOwnerTools({ language })

    expect(await findToolsTitle(language)).toBeTruthy()
  })

  it('shows the Owner tools screen and not the Statistics screen at that address', async () => {
    await renderOwnerTools()
    await findToolsTitle()

    expect(screen.queryByRole('heading', { name: translated('en', 'adminStats.title') })).toBeNull()
  })

  it('shows the Owner tools screen at once when me is already known to be an admin', async () => {
    await renderOwnerTools({ seedCache: seedMe(testAdminMe) })

    expect(screen.getByRole('heading', { level: 1, name: translated('en', 'ownerTools.title') })).toBeTruthy()
  })

  it('shows the normal not-found screen at the same address to a signed-in person who is not an admin', async () => {
    const { router, fetchMock } = await renderOwnerTools({ me: testMe })

    expect(await screen.findByRole('heading', { name: translated('en', 'notFound.title') })).toBeTruthy()
    expect(screen.getByRole('button', { name: translated('en', 'notFound.goHome') })).toBeTruthy()
    expect(router.state.location.pathname).toBe(routes.adminTools)
    expect(router.state.historyAction).toBe('POP')
    expect(screen.queryByRole('heading', { name: translated('en', 'ownerTools.title') })).toBeNull()
    expect(toolsRequests(fetchMock)).toEqual([])
  })

  it('shows not-found at once, with no Owner tools screen first, when me is already known to be a non-admin', async () => {
    const { fetchMock } = await renderOwnerTools({ me: testMe, seedCache: seedMe(testMe) })

    expect(notFoundHeading()).toBeTruthy()
    expect(screen.queryByRole('heading', { name: translated('en', 'ownerTools.title') })).toBeNull()
    expect(toolsRequests(fetchMock)).toEqual([])
  })

  it('shows the not-found screen to a guest at the same address, not a redirect to Settings', async () => {
    const { router, fetchMock } = await renderOwnerTools({ me: testGuestMe })

    expect(await screen.findByRole('heading', { name: translated('en', 'notFound.title') })).toBeTruthy()
    expect(router.state.location.pathname).toBe(routes.adminTools)
    expect(router.state.historyAction).toBe('POP')
    expect(toolsRequests(fetchMock)).toEqual([])
  })

  it.each(languages)('writes the not-found screen to a non-admin in the language of the person (%s)', async (language) => {
    await renderOwnerTools({ me: testMe, language })

    expect(await screen.findByRole('heading', { name: translated(language, 'notFound.title') })).toBeTruthy()
  })

  it('requests nothing for the screen while me is still being asked', async () => {
    const { fetchMock } = await renderOwnerTools({ appAnswers: { 'GET /api/me': neverAnswer() } })

    expect(screen.getByRole('status')).toBeTruthy()
    expect(screen.queryByRole('heading', { name: translated('en', 'ownerTools.title') })).toBeNull()
    expect(screen.queryByRole('heading', { name: translated('en', 'notFound.title') })).toBeNull()
    expect(toolsRequests(fetchMock)).toEqual([])
  })

  it('sends a signed-out visitor to the welcome page and asks for nothing', async () => {
    const { router, fetchMock } = await renderOwnerTools({
      appAnswers: { 'GET /api/me': problemAnswer(401, 'AUTH_NOT_SIGNED_IN') },
    })

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.welcome)
    })
    expect(toolsRequests(fetchMock)).toEqual([])
  })
})
