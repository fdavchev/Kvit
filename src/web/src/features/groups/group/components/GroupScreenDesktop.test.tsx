import { screen, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { routes } from '@/core/router/routes'
import { activityTab, balancesTab, expensesTab, renderGroupApp, type GroupAppOptions } from '@/test/activityTestHelpers'
import { balancesPath, greeceTripBalances, settlementsPath } from '@/test/balancesTestData'
import { findBalanceCards, findOnlyBalanceCard } from '@/test/balancesTestHelpers'
import { freezeTime, unfreezeTime } from '@/test/expenseTestHelpers'
import { testGroup } from '@/test/groupTestData'
import { stubDesktopScreen } from '@/test/matchMediaTestHelpers'
import { ownerViewMembers } from '@/test/memberTestData'
import { requestCount } from '@/test/requestTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupId = testGroup.id
const filipView: GroupAppOptions = { members: ownerViewMembers, group: testGroup, balances: greeceTripBalances }
const bigBalanceClass = 'lg:text-[clamp(2rem,3.5vw,3rem)]'

async function findExpenseRow(): Promise<HTMLElement> {
  return screen.findByRole('link', { name: /Dinner/ })
}

describe('GroupScreen on a desktop-sized window', () => {
  beforeEach(() => {
    freezeTime()
    stubDesktopScreen()
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('shows the expenses and the balances side by side on the Expenses tab, in a grid that stacks when narrow', async () => {
    await renderGroupApp(routes.group(groupId), filipView)

    const expenseRow = await findExpenseRow()
    const card = await findOnlyBalanceCard()
    const grid = card.closest('.grid')

    expect(grid?.contains(expenseRow)).toBe(true)
    expect(grid?.className.split(/\s+/)).toEqual(
      expect.arrayContaining(['grid-cols-[repeat(auto-fit,minmax(min(100%,24rem),1fr))]']),
    )
    expect(expenseRow.compareDocumentPosition(card) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
  })

  it('asks for the balances and the payments on the Expenses tab, as the Balances tab does', async () => {
    const { fetchMock } = await renderGroupApp(routes.group(groupId), filipView)
    await findOnlyBalanceCard()

    expect(requestCount(fetchMock, 'GET', balancesPath)).toBe(1)
    expect(requestCount(fetchMock, 'GET', settlementsPath)).toBe(1)
  })

  it('keeps the same three tabs, with Expenses as the current one', async () => {
    await renderGroupApp(routes.group(groupId), filipView)
    await findExpenseRow()

    expect(expensesTab().getAttribute('aria-current')).toBe('page')
    expect(balancesTab().getAttribute('aria-current')).toBeNull()
    expect(activityTab().getAttribute('aria-current')).toBeNull()
  })

  it('shows only the balances on the Balances tab', async () => {
    await renderGroupApp(routes.groupBalances(groupId), filipView)

    expect(await findBalanceCards()).toHaveLength(1)
    expect(screen.queryByRole('link', { name: /Dinner/ })).toBeNull()
  })

  it('shows no balances on the Activity tab', async () => {
    const { fetchMock } = await renderGroupApp(routes.groupActivity(groupId), filipView)
    await screen.findByText(translated('en', 'activity.empty'))

    expect(screen.queryByRole('region', { name: translated('en', 'balances.yourBalance') })).toBeNull()
    expect(requestCount(fetchMock, 'GET', balancesPath)).toBe(0)
  })

  it('draws the balance of the person big on desktop', async () => {
    await renderGroupApp(routes.groupBalances(groupId), filipView)

    const card = await findOnlyBalanceCard()

    expect(card.querySelector(`[class~="${bigBalanceClass}"]`)).not.toBeNull()
  })

  it('uses the wide page width and keeps the tabs at a readable width', async () => {
    await renderGroupApp(routes.group(groupId), filipView)
    await findExpenseRow()

    expect(screen.getByRole('main').className).toContain('lg:max-w-[64rem]')
    expect(expensesTab().parentElement?.className).toContain('lg:max-w-[36rem]')
  })

  it('shows the left menu with Groups as the current page', async () => {
    await renderGroupApp(routes.group(groupId), filipView)
    await findExpenseRow()

    const rail = screen.getByRole('navigation')

    expect(within(rail).getByRole('link', { name: translated('en', 'nav.groups') }).getAttribute('aria-current')).toBe('page')
  })
})
