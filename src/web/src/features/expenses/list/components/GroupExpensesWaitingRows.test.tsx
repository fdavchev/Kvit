import { act, fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import type { ExpenseListRow } from '@/core/services/expenses/expensesService'
import { money } from '@/test/budgetLeftCardTestHelpers'
import { dinnerRow, expenseRows, foodCategory, testCategories } from '@/test/expenseTestData'
import { categoriesPath, categoryName, expensesPath, isBefore } from '@/test/expenseTestHelpers'
import { testGroupId } from '@/test/groupTestData'
import {
  otherGroupId,
  queuedExpense,
  queuedOneBill,
  queuedPersonalEntry,
  queuedSettlement,
} from '@/test/outboxScreenTestData'
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
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { jsonAnswer, stubFetchByRequest } from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { GroupExpenses } from './GroupExpenses'
import type { OutboxItem } from '@/core/outbox/outboxItem'

interface RenderOptions {
  language?: Language
  expenses?: ExpenseListRow[]
  waiting?: OutboxItem[]
}

async function renderExpenses(options: RenderOptions = {}) {
  const language = options.language ?? 'en'
  stubFetchByRequest({
    [`GET ${expensesPath}`]: jsonAnswer({ expenses: options.expenses ?? expenseRows }),
    [`GET ${categoriesPath}`]: jsonAnswer({ categories: testCategories }),
  })
  seedOutbox(testMe.id, options.waiting ?? [])
  return renderElementWithProviders(
    <GroupExpenses groupId={testGroupId} actionsWhenEmpty={null} />,
    '/expenses',
    { language, seedCache: seedMe({ ...testMe, language }) },
  )
}

function paidBy(name: string, language: Language = 'en'): string {
  return `${translated(language, 'expense.paidBy')} ${name}`
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('GroupExpenses with changes waiting in the outbox', () => {
  describe('a waiting expense of this group', () => {
    it('is shown as a greyed row with its title, its amount, who paid and "Not synced yet"', async () => {
      await renderExpenses({ waiting: [queuedExpense('waiting')] })

      const row = await findWaitingRowWith('Museum tickets')

      expect(waitingRowText(row)).toContain(money(150000, 'MKD'))
      expect(waitingRowText(row)).toContain(`${paidBy('Ana')} · ${outboxText('en', 'outbox.notSynced')}`)
    })

    it.each(languages)('writes the second line in the language of the app (%s)', async (language) => {
      await renderExpenses({ language, waiting: [queuedExpense('waiting')] })

      const row = await findWaitingRowWith('Museum tickets')

      expect(waitingRowText(row)).toContain(`${paidBy('Ana', language)} · ${outboxText(language, 'outbox.notSynced')}`)
    })

    it('is shown while it is being sent', async () => {
      const { outboxSender } = await renderExpenses()
      await screen.findByRole('link', { name: /Dinner/ })

      await addSendingItem(outboxSender, queuedExpense('on-its-way'))

      expect(await findWaitingRowWith('Museum tickets')).toBeTruthy()
    })

    it('is shown when it is queued after the list was already on the screen', async () => {
      const { outboxSender } = await renderExpenses()
      await screen.findByRole('link', { name: /Dinner/ })

      await act(async () => {
        await outboxSender.enqueue(queuedExpense('later'))
      })

      expect(await findWaitingRowWith('Museum tickets')).toBeTruthy()
    })

    it('is not a link and has a button to tap', async () => {
      await renderExpenses({ waiting: [queuedExpense('waiting')] })

      const row = await findWaitingRowWith('Museum tickets')

      expect(row.closest('a')).toBeNull()
      expect(tapTargetOf(row).tagName).toBe('BUTTON')
    })

    it('is shown by the name of its category when it has no title', async () => {
      await renderExpenses({
        waiting: [queuedExpense('no-title', { display: { title: undefined, categoryId: foodCategory.id } })],
      })

      const row = await findWaitingRowWith(categoryName('en', foodCategory))

      expect(waitingRowText(row)).toContain(money(150000, 'MKD'))
    })

    it('is shown with the dash of the real rows when it has neither a title nor a category', async () => {
      await renderExpenses({
        waiting: [queuedExpense('nothing', { display: { title: undefined, categoryId: null } })],
      })

      await waitFor(() => {
        expect(waitingRows()).toHaveLength(1)
      })

      expect(waitingRowText(waitingRows()[0])).toContain('—')
    })

    it('says that the euro price is set when sent, when the expense is in euros', async () => {
      await renderExpenses({
        waiting: [queuedExpense('euros', { display: { title: 'Ferry', amountMinor: 4550, currency: 'EUR' } })],
      })

      const row = await findWaitingRowWith('Ferry')

      expect(waitingRowText(row)).toContain(money(4550, 'EUR'))
      expect(waitingRowText(row)).toContain(outboxText('en', 'outbox.rateWhenSent'))
    })

    it('does not talk about the euro price when the expense is in denars', async () => {
      await renderExpenses({ waiting: [queuedExpense('denars')] })

      const row = await findWaitingRowWith('Museum tickets')

      expect(waitingRowText(row)).not.toContain(outboxText('en', 'outbox.rateWhenSent'))
    })

    it('is shown also when the group has no expense on the server yet', async () => {
      await renderExpenses({ expenses: [], waiting: [queuedExpense('first-ever')] })

      expect(await findWaitingRowWith('Museum tickets')).toBeTruthy()
    })
  })

  describe('what is not shown', () => {
    it('shows no waiting row when nothing waits, and the list is as before', async () => {
      await renderExpenses()

      await screen.findByRole('link', { name: /Dinner/ })

      expect(waitingRows()).toHaveLength(0)
    })

    it('shows no row for a change that failed, which is only told about in the bar and its sheet', async () => {
      await renderExpenses({ waiting: [queuedExpense('refused', { state: 'failed' })] })
      await screen.findByRole('link', { name: /Dinner/ })

      await expectNoWaitingRows()
      expect(screen.queryByText('Museum tickets')).toBeNull()
    })

    it('shows no row for an expense of another group', async () => {
      await renderExpenses({ waiting: [queuedExpense('theirs', { groupId: otherGroupId })] })
      await screen.findByRole('link', { name: /Dinner/ })

      await expectNoWaitingRows()
    })

    it('shows no row for a payment, a One bill or a personal entry', async () => {
      await renderExpenses({
        waiting: [queuedSettlement('payment'), queuedOneBill('bill'), queuedPersonalEntry('entry')],
      })
      await screen.findByRole('link', { name: /Dinner/ })

      await expectNoWaitingRows()
    })

    it('does not write the waiting expense into the saved data of the screen', async () => {
      const { queryClient } = await renderExpenses({ waiting: [queuedExpense('waiting')] })
      await findWaitingRowWith('Museum tickets')

      const cachedData = queryClient
        .getQueryCache()
        .getAll()
        .map((query) => JSON.stringify(query.state.data ?? null))
        .join('\n')

      expect(cachedData).not.toContain('Museum tickets')
    })
  })

  describe('where the waiting rows sit', () => {
    it('keeps every real expense on the screen', async () => {
      await renderExpenses({ waiting: [queuedExpense('waiting')] })
      await findWaitingRowWith('Museum tickets')

      expect(screen.getAllByRole('link', { name: /Paid by/ })).toHaveLength(expenseRows.length)
    })

    it('puts a waiting row above the first real expense', async () => {
      await renderExpenses({ waiting: [queuedExpense('waiting')] })

      const row = await findWaitingRowWith('Museum tickets')
      const firstRealRow = await screen.findByRole('link', { name: /Dinner/ })

      expect(isBefore(row, firstRealRow)).toBe(true)
    })

    it('puts a waiting row above the real expenses even when its date is older than all of them', async () => {
      await renderExpenses({
        waiting: [queuedExpense('old', { display: { title: 'Old receipt', date: '2025-01-02' } })],
      })

      const row = await findWaitingRowWith('Old receipt')
      const firstRealRow = await screen.findByRole('link', { name: /Dinner/ })

      expect(isBefore(row, firstRealRow)).toBe(true)
    })

    it('puts the newest waiting row first', async () => {
      await renderExpenses({
        waiting: [
          queuedExpense('older', { createdAt: '2026-10-06T10:00:00.000Z', display: { title: 'First queued' } }),
          queuedExpense('newer', { createdAt: '2026-10-06T10:05:00.000Z', display: { title: 'Second queued' } }),
        ],
      })
      await findWaitingRowWith('Second queued')

      expect(waitingRowTexts()[0]).toContain('Second queued')
      expect(waitingRowTexts()[1]).toContain('First queued')
    })

    it('does not touch the dinner row, whose title is the same as a waiting dinner', async () => {
      await renderExpenses({
        expenses: [dinnerRow],
        waiting: [queuedExpense('dinner', { display: { title: 'Dinner', amountMinor: 50000 } })],
      })

      const realRow = await screen.findByRole('link', { name: /Dinner/ })
      const waitingRow = await findWaitingRowWith('Dinner')

      expect(waitingRow).not.toBe(realRow)
      expect(waitingRow.closest('a')).toBeNull()
    })
  })

  describe('tapping a waiting row', () => {
    it('opens the sheet that asks whether to discard the change', async () => {
      await renderExpenses({ waiting: [queuedExpense('waiting')] })
      const row = await findWaitingRowWith('Museum tickets')

      fireEvent.click(tapTargetOf(row))

      expect(discardSheet()).toBeTruthy()
    })

    it('opens no other page', async () => {
      const { router } = await renderExpenses({ waiting: [queuedExpense('waiting')] })
      const row = await findWaitingRowWith('Museum tickets')

      fireEvent.click(tapTargetOf(row))

      expect(router.state.location.pathname).toBe('/expenses')
    })

    it('takes the change out of the outbox, and the row off the screen, when Discard is pressed', async () => {
      await renderExpenses({ waiting: [queuedExpense('waiting'), queuedExpense('other', { display: { title: 'Taxi home' } })] })
      const row = await findWaitingRowWith('Museum tickets')

      await discardThroughTheSheet(row)

      await waitFor(() => {
        expect(waitingRowTexts().some((text) => text.includes('Museum tickets'))).toBe(false)
      })
      expect(savedIdsOfTheSignedInPerson()).toEqual(['other'])
      expect(waitingRowTexts().some((text) => text.includes('Taxi home'))).toBe(true)
    })

    it('keeps the change when the sheet is closed without discarding', async () => {
      await renderExpenses({ waiting: [queuedExpense('waiting')] })
      const row = await findWaitingRowWith('Museum tickets')
      fireEvent.click(tapTargetOf(row))

      fireEvent.click(closeActionOfDiscardSheet())

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      expect(savedIdsOfTheSignedInPerson()).toEqual(['waiting'])
      expect(waitingRows()).toHaveLength(1)
    })

    it('does not let a change that is being sent be discarded', async () => {
      const { outboxSender } = await renderExpenses()
      await screen.findByRole('link', { name: /Dinner/ })
      await addSendingItem(outboxSender, queuedExpense('on-its-way'))
      const row = await findWaitingRowWith('Museum tickets')

      fireEvent.click(tapTargetOf(row))

      expect(
        within(discardSheet()).getByRole('button', { name: outboxText('en', 'outbox.sending') }).matches(':disabled'),
      ).toBe(true)
    })
  })

  describe('when the change leaves the outbox by itself', () => {
    it('takes the row off the screen when the sender discards the change', async () => {
      const { outboxSender } = await renderExpenses({ waiting: [queuedExpense('waiting')] })
      await findWaitingRowWith('Museum tickets')

      await act(async () => {
        await outboxSender.discard('waiting')
      })

      await waitFor(() => {
        expect(waitingRows()).toHaveLength(0)
      })
    })
  })
})
