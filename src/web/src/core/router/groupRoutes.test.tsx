import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useApiHealth } from '@/features/auth/welcome/hooks/useApiHealth'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { greeceGroupRow, groupListOf, testGroup } from '@/test/groupTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { jsonAnswer, problemAnswer, stubFetchByRequest } from '@/test/requestTestHelpers'
import { testMe } from '@/test/testMe'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { routeObjects } from './router'
import { routes } from './routes'

vi.mock('@/features/auth/welcome/hooks/useApiHealth', () => ({
  useApiHealth: vi.fn(),
}))

const mustChangePasswordMe = { ...testMe, mustChangePassword: true }

const groupRoutePaths = [
  routes.groups,
  routes.newGroup,
  routes.recentlyDeletedGroups,
  routes.group(testGroup.id),
  routes.groupSettings(testGroup.id),
]

const barRoutePaths = [
  routes.dashboard,
  routes.groups,
  routes.recentlyDeletedGroups,
  routes.settings,
]

const noBarRoutePaths = [
  routes.newGroup,
  routes.group(testGroup.id),
  routes.groupSettings(testGroup.id),
  routes.changePassword,
]

function stubSignedInPerson() {
  return stubFetchByRequest({
    'GET /api/me': jsonAnswer(testMe),
    'GET /api/groups': jsonAnswer(groupListOf({ groups: [greeceGroupRow] })),
    [`GET /api/groups/${testGroup.id}`]: jsonAnswer(testGroup),
  })
}

describe('group routes followed through the real route table', () => {
  beforeEach(() => {
    vi.mocked(useApiHealth).mockReset()
    stubDeviceColorScheme('light')
    stubGoogleSignIn()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(groupRoutePaths)('sends a signed-out visitor who opens %s to the welcome screen', async (path) => {
    stubFetchByRequest({ 'GET /api/me': problemAnswer(401, 'AUTH_NOT_SIGNED_IN') })

    const { router } = await renderRoutesWithProviders(routeObjects, path)

    expect(
      await screen.findByRole('link', { name: translated('en', 'welcome.haveAccount') }),
    ).toBeTruthy()
    expect(router.state.location.pathname).toBe(routes.welcome)
  })

  it.each(groupRoutePaths)('sends a person who must change the password from %s to the change-password screen', async (path) => {
    stubFetchByRequest({ 'GET /api/me': jsonAnswer(mustChangePasswordMe) })

    const { router } = await renderRoutesWithProviders(routeObjects, path)

    expect(
      await screen.findByRole('heading', { name: translated('en', 'auth.changePassword.title') }),
    ).toBeTruthy()
    expect(router.state.location.pathname).toBe(routes.changePassword)
  })

  it('shows the groups list at /groups', async () => {
    stubSignedInPerson()

    await renderRoutesWithProviders(routeObjects, routes.groups)

    expect(
      await screen.findByRole('heading', { name: translated('en', 'groups.title') }),
    ).toBeTruthy()
    expect(await screen.findByText(greeceGroupRow.name)).toBeTruthy()
  })

  it('shows the new-group screen at /groups/new and does not ask for a group called new', async () => {
    const fetchMock = stubSignedInPerson()

    const { router } = await renderRoutesWithProviders(routeObjects, routes.newGroup)

    expect(
      await screen.findByRole('heading', { name: translated('en', 'newGroup.title') }),
    ).toBeTruthy()
    expect(router.state.location.pathname).toBe(routes.newGroup)
    expect(fetchMock.mock.calls.map(([url]) => String(url))).not.toContain('/api/groups/new')
  })

  it('shows the recently-deleted screen at /groups/recently-deleted and does not ask for a group called recently-deleted', async () => {
    const fetchMock = stubSignedInPerson()

    await renderRoutesWithProviders(routeObjects, routes.recentlyDeletedGroups)

    expect(
      await screen.findByRole('heading', { name: translated('en', 'recentlyDeleted.title') }),
    ).toBeTruthy()
    expect(fetchMock.mock.calls.map(([url]) => String(url))).not.toContain(
      '/api/groups/recently-deleted',
    )
  })

  it('shows the group screen at /groups/{id} and asks for that group', async () => {
    const fetchMock = stubSignedInPerson()

    await renderRoutesWithProviders(routeObjects, routes.group(testGroup.id))

    expect(await screen.findByRole('heading', { level: 1, name: testGroup.name })).toBeTruthy()
    expect(fetchMock.mock.calls.map(([url]) => String(url))).toContain(
      `/api/groups/${testGroup.id}`,
    )
  })

  it('shows the group settings screen at /groups/{id}/settings', async () => {
    stubSignedInPerson()

    await renderRoutesWithProviders(routeObjects, routes.groupSettings(testGroup.id))

    expect(
      await screen.findByRole('heading', { level: 1, name: translated('en', 'groupSettings.title') }),
    ).toBeTruthy()
  })

  it('takes a person from the groups list to the new-group screen and back to the groups list', async () => {
    stubSignedInPerson()
    const { router } = await renderRoutesWithProviders(routeObjects, routes.groups)
    await screen.findByText(greeceGroupRow.name)

    fireEvent.click(screen.getByRole('link', { name: translated('en', 'groups.newButton') }))
    await screen.findByRole('heading', { name: translated('en', 'newGroup.title') })
    fireEvent.click(screen.getByRole('link', { name: translated('en', 'common.back') }))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.groups)
    })
  })

  it.each(barRoutePaths)('shows the bottom bar with the Home, Groups and Settings tabs at %s', async (path) => {
    stubSignedInPerson()

    await renderRoutesWithProviders(routeObjects, path)

    const bar = await screen.findByRole('navigation')
    expect(
      within(bar)
        .getAllByRole('link')
        .map((link) => link.textContent),
    ).toEqual([
      translated('en', 'nav.home'),
      translated('en', 'nav.groups'),
      translated('en', 'nav.settings'),
    ])
  })

  it.each([
    [routes.dashboard, 'nav.home'],
    [routes.groups, 'nav.groups'],
    [routes.recentlyDeletedGroups, 'nav.groups'],
    [routes.settings, 'nav.settings'],
  ])('marks the current tab of the bottom bar at %s as %s', async (path, currentKey) => {
    stubSignedInPerson()

    await renderRoutesWithProviders(routeObjects, path)

    const bar = await screen.findByRole('navigation')
    const current = within(bar)
      .getAllByRole('link')
      .filter((link) => link.getAttribute('aria-current') === 'page')
    expect(current.map((link) => link.textContent)).toEqual([translated('en', currentKey)])
  })

  it.each(noBarRoutePaths)('shows no bottom bar at %s', async (path) => {
    stubSignedInPerson()

    await renderRoutesWithProviders(routeObjects, path)

    await screen.findByRole('heading', { level: 1 })
    expect(screen.queryByRole('navigation')).toBeNull()
  })

  it('takes a person from the home screen to the groups list through the bottom bar', async () => {
    stubSignedInPerson()
    const { router } = await renderRoutesWithProviders(routeObjects, routes.dashboard)
    const bar = await screen.findByRole('navigation')

    fireEvent.click(within(bar).getByRole('link', { name: translated('en', 'nav.groups') }))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.groups)
    })
    expect(
      await screen.findByRole('heading', { name: translated('en', 'groups.title') }),
    ).toBeTruthy()
    expect(screen.getByRole('navigation')).toBeTruthy()
  })

  it('takes a person from the groups list to Settings through the bottom bar and shows no Back button there', async () => {
    stubSignedInPerson()
    await renderRoutesWithProviders(routeObjects, routes.groups)
    const bar = await screen.findByRole('navigation')

    fireEvent.click(within(bar).getByRole('link', { name: translated('en', 'nav.settings') }))

    expect(
      await screen.findByRole('heading', { name: translated('en', 'settings.title') }),
    ).toBeTruthy()
    expect(screen.queryByRole('link', { name: translated('en', 'common.back') })).toBeNull()
  })
})
