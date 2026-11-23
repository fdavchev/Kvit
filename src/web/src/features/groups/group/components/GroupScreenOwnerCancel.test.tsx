import { fireEvent, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { routes } from '@/core/router/routes'
import { renderGroupApp, type GroupAppOptions } from '@/test/activityTestHelpers'
import {
  markoOwesAnaBalances,
  markoPaidAnaPending,
  settlementIds,
  settlementOf,
  settlementPathOf,
} from '@/test/balancesTestData'
import { closingGroup, finishedGroup } from '@/test/closingTestData'
import { freezeTime, unfreezeTime } from '@/test/expenseTestHelpers'
import { testGroup } from '@/test/groupTestData'
import { anaMember, ownerViewMembers, petarMember } from '@/test/memberTestData'
import { noContentAnswer, requestCount } from '@/test/requestTestHelpers'
import { anaSignedIn, onePaymentBalances, seedActivityAndExpenses } from '@/test/settlementActionsTestData'
import {
  allButtonsNamed,
  betweenOthersRow,
  buttonNamedIn,
  buttonsNamedIn,
  findCardsInLanguage,
} from '@/test/settlementActionsTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupId = testGroup.id
const filipView: GroupAppOptions = { members: ownerViewMembers, group: testGroup }
const cancelPath = `${settlementPathOf(settlementIds.markoToAna)}/cancel`

async function renderBalances(options: GroupAppOptions) {
  const rendered = await renderGroupApp(routes.groupBalances(groupId), {
    balances: markoOwesAnaBalances,
    settlements: [markoPaidAnaPending],
    seedCache: seedActivityAndExpenses,
    ...options,
  })
  await findCardsInLanguage('en')
  return rendered
}

describe('Cancel for the owner on a waiting payment that a name without an account made', () => {
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

  it('is on the Between others row of that payment for the owner', async () => {
    await renderBalances(filipView)

    expect(buttonsNamedIn(betweenOthersRow('Marko pays Ana'), 'Cancel')).toHaveLength(1)
  })

  it('cancels that payment when the owner presses it', async () => {
    const { fetchMock } = await renderBalances({
      ...filipView,
      answers: { [`POST ${cancelPath}`]: noContentAnswer() },
    })

    fireEvent.click(buttonNamedIn(betweenOthersRow('Marko pays Ana'), 'Cancel'))

    await waitFor(() => {
      expect(requestCount(fetchMock, 'POST', cancelPath)).toBe(1)
    })
  })

  it('is there for the owner of a group that is being closed, because the owner may still change it', async () => {
    await renderBalances({ ...filipView, group: closingGroup })

    expect(buttonsNamedIn(betweenOthersRow('Marko pays Ana'), 'Cancel')).toHaveLength(1)
  })

  it('is not there in a finished group', async () => {
    await renderBalances({ ...filipView, group: finishedGroup })

    expect(allButtonsNamed('Cancel')).toEqual([])
  })

  it('is not there for a member who is not the owner', async () => {
    await renderBalances(anaSignedIn())

    expect(allButtonsNamed('Cancel')).toEqual([])
  })

  it('is not there for the owner when the payer of the waiting payment has an account', async () => {
    const petarPaidAnaPending = settlementOf({
      id: settlementIds.markoToAna,
      fromMemberId: petarMember.id,
      fromName: petarMember.displayName,
      toMemberId: anaMember.id,
      toName: 'Ana',
    })
    await renderBalances({
      ...filipView,
      balances: onePaymentBalances(petarMember, anaMember, 80000),
      settlements: [petarPaidAnaPending],
    })

    expect(allButtonsNamed('Cancel')).toEqual([])
  })
})
