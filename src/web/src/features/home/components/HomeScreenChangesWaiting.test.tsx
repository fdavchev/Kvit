import { act, cleanup, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { amountParagraphIn } from '@/test/balancesTestHelpers'
import { money } from '@/test/budgetLeftCardTestHelpers'
import { regionNamed, renderHome } from '@/test/homeTestHelpers'
import {
  otherGroupId,
  queuedExpense,
  queuedOneBill,
  queuedPersonalEntry,
  queuedSettlement,
} from '@/test/outboxScreenTestData'
import { noteText } from '@/test/outboxScreenTestHelpers'
import { seedOutbox } from '@/test/outboxTestHelpers'
import { testMe } from '@/test/testMe'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

async function renderHomeWithWaiting(waiting: OutboxItem[] = [], language: Language = 'en') {
  seedOutbox(testMe.id, waiting)
  const rendered = await renderHome({ language })
  await screen.findByRole('region', { name: translated(language, 'home.owed') })
  return rendered
}

function anyNote(): HTMLElement | null {
  return screen.queryByText(/not counted yet/)
}

function totalsText(language: Language = 'en'): string {
  return ['home.owed', 'home.owe']
    .map((key) => (regionNamed(translated(language, key)).textContent ?? '').replace(/\s+/g, ' '))
    .join(' | ')
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('the home screen with changes waiting in the outbox', () => {
  it('says how many changes are waiting and not counted yet', async () => {
    await renderHomeWithWaiting([queuedExpense('expense'), queuedSettlement('payment'), queuedOneBill('bill')])

    expect(await screen.findByText(noteText(3))).toBeTruthy()
  })

  it.each(languages)('writes the note in the language of the app (%s)', async (language) => {
    await renderHomeWithWaiting([queuedExpense('expense')], language)

    expect(await screen.findByText(noteText(1, language))).toBeTruthy()
  })

  it('counts the changes of every group', async () => {
    await renderHomeWithWaiting([queuedExpense('mine'), queuedExpense('theirs', { groupId: otherGroupId })])

    expect(await screen.findByText(noteText(2))).toBeTruthy()
  })

  it('counts a change that is being sent', async () => {
    await renderHomeWithWaiting([queuedExpense('on-its-way', { state: 'sending' })])

    expect(await screen.findByText(noteText(1))).toBeTruthy()
  })

  it('shows no note when nothing waits', async () => {
    await renderHomeWithWaiting()

    expect(anyNote()).toBeNull()
  })

  it('shows no note for a change that failed', async () => {
    await renderHomeWithWaiting([queuedExpense('refused', { state: 'failed' })])

    expect(anyNote()).toBeNull()
  })

  it('shows no note for personal entries, which do not change what you owe or are owed', async () => {
    await renderHomeWithWaiting([queuedPersonalEntry('entry')])

    expect(anyNote()).toBeNull()
  })

  it('keeps the two totals boxes as the server answered, without the waiting amount', async () => {
    await renderHomeWithWaiting([queuedExpense('big', { display: { amountMinor: 99900000 } })])
    await screen.findByText(noteText(1))

    const owed = regionNamed(translated('en', 'home.owed'))
    const owe = regionNamed(translated('en', 'home.owe'))

    expect(amountParagraphIn(owed, money(185000, 'MKD'))).toBeTruthy()
    expect(amountParagraphIn(owed, money(2000, 'EUR'))).toBeTruthy()
    expect(amountParagraphIn(owe, money(60000, 'MKD'))).toBeTruthy()
    expect(owed.textContent).not.toContain('999')
    expect(owe.textContent).not.toContain('999')
  })

  it('does not change the totals boxes at all, compared with the same screen when nothing waits', async () => {
    await renderHomeWithWaiting()
    const before = totalsText()
    cleanup()

    await renderHomeWithWaiting([queuedExpense('a'), queuedSettlement('b'), queuedOneBill('c')])
    await screen.findByText(noteText(3))

    expect(totalsText()).toBe(before)
  })

  it('takes the note away when the waiting change leaves the outbox', async () => {
    const { outboxSender } = await renderHomeWithWaiting([queuedExpense('waiting')])
    await screen.findByText(noteText(1))

    await act(async () => {
      await outboxSender.discard('waiting')
    })

    await waitFor(() => {
      expect(anyNote()).toBeNull()
    })
  })
})
