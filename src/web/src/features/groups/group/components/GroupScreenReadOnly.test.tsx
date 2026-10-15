import { screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { routes } from '@/core/router/routes'
import { renderGroupApp, type GroupAppOptions } from '@/test/activityTestHelpers'
import {
  greeceTripInTwoCurrencies,
  markoPaidAnaPending,
  markoPaidFilipPending,
} from '@/test/balancesTestData'
import { findBalanceCards, queryAllSectionsNamed } from '@/test/balancesTestHelpers'
import {
  closingGroup,
  finishedBarText,
  finishedGroup,
  finishedGroupWithoutAnswers,
  closingTexts,
} from '@/test/closingTestData'
import { anaView, findBarTitled, findClosingBar, ownerView } from '@/test/closingTestHelpers'
import { freezeTime, unfreezeTime } from '@/test/expenseTestHelpers'
import { testGroup } from '@/test/groupTestData'
import { anaMember, anaUserId, filipMember } from '@/test/memberTestData'
import { confirmedPaymentOf, paymentIds, pendingPaymentOf } from '@/test/settlementActionsTestData'
import { allButtonsNamed, buttonsNamedIn } from '@/test/settlementActionsTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { testMe } from '@/test/testMe'
import type { Group } from '@/core/services/groups/groupsService'
import type { Settlement } from '@/core/services/settlements/settlementsService'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupId = testGroup.id
const promptParams = { name: 'Filip', group: testGroup.name }

const filipPaysAnaInEuros = pendingPaymentOf(filipMember, anaMember, 2000, {
  id: paymentIds.second,
  currency: 'EUR',
})
const anaPaidFilipConfirmed = confirmedPaymentOf(anaMember, filipMember, 10000, {
  id: paymentIds.third,
  recordedByUserId: testMe.id,
})
const anaPaidFilipConfirmedByAna = confirmedPaymentOf(anaMember, filipMember, 10000, {
  id: paymentIds.fourth,
  recordedByUserId: anaUserId,
})
const anaPaidFilipWaiting = pendingPaymentOf(anaMember, filipMember, 50000, {
  id: paymentIds.first,
})

const ownerSettlements: Settlement[] = [markoPaidFilipPending, filipPaysAnaInEuros, anaPaidFilipConfirmed]
const anaSettlements: Settlement[] = [markoPaidAnaPending, anaPaidFilipWaiting, anaPaidFilipConfirmedByAna]

interface Viewer {
  name: string
  view: (group: Group) => GroupAppOptions
  settlements: Settlement[]
}

const owner: Viewer = { name: 'the owner', view: (group) => ownerView(group), settlements: ownerSettlements }
const member: Viewer = { name: 'a member', view: (group) => anaView(group), settlements: anaSettlements }

const openGroup: Group = { ...testGroup, memberCount: 5 }

interface PaymentControls {
  markPaid: number
  confirm: number
  reject: number
  cancel: number
  delete: number
  recordAnother: number
}

function paymentControls(): PaymentControls {
  const needsYou = queryAllSectionsNamed('Needs you')
  return {
    markPaid: allButtonsNamed('Mark paid').length,
    confirm: needsYou.flatMap((section) => buttonsNamedIn(section, 'Confirm')).length,
    reject: allButtonsNamed('Reject').length,
    cancel: allButtonsNamed('Cancel').length,
    delete: allButtonsNamed('Delete').length,
    recordAnother: allButtonsNamed('Record another payment').length,
  }
}

async function renderBalances(viewer: Viewer, group: Group) {
  const rendered = await renderGroupApp(routes.groupBalances(groupId), {
    ...viewer.view(group),
    balances: greeceTripInTwoCurrencies,
    settlements: viewer.settlements,
  })
  await findBalanceCards()
  return rendered
}

describe('the group screen when the group is read-only', () => {
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

  describe.each([owner, member])('the payment buttons on the Balances tab, for $name', (viewer) => {
    it('has every kind of button while the group is open, so the other tests prove something', async () => {
      await renderBalances(viewer, openGroup)

      const controls = paymentControls()

      expect(controls.markPaid).toBeGreaterThan(0)
      expect(controls.confirm).toBeGreaterThan(0)
      expect(controls.reject).toBeGreaterThan(0)
      expect(controls.cancel).toBeGreaterThan(0)
      expect(controls.delete).toBeGreaterThan(0)
      expect(controls.recordAnother).toBeGreaterThan(0)
    })

    it('has no Mark paid, Confirm, Reject, Cancel, Delete or Record another payment when the group is finished', async () => {
      await renderBalances(viewer, finishedGroup)
      await findClosingBar(finishedBarText.en)

      expect(paymentControls()).toEqual({
        markPaid: 0,
        confirm: 0,
        reject: 0,
        cancel: 0,
        delete: 0,
        recordAnother: 0,
      })
    })
  })

  describe('the payment buttons on the Balances tab while the group is being closed', () => {
    it('stay for the owner, because a change of a payment cancels the closing', async () => {
      await renderBalances(owner, closingGroup)
      await findBarTitled(closingTexts.waitingTitle)

      const controls = paymentControls()

      expect(controls.markPaid).toBeGreaterThan(0)
      expect(controls.confirm).toBeGreaterThan(0)
      expect(controls.reject).toBeGreaterThan(0)
      expect(controls.cancel).toBeGreaterThan(0)
      expect(controls.delete).toBeGreaterThan(0)
      expect(controls.recordAnother).toBeGreaterThan(0)
    })

    it('are gone for a member, who can only Confirm or Object', async () => {
      await renderBalances(member, closingGroup)
      await findBarTitled(closingTexts.prompt, 'en', promptParams)

      expect(paymentControls()).toEqual({
        markPaid: 0,
        confirm: 0,
        reject: 0,
        cancel: 0,
        delete: 0,
        recordAnother: 0,
      })
    })
  })

  describe('the Expenses tab', () => {
    it('keeps the rows of the expenses as links to their details when the group is finished', async () => {
      await renderGroupApp(routes.group(groupId), ownerView(finishedGroup))

      expect(await screen.findByRole('link', { name: /Dinner/ })).toBeTruthy()
    })

    it('keeps the links to Members and to Group settings, because Unlock and Leave group live there', async () => {
      await renderGroupApp(routes.group(groupId), anaView(finishedGroup))
      await findClosingBar(finishedBarText.en)

      expect(screen.getByRole('link', { name: 'Members' })).toBeTruthy()
      expect(screen.getByRole('link', { name: 'Group settings' })).toBeTruthy()
    })

    it('shows the Add a name button of a group with one person and no expenses while the group is open', async () => {
      await renderGroupApp(routes.group(groupId), { ...ownerView({ ...testGroup, memberCount: 1 }), expenses: [] })

      expect(await screen.findByRole('button', { name: 'Add a name' })).toBeTruthy()
    })

    it('shows no Add a name button for a finished group with one person and no expenses', async () => {
      await renderGroupApp(routes.group(groupId), { ...ownerView(finishedGroupWithoutAnswers), expenses: [] })
      await findClosingBar(finishedBarText.en)
      await screen.findByText('No expenses yet')

      expect(screen.queryByRole('button', { name: 'Add a name' })).toBeNull()
    })
  })
})
