import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { budgetNow, findEntryList, findTotalCard, monthButton, renderBudget } from '@/test/budgetTestHelpers'
import { money } from '@/test/budgetLeftCardTestHelpers'
import { isBefore } from '@/test/expenseTestHelpers'
import { queuedExpense, queuedOneBill, queuedPersonalEntry, queuedSettlement } from '@/test/outboxScreenTestData'
import {
  addSendingItem,
  closeActionOfDiscardSheet,
  discardSheet,
  discardThroughTheSheet,
  expectNoWaitingRows,
  findWaitingRowWith,
  savedIdsOfTheSignedInPerson,
  tapTargetOf,
  waitingRows,
  waitingRowText,
  waitingRowTexts,
} from '@/test/outboxScreenTestHelpers'
import { outboxText } from '@/test/outboxTextsTestData'
import { seedOutbox } from '@/test/outboxTestHelpers'
import { coffeeCategory, personalFoodCategory } from '@/test/personalCategoryTestData'
import { emptyMonth, octoberMonth, septemberWithArchivedCategory } from '@/test/personalEntryTestData'
import { personalCategoryLabel } from '@/test/personalCategoryTestHelpers'
import { testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import type { PersonalEntryMonth } from '@/core/services/personalEntries/personalEntriesService'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

interface RenderOptions {
  language?: Language
  waiting?: OutboxItem[]
  month?: PersonalEntryMonth
}

async function renderBudgetWithWaiting(options: RenderOptions = {}) {
  seedOutbox(testMe.id, options.waiting ?? [])
  const rendered = await renderBudget({
    language: options.language,
    months: [options.month ?? octoberMonth, septemberWithArchivedCategory],
  })
  return rendered
}

function realEntryItems(list: HTMLElement): HTMLElement[] {
  return Array.from(list.querySelectorAll<HTMLElement>('li')).filter(
    (item) => !item.matches('[data-waiting-row]') && item.querySelector('[data-waiting-row]') === null,
  )
}

describe('BudgetScreen with changes waiting in the outbox', () => {
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

  describe('a waiting personal entry of the month on the screen', () => {
    it('is shown as a greyed row with its note, its amount and "Not synced yet"', async () => {
      await renderBudgetWithWaiting({ waiting: [queuedPersonalEntry('waiting')] })

      const row = await findWaitingRowWith('Bakery run')

      expect(waitingRowText(row)).toContain(money(70000, 'MKD'))
      expect(waitingRowText(row)).toContain(outboxText('en', 'outbox.notSynced'))
    })

    it.each(languages)('writes "Not synced yet" in the language of the app (%s)', async (language) => {
      await renderBudgetWithWaiting({ language, waiting: [queuedPersonalEntry('waiting')] })

      const row = await findWaitingRowWith('Bakery run')

      expect(waitingRowText(row)).toContain(outboxText(language, 'outbox.notSynced'))
    })

    it('is shown while it is being sent', async () => {
      const { outboxSender } = await renderBudgetWithWaiting()
      await findTotalCard()

      await addSendingItem(outboxSender, queuedPersonalEntry('on-its-way'))

      expect(await findWaitingRowWith('Bakery run')).toBeTruthy()
    })

    it('is shown by the name of its category when it has no note', async () => {
      await renderBudgetWithWaiting({
        waiting: [queuedPersonalEntry('no-note', { display: { title: undefined, categoryId: coffeeCategory.id } })],
      })

      await waitFor(() => {
        expect(waitingRowTexts().some((text) => text.includes(personalCategoryLabel('en', coffeeCategory)))).toBe(true)
      })
    })

    it('is shown by the name of a built-in category in the language of the app', async () => {
      await renderBudgetWithWaiting({
        language: 'mk',
        waiting: [queuedPersonalEntry('no-note', { display: { title: undefined, categoryId: personalFoodCategory.id } })],
      })

      await waitFor(() => {
        expect(waitingRowTexts().some((text) => text.includes(personalCategoryLabel('mk', personalFoodCategory)))).toBe(true)
      })
    })

    it('is shown as "Income" when it is income without a note', async () => {
      await renderBudgetWithWaiting({
        waiting: [
          queuedPersonalEntry('income', { display: { title: undefined, entryKind: 'Income', categoryId: null, amountMinor: 5000000 } }),
        ],
      })

      const row = await findWaitingRowWith(translated('en', 'budget.income.listTitle'))

      expect(waitingRowText(row)).toContain(money(5000000, 'MKD'))
    })

    it('is shown by its note when it is income with a note', async () => {
      await renderBudgetWithWaiting({
        waiting: [queuedPersonalEntry('income', { display: { title: 'Birthday money', entryKind: 'Income', categoryId: null } })],
      })

      expect(await findWaitingRowWith('Birthday money')).toBeTruthy()
    })

    it('says that the euro price is set when sent, when the entry is in euros', async () => {
      await renderBudgetWithWaiting({
        waiting: [queuedPersonalEntry('euros', { display: { title: 'Ferry', amountMinor: 4550, currency: 'EUR' } })],
      })

      const row = await findWaitingRowWith('Ferry')

      expect(waitingRowText(row)).toContain(money(4550, 'EUR'))
      expect(waitingRowText(row)).toContain(outboxText('en', 'outbox.rateWhenSent'))
    })

    it('is not a link and has a button to tap', async () => {
      await renderBudgetWithWaiting({ waiting: [queuedPersonalEntry('waiting')] })

      const row = await findWaitingRowWith('Bakery run')

      expect(row.closest('a')).toBeNull()
      expect(tapTargetOf(row).tagName).toBe('BUTTON')
    })

    it('is shown also when the month has no entry on the server yet', async () => {
      await renderBudgetWithWaiting({ month: emptyMonth('2026-10'), waiting: [queuedPersonalEntry('first-ever')] })

      expect(await findWaitingRowWith('Bakery run')).toBeTruthy()
    })
  })

  describe('what is not shown', () => {
    it('shows no waiting row when nothing waits', async () => {
      await renderBudgetWithWaiting()
      await findEntryList()

      expect(waitingRows()).toHaveLength(0)
    })

    it('shows no row for an entry that failed', async () => {
      await renderBudgetWithWaiting({ waiting: [queuedPersonalEntry('refused', { state: 'failed' })] })
      await findEntryList()

      await expectNoWaitingRows()
    })

    it('shows no row for an entry of another month', async () => {
      await renderBudgetWithWaiting({
        waiting: [queuedPersonalEntry('september', { display: { title: 'September lunch', date: '2026-09-30' } })],
      })
      await findEntryList()

      await expectNoWaitingRows()
    })

    it('shows no row for an entry of the next year with the same month number', async () => {
      await renderBudgetWithWaiting({
        waiting: [queuedPersonalEntry('next-year', { display: { title: 'Next year lunch', date: '2027-10-19' } })],
      })
      await findEntryList()

      await expectNoWaitingRows()
    })

    it('shows no row for an expense, a payment or a One bill', async () => {
      await renderBudgetWithWaiting({
        waiting: [
          queuedExpense('expense', { display: { date: '2026-10-19' } }),
          queuedSettlement('payment'),
          queuedOneBill('bill', { display: { date: '2026-10-19' } }),
        ],
      })
      await findEntryList()

      await expectNoWaitingRows()
    })

    it('shows the row of an entry of another month when that month is opened', async () => {
      await renderBudgetWithWaiting({
        waiting: [queuedPersonalEntry('september', { display: { title: 'September lunch', date: '2026-09-30' } })],
      })
      await findEntryList()

      fireEvent.click(monthButton('Sep'))

      expect(await findWaitingRowWith('September lunch')).toBeTruthy()
    })

    it('does not write the waiting entry into the saved data of the screen', async () => {
      const { queryClient } = await renderBudgetWithWaiting({ waiting: [queuedPersonalEntry('waiting')] })
      await findWaitingRowWith('Bakery run')

      const cachedData = queryClient
        .getQueryCache()
        .getAll()
        .map((query) => JSON.stringify(query.state.data ?? null))
        .join('\n')

      expect(cachedData).not.toContain('Bakery run')
    })
  })

  describe('where the waiting rows sit', () => {
    it('keeps every real entry on the screen', async () => {
      await renderBudgetWithWaiting({ waiting: [queuedPersonalEntry('waiting')] })
      await findWaitingRowWith('Bakery run')

      const list = await findEntryList()

      expect(realEntryItems(list)).toHaveLength(octoberMonth.entries.length)
    })

    it('puts a waiting row above the first real entry', async () => {
      await renderBudgetWithWaiting({ waiting: [queuedPersonalEntry('waiting')] })
      const row = await findWaitingRowWith('Bakery run')
      const list = await findEntryList()

      expect(isBefore(row, realEntryItems(list)[0])).toBe(true)
    })

    it('puts the newest waiting row first', async () => {
      await renderBudgetWithWaiting({
        waiting: [
          queuedPersonalEntry('older', { createdAt: '2026-10-19T10:00:00.000Z', display: { title: 'First queued' } }),
          queuedPersonalEntry('newer', { createdAt: '2026-10-19T10:05:00.000Z', display: { title: 'Second queued' } }),
        ],
      })
      await findWaitingRowWith('Second queued')

      expect(waitingRowTexts()[0]).toContain('Second queued')
      expect(waitingRowTexts()[1]).toContain('First queued')
    })
  })

  describe('tapping a waiting row', () => {
    it('opens the sheet that asks whether to discard the change', async () => {
      await renderBudgetWithWaiting({ waiting: [queuedPersonalEntry('waiting')] })
      const row = await findWaitingRowWith('Bakery run')

      fireEvent.click(tapTargetOf(row))

      expect(discardSheet()).toBeTruthy()
    })

    it('takes the entry out of the outbox, and the row off the screen, when Discard is pressed', async () => {
      await renderBudgetWithWaiting({
        waiting: [queuedPersonalEntry('waiting'), queuedPersonalEntry('other', { display: { title: 'Taxi home' } })],
      })
      const row = await findWaitingRowWith('Bakery run')

      await discardThroughTheSheet(row)

      await waitFor(() => {
        expect(waitingRowTexts().some((text) => text.includes('Bakery run'))).toBe(false)
      })
      expect(savedIdsOfTheSignedInPerson()).toEqual(['other'])
    })

    it('keeps the entry when the sheet is closed without discarding', async () => {
      await renderBudgetWithWaiting({ waiting: [queuedPersonalEntry('waiting')] })
      const row = await findWaitingRowWith('Bakery run')
      fireEvent.click(tapTargetOf(row))

      fireEvent.click(closeActionOfDiscardSheet())

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      expect(savedIdsOfTheSignedInPerson()).toEqual(['waiting'])
    })

    it('does not open the sheet to edit an entry', async () => {
      await renderBudgetWithWaiting({ waiting: [queuedPersonalEntry('waiting')] })
      const row = await findWaitingRowWith('Bakery run')

      fireEvent.click(tapTargetOf(row))

      expect(discardSheet()).toBeTruthy()
      expect(screen.getAllByRole('dialog')).toHaveLength(1)
    })
  })
})
