import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { renderGroupApp } from '@/test/activityTestHelpers'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { jsonAnswer } from '@/test/requestTestHelpers'
import { testGuestMe } from '@/test/testMe'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { routes } from './routes'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

function renderAsGuest(path: string) {
  return renderGroupApp(path, { answers: { 'GET /api/me': jsonAnswer(testGuestMe) } })
}

describe('the routes a guest cannot use, followed through the real route table', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    stubDeviceColorScheme('light')
    stubGoogleSignIn()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each([
    ['New group', routes.newGroup],
    ['the group form', routes.newGroupGroup],
    ['One bill', routes.newBill],
    ['Set a password', routes.setPassword],
    ['Change password', routes.changePassword],
  ])('sends a guest who opens %s to Settings, where the guest card explains to make an account', async (_label, path) => {
    const { router } = await renderAsGuest(path)

    expect(await screen.findByRole('heading', { name: translated('en', 'settings.guest.title') })).toBeTruthy()
    expect(router.state.location.pathname).toBe(routes.settings)
    expect(router.state.historyAction).toBe('REPLACE')
  })

  it('sends a guest to Settings without asking the server for the categories of a One bill', async () => {
    const { fetchMock } = await renderAsGuest(routes.newBill)
    await screen.findByRole('heading', { name: translated('en', 'settings.guest.title') })

    expect(fetchMock.mock.calls.map(([url]) => String(url))).not.toContain('/api/categories')
  })

  it('still shows the New group choice to a person with an account', async () => {
    const { router } = await renderGroupApp(routes.newGroup)

    expect(await screen.findByRole('heading', { level: 1, name: translated('en', 'newGroup.title') })).toBeTruthy()
    expect(router.state.location.pathname).toBe(routes.newGroup)
  })

  it('opens the make-account screen from the guest card and comes back to Settings with its back button', async () => {
    const { router } = await renderAsGuest(routes.settings)

    fireEvent.click(await screen.findByRole('link', { name: translated('en', 'settings.guest.makeAccount') }))

    expect(
      await screen.findByRole('heading', { level: 1, name: translated('en', 'settings.guest.makeAccount') }),
    ).toBeTruthy()
    expect(router.state.location.pathname).toBe(routes.makeAccount)

    fireEvent.click(screen.getByRole('link', { name: translated('en', 'common.back') }))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.settings)
    })
  })
})
