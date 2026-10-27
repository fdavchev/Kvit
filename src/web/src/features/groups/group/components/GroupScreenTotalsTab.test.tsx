import { act, fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import {
  activityTab,
  balancesTab,
  expensesTab,
  renderGroupApp,
  totalsTab,
  type GroupAppOptions,
} from '@/test/activityTestHelpers'
import { activityPath } from '@/test/activityTestData'
import { balancesPath, greeceTripBalances, settlementsPath } from '@/test/balancesTestData'
import { findBalanceCards } from '@/test/balancesTestHelpers'
import { closingGroup, finishedGroup } from '@/test/closingTestData'
import { anaView, ownerView, showsGroupScreen as showsClosingGroupScreen } from '@/test/closingTestHelpers'
import { testCategories } from '@/test/expenseTestData'
import { categoriesPath, expensesPath, freezeTime, isBefore, unfreezeTime } from '@/test/expenseTestHelpers'
import { groupOf, testGroup } from '@/test/groupTestData'
import { mkdOnlySpending, onlyUncategorisedSpending, spendingPath } from '@/test/groupSpendingTestData'
import { currencyRegion, findCurrencyRegion, queryCurrencyRegion } from '@/test/groupSpendingTestHelpers'
import { stubDesktopScreen } from '@/test/matchMediaTestHelpers'
import { ownerViewMembers } from '@/test/memberTestData'
import { jsonAnswer, problemAnswer, requestCount } from '@/test/requestTestHelpers'
import { testGuestMe } from '@/test/testMe'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupId = testGroup.id
const filipView: GroupAppOptions = { members: ownerViewMembers, group: testGroup, spending: mkdOnlySpending }

async function showsGroupScreen(): Promise<void> {
  await screen.findByRole('heading', { level: 1, name: testGroup.name })
}

function tabLabels(language: Language = 'en'): (string | null)[] {
  const bar = expensesTab(language).parentElement
  if (bar === null) {
    throw new Error('The Expenses tab is not inside the tab bar')
  }
  return within(bar)
    .getAllByRole('link')
    .map((link) => link.textContent)
}

describe('GroupScreen with the Totals tab', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    freezeTime()
    stubDeviceColorScheme('light')
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('the pill switch', () => {
    it.each(languages)('shows four tabs in the order Expenses, Balances, Activity, Totals (%s)', async (language) => {
      await renderGroupApp(routes.group(groupId), { language })
      await showsGroupScreen()

      expect(tabLabels(language)).toEqual([
        translated(language, 'expenses.title'),
        translated(language, 'balances.title'),
        translated(language, 'activity.title'),
        translated(language, 'groupTotals.tab'),
      ])
    })

    it('writes the name of the fourth tab in English as Totals', async () => {
      await renderGroupApp(routes.group(groupId))
      await showsGroupScreen()

      expect(totalsTab().textContent).toBe('Totals')
    })

    it('writes the name of the fourth tab in Macedonian as Вкупно', async () => {
      await renderGroupApp(routes.group(groupId), { language: 'mk' })
      await showsGroupScreen()

      expect(totalsTab('mk').textContent).toBe('Вкупно')
    })

    it('links Totals to its address under the group', async () => {
      await renderGroupApp(routes.group(groupId))
      await showsGroupScreen()

      expect(totalsTab().getAttribute('href')).toBe(routes.groupTotals(groupId))
      expect(routes.groupTotals(groupId)).toBe(`/groups/${groupId}/totals`)
    })

    it('marks Totals as the current page on the Totals tab and none of the other three', async () => {
      await renderGroupApp(routes.groupTotals(groupId), filipView)
      await showsGroupScreen()

      expect(totalsTab().getAttribute('aria-current')).toBe('page')
      expect(expensesTab().getAttribute('aria-current')).not.toBe('page')
      expect(balancesTab().getAttribute('aria-current')).not.toBe('page')
      expect(activityTab().getAttribute('aria-current')).not.toBe('page')
    })

    it.each([
      ['the Expenses tab', routes.group(groupId)],
      ['the Balances tab', routes.groupBalances(groupId)],
      ['the Activity tab', routes.groupActivity(groupId)],
    ])('does not mark Totals as the current page on %s', async (_name, path) => {
      await renderGroupApp(path)
      await showsGroupScreen()

      expect(totalsTab().getAttribute('aria-current')).not.toBe('page')
    })

    it('opens the Totals tab when its link is pressed, and the Expenses tab again when that link is pressed', async () => {
      const { router } = await renderGroupApp(routes.group(groupId), filipView)
      await showsGroupScreen()

      fireEvent.click(totalsTab())
      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groupTotals(groupId))
      })
      expect(await findCurrencyRegion('MKD')).toBeTruthy()
      expect(totalsTab().getAttribute('aria-current')).toBe('page')

      fireEvent.click(expensesTab())
      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(groupId))
      })
      expect(await screen.findByRole('link', { name: /Dinner/ })).toBeTruthy()
      expect(queryCurrencyRegion('MKD')).toBeNull()
    })

    it('opens the Totals tab from the Balances tab and the Activity tab', async () => {
      const { router } = await renderGroupApp(routes.groupBalances(groupId), filipView)
      await showsGroupScreen()

      fireEvent.click(totalsTab())
      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groupTotals(groupId))
      })
      await findCurrencyRegion('MKD')

      fireEvent.click(activityTab())
      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groupActivity(groupId))
      })
      await screen.findByText(translated('en', 'activity.empty'))

      fireEvent.click(totalsTab())
      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groupTotals(groupId))
      })
      expect(await findCurrencyRegion('MKD')).toBeTruthy()
    })

    it('selects the Totals panel by the address alone, with the group header, the Members chip and the Settings chip in place', async () => {
      await renderGroupApp(routes.groupTotals(groupId), { ...filipView, group: groupOf({ memberCount: 3 }) })

      expect(await findCurrencyRegion('MKD')).toBeTruthy()
      expect(screen.getByRole('heading', { level: 1, name: testGroup.name })).toBeTruthy()
      expect(screen.getByText('3 people · MKD')).toBeTruthy()
      expect(screen.getByRole('link', { name: translated('en', 'members.title') }).getAttribute('href')).toBe(
        routes.groupMembers(groupId),
      )
      expect(screen.getByRole('link', { name: translated('en', 'group.settings') }).getAttribute('href')).toBe(
        routes.groupSettings(groupId),
      )
    })

    it('shows no + button, no expense rows and no activity on the Totals tab', async () => {
      await renderGroupApp(routes.groupTotals(groupId), filipView)
      await findCurrencyRegion('MKD')

      expect(screen.queryByRole('link', { name: translated('en', 'expense.addTitle') })).toBeNull()
      expect(screen.queryByRole('link', { name: /Dinner/ })).toBeNull()
      expect(screen.queryByText(translated('en', 'activity.empty'))).toBeNull()
    })

    it('goes back and forward with the browser buttons between the Expenses tab and the Totals tab', async () => {
      const { router } = await renderGroupApp(routes.group(groupId), filipView)
      await showsGroupScreen()
      fireEvent.click(totalsTab())
      await findCurrencyRegion('MKD')

      await act(() => router.navigate(-1))

      expect(router.state.location.pathname).toBe(routes.group(groupId))
      expect(await screen.findByRole('link', { name: /Dinner/ })).toBeTruthy()
      expect(queryCurrencyRegion('MKD')).toBeNull()
      expect(totalsTab().getAttribute('aria-current')).not.toBe('page')

      await act(() => router.navigate(1))

      expect(router.state.location.pathname).toBe(routes.groupTotals(groupId))
      expect(await findCurrencyRegion('MKD')).toBeTruthy()
      expect(totalsTab().getAttribute('aria-current')).toBe('page')
    })
  })

  describe('using the tab with the keyboard and a screen reader', () => {
    it('is a native link that the Tab key reaches, after the Activity tab', async () => {
      await renderGroupApp(routes.group(groupId))
      await showsGroupScreen()

      const tab = totalsTab()

      expect(tab.tagName).toBe('A')
      expect(tab.getAttribute('tabindex')).not.toBe('-1')
      expect(tab.hasAttribute('href')).toBe(true)
      expect(isBefore(activityTab(), tab)).toBe(true)
    })

    it('has a tap area at least 44 px high, like the other three tabs', async () => {
      await renderGroupApp(routes.group(groupId))
      await showsGroupScreen()

      expect(Array.from(totalsTab().classList)).toContain('min-h-11')
    })

    it('has no more than one current tab at a time', async () => {
      await renderGroupApp(routes.groupTotals(groupId), filipView)
      await showsGroupScreen()

      const bar = expensesTab().parentElement
      const current = within(bar ?? document.body)
        .getAllByRole('link')
        .filter((link) => link.getAttribute('aria-current') === 'page')

      expect(current.map((link) => link.textContent)).toEqual([translated('en', 'groupTotals.tab')])
    })
  })

  describe('what each tab asks for', () => {
    it('asks for the spending exactly once when the Totals tab is opened', async () => {
      const { fetchMock } = await renderGroupApp(routes.groupTotals(groupId), filipView)
      await findCurrencyRegion('MKD')

      expect(requestCount(fetchMock, 'GET', spendingPath)).toBe(1)
    })

    it('does not ask for the expenses, the balances, the payments or the activity while the Totals tab is open', async () => {
      const { fetchMock } = await renderGroupApp(routes.groupTotals(groupId), filipView)
      await findCurrencyRegion('MKD')

      expect(requestCount(fetchMock, 'GET', expensesPath)).toBe(0)
      expect(requestCount(fetchMock, 'GET', balancesPath)).toBe(0)
      expect(requestCount(fetchMock, 'GET', settlementsPath)).toBe(0)
      expect(requestCount(fetchMock, 'GET', activityPath)).toBe(0)
    })

    it('asks for the built-in categories once when a row has a category', async () => {
      const { fetchMock } = await renderGroupApp(routes.groupTotals(groupId), filipView)
      await findCurrencyRegion('MKD')

      expect(requestCount(fetchMock, 'GET', categoriesPath)).toBe(1)
    })

    it('does not ask for the built-in categories when no row has a category', async () => {
      const { fetchMock } = await renderGroupApp(routes.groupTotals(groupId), {
        ...filipView,
        spending: onlyUncategorisedSpending,
      })
      await findCurrencyRegion('MKD')

      expect(requestCount(fetchMock, 'GET', categoriesPath)).toBe(0)
    })

    it('does not ask for the spending while the Expenses tab is open', async () => {
      const { fetchMock } = await renderGroupApp(routes.group(groupId), filipView)
      await screen.findByRole('link', { name: /Dinner/ })

      expect(requestCount(fetchMock, 'GET', spendingPath)).toBe(0)
    })

    it('does not ask for the spending while the Balances tab is open', async () => {
      const { fetchMock } = await renderGroupApp(routes.groupBalances(groupId), {
        ...filipView,
        balances: greeceTripBalances,
      })
      await findBalanceCards()

      expect(requestCount(fetchMock, 'GET', spendingPath)).toBe(0)
    })

    it('does not ask for the spending while the Activity tab is open', async () => {
      const { fetchMock } = await renderGroupApp(routes.groupActivity(groupId), filipView)
      await screen.findByText(translated('en', 'activity.empty'))

      expect(requestCount(fetchMock, 'GET', spendingPath)).toBe(0)
    })

    it('keeps the spending in the cache under the key groups, the group id, spending', async () => {
      const { queryClient } = await renderGroupApp(routes.groupTotals(groupId), filipView)
      await findCurrencyRegion('MKD')

      expect(queryClient.getQueryData(['groups', groupId, 'spending'])).toEqual(mkdOnlySpending)
    })

    it('shows a saved answer at once, with no placeholders, when the cache already holds the spending', async () => {
      await renderGroupApp(routes.groupTotals(groupId), {
        ...filipView,
        spending: onlyUncategorisedSpending,
        seedCache: (queryClient) => {
          queryClient.setQueryData(['groups', groupId, 'spending'], mkdOnlySpending)
          queryClient.setQueryData(['categories'], testCategories)
        },
      })

      expect(await findCurrencyRegion('MKD')).toBeTruthy()
      expect(screen.queryByRole('status')).toBeNull()
    })

    it('shows the cached answer at once when the Totals tab is opened again, without placeholders', async () => {
      const { router } = await renderGroupApp(routes.groupTotals(groupId), filipView)
      await findCurrencyRegion('MKD')
      fireEvent.click(expensesTab())
      await screen.findByRole('link', { name: /Dinner/ })

      fireEvent.click(totalsTab())
      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groupTotals(groupId))
      })

      expect(currencyRegion('MKD')).toBeTruthy()
      expect(screen.queryByRole('status')).toBeNull()
    })

    it('does not ask for the spending on the desktop Expenses tab, which shows the balances beside the expenses', async () => {
      stubDesktopScreen()
      const { fetchMock } = await renderGroupApp(routes.group(groupId), {
        ...filipView,
        balances: greeceTripBalances,
      })
      await findBalanceCards()

      expect(requestCount(fetchMock, 'GET', spendingPath)).toBe(0)
    })
  })

  describe('on a desktop-sized window', () => {
    beforeEach(() => {
      stubDesktopScreen()
    })

    it('shows the four tabs and only the totals on the Totals tab, with no balances beside them', async () => {
      const { fetchMock } = await renderGroupApp(routes.groupTotals(groupId), {
        ...filipView,
        balances: greeceTripBalances,
      })
      await findCurrencyRegion('MKD')

      expect(tabLabels()).toHaveLength(4)
      expect(totalsTab().getAttribute('aria-current')).toBe('page')
      expect(screen.queryByRole('link', { name: /Dinner/ })).toBeNull()
      expect(requestCount(fetchMock, 'GET', balancesPath)).toBe(0)
    })

    it('keeps the balances beside the expenses on the Expenses tab', async () => {
      await renderGroupApp(routes.group(groupId), { ...filipView, balances: greeceTripBalances })

      expect(await findBalanceCards()).toHaveLength(1)
      expect(await screen.findByRole('link', { name: /Dinner/ })).toBeTruthy()
      expect(queryCurrencyRegion('MKD')).toBeNull()
    })
  })

  describe('who sees the tab', () => {
    it('shows the tab and the totals to a member who is not the owner', async () => {
      await renderGroupApp(routes.groupTotals(groupId), { ...anaView(testGroup), spending: mkdOnlySpending })

      expect(await findCurrencyRegion('MKD')).toBeTruthy()
      expect(totalsTab().getAttribute('href')).toBe(routes.groupTotals(groupId))
    })

    it('shows the tab and the totals to a guest', async () => {
      await renderGroupApp(routes.groupTotals(groupId), {
        ...filipView,
        group: groupOf({ isOwner: false }),
        answers: { 'GET /api/me': jsonAnswer(testGuestMe) },
      })

      expect(await findCurrencyRegion('MKD')).toBeTruthy()
      expect(totalsTab().getAttribute('href')).toBe(routes.groupTotals(groupId))
    })

    it.each([
      ['a Closing group', closingGroup],
      ['a Finished group', finishedGroup],
    ])('shows the tab and the totals in %s', async (_name, group) => {
      await renderGroupApp(routes.groupTotals(groupId), { ...ownerView(group), spending: mkdOnlySpending })
      await showsClosingGroupScreen()

      expect(await findCurrencyRegion('MKD')).toBeTruthy()
      expect(totalsTab().getAttribute('aria-current')).toBe('page')
    })

    it.each([
      ['a member who is not the owner', anaView(testGroup)],
      ['the owner in a Finished group', ownerView(finishedGroup)],
    ])('shows the Totals tab on the Expenses tab to %s', async (_name, options) => {
      await renderGroupApp(routes.group(groupId), options)
      await screen.findByRole('link', { name: translated('en', 'groupTotals.tab') })

      expect(totalsTab().getAttribute('href')).toBe(routes.groupTotals(groupId))
    })
  })

  describe('a group that is not found', () => {
    it('shows the group-not-found message with a Go to Groups link and no retry button when the spending answers 404 GROUP_NOT_FOUND', async () => {
      await renderGroupApp(routes.groupTotals(groupId), {
        ...filipView,
        answers: { [`GET ${spendingPath}`]: problemAnswer(404, 'GROUP_NOT_FOUND') },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.GROUP_NOT_FOUND'))
      expect(screen.getByRole('link', { name: translated('en', 'groups.goToGroups') })).toBeTruthy()
      expect(screen.queryByRole('button', { name: translated('en', 'common.retry') })).toBeNull()
    })
  })
})
