import { fireEvent, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { routes } from '@/core/router/routes'
import { renderGroupApp, type GroupAppOptions } from '@/test/activityTestHelpers'
import {
  greeceTripBalances,
  markoPaidFilipPending,
  settlementIds,
  settlementPathOf,
} from '@/test/balancesTestData'
import { rowsOf, rowWithText, sectionNamed } from '@/test/balancesTestHelpers'
import { freezeTime, membersPath, unfreezeTime } from '@/test/expenseTestHelpers'
import { testGroup } from '@/test/groupTestData'
import { filipMember, markoMember, ownerViewMembers } from '@/test/memberTestData'
import { noContentAnswer, requestCount } from '@/test/requestTestHelpers'
import {
  confirmedPaymentOf,
  paymentIds,
  seedActivityAndExpenses,
} from '@/test/settlementActionsTestData'
import { buttonNamedIn, findCardsInLanguage } from '@/test/settlementActionsTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { testMe } from '@/test/testMe'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupId = testGroup.id
const filipView: GroupAppOptions = { members: ownerViewMembers, group: testGroup }
const markoSentence = 'Marko says they paid you 800 MKD'

const markoPaidFilip = confirmedPaymentOf(markoMember, filipMember, 80000, {
  id: paymentIds.first,
  recordedByUserId: testMe.id,
  createdAt: '2026-10-06T08:00:00Z',
})

async function renderBalances(options: GroupAppOptions = {}) {
  const rendered = await renderGroupApp(routes.groupBalances(groupId), {
    balances: greeceTripBalances,
    ...filipView,
    seedCache: seedActivityAndExpenses,
    ...options,
  })
  await findCardsInLanguage('en')
  return rendered
}

describe('the members after a payment changes on the Balances tab', () => {
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

  it('asks for the members once while nothing has changed', async () => {
    const { fetchMock } = await renderBalances({ settlements: [markoPaidFilipPending] })

    expect(requestCount(fetchMock, 'GET', membersPath)).toBe(1)
  })

  it('asks for the members again after a payment is confirmed', async () => {
    const confirmPath = `${settlementPathOf(settlementIds.markoToFilip)}/confirm`
    const { fetchMock } = await renderBalances({
      settlements: [markoPaidFilipPending],
      answers: { [`POST ${confirmPath}`]: noContentAnswer() },
    })

    fireEvent.click(buttonNamedIn(rowWithText(sectionNamed('Needs you'), markoSentence), 'Confirm'))

    await waitFor(() => {
      expect(requestCount(fetchMock, 'GET', membersPath)).toBe(2)
    })
  })

  it('asks for the members again after a payment is rejected', async () => {
    const rejectPath = `${settlementPathOf(settlementIds.markoToFilip)}/reject`
    const { fetchMock } = await renderBalances({
      settlements: [markoPaidFilipPending],
      answers: { [`POST ${rejectPath}`]: noContentAnswer() },
    })

    fireEvent.click(buttonNamedIn(rowWithText(sectionNamed('Needs you'), markoSentence), 'Reject'))

    await waitFor(() => {
      expect(requestCount(fetchMock, 'GET', membersPath)).toBe(2)
    })
  })

  it('asks for the members again after a payment is deleted', async () => {
    const { fetchMock } = await renderBalances({
      settlements: [markoPaidFilip],
      answers: { [`DELETE ${settlementPathOf(paymentIds.first)}`]: noContentAnswer() },
    })

    fireEvent.click(buttonNamedIn(rowsOf(sectionNamed('Payments'))[0], 'Delete'))

    await waitFor(() => {
      expect(requestCount(fetchMock, 'GET', membersPath)).toBe(2)
    })
  })
})
