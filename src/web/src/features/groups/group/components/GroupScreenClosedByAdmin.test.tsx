import { screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { Group } from '@/core/services/groups/groupsService'
import { renderGroupApp, type GroupAppOptions } from '@/test/activityTestHelpers'
import {
  greeceTripInTwoCurrencies,
  markoPaidAnaPending,
  markoPaidFilipPending,
} from '@/test/balancesTestData'
import { findBalanceCards } from '@/test/balancesTestHelpers'
import {
  closedByAdminBarText,
  closedByAdminClosingGroup,
  closedByAdminFinishedGroup,
  closedByAdminGroup,
  closedByAdminSingleMemberGroup,
  closingTexts,
  finishedBarText,
  textOf,
} from '@/test/closingTestData'
import { anaView, buttonLabelsIn, findBarTitled, ownerView } from '@/test/closingTestHelpers'
import { freezeTime, unfreezeTime } from '@/test/expenseTestHelpers'
import { testGroup } from '@/test/groupTestData'
import { allButtonsNamed } from '@/test/settlementActionsTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupId = testGroup.id
const addExpenseLinkName = 'Add expense'
const openGroup: Group = { ...testGroup, memberCount: 5 }

interface Viewer {
  name: string
  view: (group: Group) => GroupAppOptions
}

const owner: Viewer = { name: 'the owner', view: (group) => ownerView(group) }
const member: Viewer = { name: 'a member', view: (group) => anaView(group) }

function paymentControlCount(): number {
  return ['Mark paid', 'Confirm', 'Reject', 'Cancel', 'Delete', 'Record another payment'].reduce(
    (count, name) => count + allButtonsNamed(name).length,
    0,
  )
}

describe('the group screen when the owner of Kvit closed the group', () => {
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

  describe.each([owner, member])('the bar, for $name', (viewer) => {
    it('says the group was closed by the owner of Kvit and is read-only, in a bar with no button at all', async () => {
      await renderGroupApp(routes.group(groupId), viewer.view(closedByAdminGroup))

      const bar = await findBarTitled(closedByAdminBarText)

      expect(buttonLabelsIn(bar)).toEqual([])
    })

    it('has no Unlock for changes, also when the group was finished before it was closed', async () => {
      await renderGroupApp(routes.group(groupId), viewer.view(closedByAdminFinishedGroup))

      const bar = await findBarTitled(closedByAdminBarText)

      expect(buttonLabelsIn(bar)).toEqual([])
      expect(screen.queryByRole('button', { name: translated('en', 'closing.unlock') })).toBeNull()
      expect(screen.queryByText(finishedBarText.en)).toBeNull()
    })

    it('shows this bar instead of the waiting or prompt bar when the group was being closed', async () => {
      await renderGroupApp(routes.group(groupId), viewer.view(closedByAdminClosingGroup))

      const bar = await findBarTitled(closedByAdminBarText)

      expect(buttonLabelsIn(bar)).toEqual([])
      expect(screen.queryByText(closingTexts.waitingTitle.en)).toBeNull()
      expect(screen.queryByText(textOf('en', closingTexts.prompt, { name: 'Filip', group: testGroup.name }))).toBeNull()
    })

    it.each(languages)('writes the bar in the language of the person (%s)', async (language) => {
      await renderGroupApp(routes.group(groupId), {
        ...viewer.view(closedByAdminGroup),
        language,
      })

      expect(await findBarTitled(closedByAdminBarText, language)).toBeTruthy()
    })
  })

  describe.each([owner, member])('the Expenses tab, for $name', (viewer) => {
    it('has the Add expense button while the group is open, so the next test proves something', async () => {
      await renderGroupApp(routes.group(groupId), viewer.view(openGroup))
      await screen.findByRole('link', { name: /Dinner/ })

      expect(screen.getByRole('link', { name: addExpenseLinkName })).toBeTruthy()
    })

    it('has no Add expense button when the owner of Kvit closed the group', async () => {
      await renderGroupApp(routes.group(groupId), viewer.view(closedByAdminGroup))
      await findBarTitled(closedByAdminBarText)

      expect(screen.queryByRole('link', { name: addExpenseLinkName })).toBeNull()
    })

    it('keeps the rows of the expenses as links to their details', async () => {
      await renderGroupApp(routes.group(groupId), viewer.view(closedByAdminGroup))

      expect(await screen.findByRole('link', { name: /Dinner/ })).toBeTruthy()
    })

    it('keeps the links to Members and to Group settings, because Leave group lives there', async () => {
      await renderGroupApp(routes.group(groupId), viewer.view(closedByAdminGroup))
      await findBarTitled(closedByAdminBarText)

      expect(screen.getByRole('link', { name: 'Members' })).toBeTruthy()
      expect(screen.getByRole('link', { name: 'Group settings' })).toBeTruthy()
    })
  })

  describe('the Add a name button of a group with one person and no expenses', () => {
    it('is shown while the group is open', async () => {
      await renderGroupApp(routes.group(groupId), { ...ownerView({ ...testGroup, memberCount: 1 }), expenses: [] })

      expect(await screen.findByRole('button', { name: 'Add a name' })).toBeTruthy()
    })

    it('is not shown when the owner of Kvit closed the group', async () => {
      await renderGroupApp(routes.group(groupId), { ...ownerView(closedByAdminSingleMemberGroup), expenses: [] })
      await findBarTitled(closedByAdminBarText)
      await screen.findByText('No expenses yet')

      expect(screen.queryByRole('button', { name: 'Add a name' })).toBeNull()
    })
  })

  describe.each([
    ['the owner', ownerView, [markoPaidFilipPending]],
    ['a member', anaView, [markoPaidAnaPending]],
  ] as const)('the payment buttons on the Balances tab, for %s', (_name, view, settlements) => {
    it('has payment buttons while the group is open, so the next test proves something', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        ...view(openGroup),
        balances: greeceTripInTwoCurrencies,
        settlements: [...settlements],
      })
      await findBalanceCards()

      expect(paymentControlCount()).toBeGreaterThan(0)
    })

    it('has none of them when the owner of Kvit closed the group', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        ...view(closedByAdminGroup),
        balances: greeceTripInTwoCurrencies,
        settlements: [...settlements],
      })
      await findBalanceCards()
      await findBarTitled(closedByAdminBarText)

      expect(paymentControlCount()).toBe(0)
    })
  })
})
