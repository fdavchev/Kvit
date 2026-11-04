import { act, cleanup, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { routes } from '@/core/router/routes'
import { greeceTripBalances } from '@/test/balancesTestData'
import { findBalanceCards } from '@/test/balancesTestHelpers'
import { testGroup, testGroupId } from '@/test/groupTestData'
import { expectRows, findCurrencyRegion, mkdOnlyRows, totalAmountElements } from '@/test/groupSpendingTestHelpers'
import { mkdOnlySpending } from '@/test/groupSpendingTestData'
import { ownerViewMembers } from '@/test/memberTestData'
import {
  otherGroupId,
  queuedExpense,
  queuedOneBill,
  queuedPersonalEntry,
  queuedSettlement,
} from '@/test/outboxScreenTestData'
import { noteText, renderGroupTab } from '@/test/outboxScreenTestHelpers'
import { seedOutbox } from '@/test/outboxTestHelpers'
import { findCardsInLanguage } from '@/test/settlementActionsTestHelpers'
import { testMe } from '@/test/testMe'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const waitingInThisGroup = [queuedExpense('expense'), queuedSettlement('payment')]

async function renderBalances(waiting: OutboxItem[] = [], language: Language = 'en') {
  seedOutbox(testMe.id, waiting)
  const rendered = await renderGroupTab(routes.groupBalances(testGroupId), {
    language,
    balances: greeceTripBalances,
    members: ownerViewMembers,
    group: testGroup,
  })
  await findCardsInLanguage(language)
  return rendered
}

async function renderTotals(waiting: OutboxItem[] = [], language: Language = 'en') {
  seedOutbox(testMe.id, waiting)
  const rendered = await renderGroupTab(routes.groupTotals(testGroupId), {
    language,
    spending: mkdOnlySpending,
    members: ownerViewMembers,
    group: testGroup,
  })
  await findCurrencyRegion('MKD', language)
  return rendered
}

function note(count: number, language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByText(noteText(count, language))
}

function anyNote(): HTMLElement | null {
  return screen.queryByText(/not counted yet/)
}

async function balanceCardTexts(): Promise<string[]> {
  return (await findBalanceCards()).map((card) => (card.textContent ?? '').replace(/\s+/g, ' '))
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('the Balances tab with changes waiting in the outbox', () => {
  it('says how many changes of this group are waiting and not counted yet', async () => {
    await renderBalances(waitingInThisGroup)

    expect(await note(2)).toBeTruthy()
  })

  it.each(languages)('writes the note in the language of the app (%s)', async (language) => {
    await renderBalances(waitingInThisGroup, language)

    expect(await note(2, language)).toBeTruthy()
  })

  it('counts a change that is being sent', async () => {
    await renderBalances([queuedExpense('on-its-way', { state: 'sending' })])

    expect(await note(1)).toBeTruthy()
  })

  it('shows no note when nothing waits', async () => {
    await renderBalances()

    expect(anyNote()).toBeNull()
  })

  it('shows no note for changes of another group', async () => {
    await renderBalances([queuedExpense('theirs', { groupId: otherGroupId }), queuedSettlement('their-payment', { groupId: otherGroupId })])

    expect(anyNote()).toBeNull()
  })

  it('counts only the changes of this group when other changes wait too', async () => {
    await renderBalances([...waitingInThisGroup, queuedExpense('theirs', { groupId: otherGroupId })])

    expect(await note(2)).toBeTruthy()
  })

  it('shows no note for personal entries or One bills', async () => {
    await renderBalances([queuedPersonalEntry('entry'), queuedOneBill('bill')])

    expect(anyNote()).toBeNull()
  })

  it('shows no note for a change that failed', async () => {
    await renderBalances([queuedExpense('refused', { state: 'failed' })])

    expect(anyNote()).toBeNull()
  })

  it('does not change the balances: they stay those of the server answer', async () => {
    await renderBalances()
    const before = await balanceCardTexts()
    cleanup()

    await renderBalances([queuedExpense('big', { display: { amountMinor: 99900000 } }), queuedSettlement('payment')])
    await note(2)

    expect(await balanceCardTexts()).toEqual(before)
  })

  it('does not change the balances when the waiting changes are many', async () => {
    await renderBalances()
    const before = await balanceCardTexts()
    cleanup()

    await renderBalances(Array.from({ length: 12 }, (_value, index) => queuedExpense(`item-${index}`)))
    await note(12)

    expect(await balanceCardTexts()).toEqual(before)
  })

  it('takes the note away when the waiting change leaves the outbox', async () => {
    const { outboxSender } = await renderBalances([queuedExpense('waiting')])
    await note(1)

    await act(async () => {
      await outboxSender.discard('waiting')
    })

    await waitFor(() => {
      expect(anyNote()).toBeNull()
    })
  })
})

describe('the Totals tab with changes waiting in the outbox', () => {
  it('says how many changes of this group are waiting and not counted yet', async () => {
    await renderTotals(waitingInThisGroup)

    expect(await note(2)).toBeTruthy()
  })

  it.each(languages)('writes the note in the language of the app (%s)', async (language) => {
    await renderTotals(waitingInThisGroup, language)

    expect(await note(2, language)).toBeTruthy()
  })

  it('shows no note when nothing waits', async () => {
    await renderTotals()

    expect(anyNote()).toBeNull()
  })

  it('shows no note for changes of another group, personal entries or One bills', async () => {
    await renderTotals([
      queuedExpense('theirs', { groupId: otherGroupId }),
      queuedPersonalEntry('entry'),
      queuedOneBill('bill'),
    ])

    expect(anyNote()).toBeNull()
  })

  it('shows no note for a change that failed', async () => {
    await renderTotals([queuedExpense('refused', { state: 'failed' })])

    expect(anyNote()).toBeNull()
  })

  it('does not change the totals: they stay those of the server answer, without the waiting amount', async () => {
    await renderTotals([queuedExpense('big', { display: { amountMinor: 99900000 } })])
    await note(1)

    const region = await findCurrencyRegion('MKD')

    expect(totalAmountElements(region, 135000, 'MKD')).not.toHaveLength(0)
    expect(totalAmountElements(region, 135000 + 99900000, 'MKD')).toHaveLength(0)
    expectRows(region, 'MKD', mkdOnlyRows())
  })
})
