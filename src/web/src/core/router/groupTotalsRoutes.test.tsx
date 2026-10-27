import { screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useApiHealth } from '@/features/auth/welcome/hooks/useApiHealth'
import { activityPath } from '@/test/activityTestData'
import { balancesPath, noExpensesYetBalances, settlementsPath } from '@/test/balancesTestData'
import { testCategories } from '@/test/expenseTestData'
import { categoriesPath, expensesPath } from '@/test/expenseTestHelpers'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { mkdOnlySpending, spendingPath } from '@/test/groupSpendingTestData'
import { greeceGroupRow, groupListOf, testGroup } from '@/test/groupTestData'
import { ownerViewMembers } from '@/test/memberTestData'
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

function stubSignedInPerson() {
  return stubFetchByRequest({
    'GET /api/me': jsonAnswer(testMe),
    'GET /api/groups': jsonAnswer(groupListOf({ groups: [greeceGroupRow] })),
    [`GET /api/groups/${testGroup.id}`]: jsonAnswer(testGroup),
    [`GET /api/groups/${testGroup.id}/members`]: jsonAnswer(ownerViewMembers),
    [`GET ${expensesPath}`]: jsonAnswer({ expenses: [] }),
    [`GET ${categoriesPath}`]: jsonAnswer({ categories: testCategories }),
    [`GET ${activityPath}`]: jsonAnswer({ events: [] }),
    [`GET ${balancesPath}`]: jsonAnswer(noExpensesYetBalances),
    [`GET ${settlementsPath}`]: jsonAnswer({ settlements: [] }),
    [`GET ${spendingPath}`]: jsonAnswer(mkdOnlySpending),
  })
}

describe('the Totals route followed through the real route table', () => {
  beforeEach(() => {
    vi.mocked(useApiHealth).mockReset()
    stubDeviceColorScheme('light')
    stubGoogleSignIn()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends a signed-out visitor who opens the Totals address to the welcome screen', async () => {
    stubFetchByRequest({ 'GET /api/me': problemAnswer(401, 'AUTH_NOT_SIGNED_IN') })

    const { router } = await renderRoutesWithProviders(routeObjects, routes.groupTotals(testGroup.id))

    expect(await screen.findByRole('link', { name: translated('en', 'welcome.haveAccount') })).toBeTruthy()
    expect(router.state.location.pathname).toBe(routes.welcome)
  })

  it('sends a person who must change the password from the Totals address to the change-password screen', async () => {
    stubFetchByRequest({ 'GET /api/me': jsonAnswer({ ...testMe, mustChangePassword: true }) })

    const { router } = await renderRoutesWithProviders(routeObjects, routes.groupTotals(testGroup.id))

    expect(await screen.findByRole('heading', { name: translated('en', 'auth.changePassword.title') })).toBeTruthy()
    expect(router.state.location.pathname).toBe(routes.changePassword)
  })

  it('shows the group screen at /groups/{id}/totals and asks for that group and its spending', async () => {
    const fetchMock = stubSignedInPerson()

    const { router } = await renderRoutesWithProviders(routeObjects, routes.groupTotals(testGroup.id))

    expect(await screen.findByRole('heading', { level: 1, name: testGroup.name })).toBeTruthy()
    expect(await screen.findByRole('region', { name: 'MKD' })).toBeTruthy()
    const urls = fetchMock.mock.calls.map(([url]) => String(url))
    expect(urls).toContain(`/api/groups/${testGroup.id}`)
    expect(urls).toContain(spendingPath)
    expect(router.state.location.pathname).toBe(routes.groupTotals(testGroup.id))
  })

  it('shows no bottom bar at the Totals address, like the other tabs of a group', async () => {
    stubSignedInPerson()

    await renderRoutesWithProviders(routeObjects, routes.groupTotals(testGroup.id))

    await screen.findByRole('heading', { level: 1, name: testGroup.name })
    expect(screen.queryByRole('navigation')).toBeNull()
  })
})
