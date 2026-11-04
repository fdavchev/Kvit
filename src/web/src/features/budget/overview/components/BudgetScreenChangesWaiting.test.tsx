import { act, cleanup, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { budgetNow, findTotalCard, renderBudget } from '@/test/budgetTestHelpers'
import {
  queuedExpense,
  queuedOneBill,
  queuedPersonalEntry,
  queuedSettlement,
} from '@/test/outboxScreenTestData'
import { noteText } from '@/test/outboxScreenTestHelpers'
import { seedOutbox } from '@/test/outboxTestHelpers'
import { octoberMonth } from '@/test/personalEntryTestData'
import { testMe } from '@/test/testMe'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

async function renderBudgetWithWaiting(waiting: OutboxItem[] = [], language: Language = 'en') {
  seedOutbox(testMe.id, waiting)
  const rendered = await renderBudget({ language, months: [octoberMonth] })
  await findTotalCard(language)
  return rendered
}

function anyNote(): HTMLElement | null {
  return screen.queryByText(/not counted yet/)
}

async function totalCardText(): Promise<string> {
  return ((await findTotalCard()).textContent ?? '').replace(/\s+/g, ' ')
}

describe('BudgetScreen with changes waiting in the outbox, near the total card', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(budgetNow)
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.clearAllMocks()
    vi.restoreAllMocks()
  })

  it('says how many personal entries are waiting and not counted yet', async () => {
    await renderBudgetWithWaiting([queuedPersonalEntry('a'), queuedPersonalEntry('b')])

    expect(await screen.findByText(noteText(2))).toBeTruthy()
  })

  it.each(languages)('writes the note in the language of the app (%s)', async (language) => {
    await renderBudgetWithWaiting([queuedPersonalEntry('a')], language)

    expect(await screen.findByText(noteText(1, language))).toBeTruthy()
  })

  it('counts an entry that is being sent', async () => {
    await renderBudgetWithWaiting([queuedPersonalEntry('on-its-way', { state: 'sending' })])

    expect(await screen.findByText(noteText(1))).toBeTruthy()
  })

  it('counts a waiting entry of another month too, because it will change a total once it is saved', async () => {
    await renderBudgetWithWaiting([queuedPersonalEntry('september', { display: { date: '2026-09-30' } })])

    expect(await screen.findByText(noteText(1))).toBeTruthy()
  })

  it('shows no note when nothing waits', async () => {
    await renderBudgetWithWaiting()

    expect(anyNote()).toBeNull()
  })

  it('shows no note for a change that failed', async () => {
    await renderBudgetWithWaiting([queuedPersonalEntry('refused', { state: 'failed' })])

    expect(anyNote()).toBeNull()
  })

  it('shows no note for expenses, payments or One bills, which do not change the budget', async () => {
    await renderBudgetWithWaiting([queuedExpense('expense'), queuedSettlement('payment'), queuedOneBill('bill')])

    expect(anyNote()).toBeNull()
  })

  it('does not change the total card: it stays what the server answered', async () => {
    await renderBudgetWithWaiting()
    const before = await totalCardText()
    cleanup()

    await renderBudgetWithWaiting([queuedPersonalEntry('big', { display: { amountMinor: 99900000 } })])
    await screen.findByText(noteText(1))

    expect(await totalCardText()).toBe(before)
  })

  it('takes the note away when the waiting entry leaves the outbox', async () => {
    const { outboxSender } = await renderBudgetWithWaiting([queuedPersonalEntry('waiting')])
    await screen.findByText(noteText(1))

    await act(async () => {
      await outboxSender.discard('waiting')
    })

    await waitFor(() => {
      expect(anyNote()).toBeNull()
    })
  })
})
