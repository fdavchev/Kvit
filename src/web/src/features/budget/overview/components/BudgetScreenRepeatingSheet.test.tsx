import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import type { RecurringIncome } from '@/core/services/recurringIncomes/recurringIncomesService'
import { kvitGlassPanelLook } from '@/shared/components/kvitGlassLooks'
import { budgetNow, renderBudget } from '@/test/budgetTestHelpers'
import { amountField, dismissSheetByTappingBackdrop, saveButton, sentBody, typeAmount } from '@/test/expenseFormTestHelpers'
import { plainSpaces } from '@/test/expenseTestHelpers'
import { findIncomeEditSheet } from '@/test/personalIncomeTestHelpers'
import {
  blockButton,
  blocksIn,
  findRepeatingRow,
  findRepeatingSheet,
  sentWritesToRecurringIncomes,
  signedMoneyText,
  weekdayName,
} from '@/test/recurringIncomeTestHelpers'
import {
  catchUpAnswer,
  catchUpPost,
  flatRentRecurringIncome,
  liveRecurringList,
  recurringIncomeAnswerRow,
  recurringIncomePut,
  recurringIncomeRestartPost,
  recurringIncomeStopPost,
  recurringIncomeUrl,
  recurringIncomesGet,
  salaryRecurringIncome,
  tutoringRecurringIncome,
  untitledRecurringIncome,
} from '@/test/recurringIncomeTestData'
import { jsonAnswer, problemAnswer, requestCount, requestsOf, type AnswerFactory } from '@/test/requestTestHelpers'
import { pressToastAction, toastShownWithText } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const salary = salaryRecurringIncome
const flatRent = flatRentRecurringIncome
const listUrl = '/api/personal/recurring-incomes'

async function openSheetWith(incomes: RecurringIncome[], language: 'en' | 'mk' = 'en', answers: Record<string, AnswerFactory> = {}) {
  const rendered = await renderBudget({ language, recurringIncomes: incomes, answers })
  fireEvent.click(await findRepeatingRow(language))
  const sheet = await findRepeatingSheet(language)
  return { ...rendered, sheet }
}

function liveStopAnswers() {
  const live = liveRecurringList([salary, flatRent])
  return {
    live,
    answers: {
      [recurringIncomesGet]: live.answer,
      [recurringIncomeStopPost(salary.id)]: () => {
        live.set([flatRent])
        return new Response(null, { status: 204 })
      },
      [recurringIncomeRestartPost(salary.id)]: () => {
        live.set([salary, flatRent])
        return new Response(null, { status: 204 })
      },
      [catchUpPost]: catchUpAnswer(1),
    },
  }
}

describe('BudgetScreen, the "Repeating income" sheet', () => {
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

  describe('what it shows', () => {
    it.each(languages)('is a glass sheet titled "Repeating income" with one block for each income (%s)', async (language) => {
      const { sheet } = await openSheetWith([salary, flatRent], language)

      expect(sheet.className.split(/\s+/)).toEqual(expect.arrayContaining(kvitGlassPanelLook.split(/\s+/)))
      expect(within(sheet).getByRole('heading', { name: translated(language, 'budget.recurring.title') })).toBeTruthy()
      expect(blocksIn(sheet, language)).toHaveLength(2)
    })

    it('shows the name, the amount with a plus in green, the rhythm and the next date of a monthly income', async () => {
      const { sheet } = await openSheetWith([salary, flatRent])

      const [block] = blocksIn(sheet)

      expect(within(block).getByText('Salary')).toBeTruthy()
      const amount = within(block).getByText(signedMoneyText(3000000, 'MKD'), { normalizer: plainSpaces })
      expect(amount.classList.contains('text-positive')).toBe(true)
      expect(within(block).getByText('Every month on the 1st')).toBeTruthy()
      expect(within(block).getByText('next 1 Nov')).toBeTruthy()
    })

    it('shows the second income in euros with its own rhythm and date', async () => {
      const { sheet } = await openSheetWith([salary, flatRent])

      const block = blocksIn(sheet)[1]

      expect(within(block).getByText('Flat rent')).toBeTruthy()
      expect(within(block).getByText(signedMoneyText(25000, 'EUR'), { normalizer: plainSpaces })).toBeTruthy()
      expect(within(block).getByText('Every month on the 5th')).toBeTruthy()
      expect(within(block).getByText('next 5 Nov')).toBeTruthy()
    })

    it('shows the weekday of a weekly income and its next date', async () => {
      const { sheet } = await openSheetWith([tutoringRecurringIncome, salary])

      const block = blocksIn(sheet)[0]

      expect(within(block).getByText('Every week on Tuesday')).toBeTruthy()
      expect(within(block).getByText('next 20 Oct')).toBeTruthy()
    })

    it('writes the blocks in the order of the server, newest first', async () => {
      const { sheet } = await openSheetWith([flatRent, salary])

      const names = blocksIn(sheet).map((block) => ['Flat rent', 'Salary'].find((name) => within(block).queryByText(name) !== null))

      expect(names).toEqual(['Flat rent', 'Salary'])
    })

    it('says "Income" for an income with no name, and leaves out "next" when there is no next date', async () => {
      const { sheet } = await openSheetWith([untitledRecurringIncome, salary])

      const block = blocksIn(sheet)[0]

      expect(within(block).getByText(translated('en', 'budget.income.listTitle'))).toBeTruthy()
      expect(within(block).getByText('Every month on the 15th')).toBeTruthy()
      expect(within(block).queryByText(/^next/)).toBeNull()
    })

    it('shows the year of a next date in another year', async () => {
      const { sheet } = await openSheetWith([{ ...salary, nextDate: '2027-01-01' }, flatRent])

      expect(within(blocksIn(sheet)[0]).getByText('next 1 Jan 2027')).toBeTruthy()
    })

    it.each(languages)('writes the amounts, the rhythm and the next date in %s', async (language) => {
      const { sheet } = await openSheetWith([tutoringRecurringIncome, flatRent], language)

      const [weekly, monthly] = blocksIn(sheet, language)

      expect(within(weekly).getByText(signedMoneyText(150000, 'MKD', language), { normalizer: plainSpaces })).toBeTruthy()
      expect(
        within(weekly).getByText(translated(language, 'budget.recurring.weekly', { weekday: weekdayName(2, language) })),
      ).toBeTruthy()
      expect(
        within(weekly).getByText(translated(language, 'budget.recurring.next', { day: language === 'en' ? '20 Oct' : '20 окт' })),
      ).toBeTruthy()
      expect(within(monthly).getByText(signedMoneyText(25000, 'EUR', language), { normalizer: plainSpaces })).toBeTruthy()
    })

    it('gives Edit and Stop in every block, 44 px tall, with Stop in the danger colour', async () => {
      const { sheet } = await openSheetWith([salary, flatRent])

      for (const block of blocksIn(sheet)) {
        expect(blockButton(block, 'expense.edit').className.split(/\s+/)).toEqual(
          expect.arrayContaining([expect.stringMatching(/^min-h-(1[1-9]|20)$/)]),
        )
        expect(blockButton(block, 'budget.recurring.stop').className.split(/\s+/)).toEqual(
          expect.arrayContaining(['text-danger', expect.stringMatching(/^min-h-(1[1-9]|20)$/)]),
        )
      }
    })

    it('sends no request of its own when it opens: only the catch-up and the one list request of the Budget tab', async () => {
      const { fetchMock } = await openSheetWith([salary, flatRent])

      expect(sentWritesToRecurringIncomes(fetchMock)).toEqual([`POST ${listUrl}/catch-up`])
      expect(requestCount(fetchMock, 'GET', listUrl)).toBe(1)
    })
  })

  describe('closing', () => {
    it.each([
      ['Escape', () => fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' })],
      ['a tap on the backdrop', () => dismissSheetByTappingBackdrop()],
    ])('closes with %s and puts the focus back on the row', async (_name, cancel) => {
      await renderBudget({ recurringIncomes: [salary, flatRent] })
      const row = await findRepeatingRow()
      row.focus()
      fireEvent.click(row)
      await findRepeatingSheet()

      cancel()

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      await waitFor(() => {
        expect(document.activeElement).toBe(row)
      })
    })
  })

  describe('Edit', () => {
    const saveAnswers = {
      [recurringIncomePut(salary.id)]: jsonAnswer(recurringIncomeAnswerRow(salary)),
    }

    it('opens the income sheet filled in with that income, in 2 taps from the row, and the list sheet goes away', async () => {
      const { sheet } = await openSheetWith([salary, flatRent], 'en', saveAnswers)
      let taps = 1

      fireEvent.click(blockButton(blocksIn(sheet)[0], 'expense.edit'))
      taps += 1

      const incomeSheet = await findIncomeEditSheet()
      expect(taps).toBe(2)
      expect(amountField().value).toBe('30000')
      expect(within(incomeSheet).getByLabelText<HTMLTextAreaElement>(translated('en', 'expense.note')).value).toBe('Salary')
      await waitFor(() => {
        expect(screen.queryByRole('dialog', { name: translated('en', 'budget.recurring.title') })).toBeNull()
      })
    })

    it('puts the focus inside the edit sheet after the list sheet has gone, so Tab stays in it', async () => {
      const { sheet } = await openSheetWith([salary, flatRent])
      const edit = blockButton(blocksIn(sheet)[0], 'expense.edit')

      edit.focus()
      fireEvent.click(edit)

      const editSheet = await findIncomeEditSheet()
      await waitFor(() => {
        expect(screen.queryByRole('dialog', { name: translated('en', 'budget.recurring.title') })).toBeNull()
      })
      await new Promise((resolve) => setTimeout(resolve, 100))
      expect(document.activeElement).toBe(amountField())
      expect(editSheet.contains(document.activeElement)).toBe(true)
    })

    it('puts the focus back on the Repeating income row when the edit sheet is closed with Escape', async () => {
      await renderBudget({ recurringIncomes: [salary, flatRent] })
      const row = await findRepeatingRow()
      row.focus()
      fireEvent.click(row)
      const edit = blockButton(blocksIn(await findRepeatingSheet())[0], 'expense.edit')
      edit.focus()
      fireEvent.click(edit)
      await findIncomeEditSheet()
      await waitFor(() => {
        expect(screen.queryByRole('dialog', { name: translated('en', 'budget.recurring.title') })).toBeNull()
      })

      fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' })

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      await new Promise((resolve) => setTimeout(resolve, 50))
      expect(document.activeElement).toBe(row)
    })

    it('opens the income that belongs to the block that was tapped', async () => {
      const { sheet } = await openSheetWith([salary, flatRent])

      fireEvent.click(blockButton(blocksIn(sheet)[1], 'expense.edit'))

      await findIncomeEditSheet()
      expect(amountField().value).toBe('250')
      expect(within(screen.getByRole('dialog')).getByLabelText<HTMLTextAreaElement>(translated('en', 'expense.note')).value).toBe('Flat rent')
    })

    it('saves a change with PUT then the catch-up, and the list and the month are asked for again', async () => {
      const { sheet, fetchMock } = await openSheetWith([salary, flatRent], 'en', saveAnswers)
      fireEvent.click(blockButton(blocksIn(sheet)[0], 'expense.edit'))
      await findIncomeEditSheet()

      typeAmount('32000')
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', recurringIncomeUrl(salary.id))).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'PUT', recurringIncomeUrl(salary.id))).toMatchObject({ amountMinor: 3200000, frequency: 'Monthly' })
      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', listUrl)).toBe(2)
      })
      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', '/api/personal/entries?month=2026-10')).toBe(2)
      })
      expect(sentWritesToRecurringIncomes(fetchMock)).toEqual([`POST ${listUrl}/catch-up`, `PUT ${recurringIncomeUrl(salary.id)}`, `POST ${listUrl}/catch-up`])
    })
  })

  describe('Stop', () => {
    it('stops that income with one tap, with no income sheet and no question, and the block goes away', async () => {
      const { answers } = liveStopAnswers()
      const { sheet, fetchMock } = await openSheetWith([salary, flatRent], 'en', answers)

      fireEvent.click(blockButton(blocksIn(sheet)[0], 'budget.recurring.stop'))

      await waitFor(() => {
        expect(blocksIn(sheet)).toHaveLength(1)
      })
      expect(within(blocksIn(sheet)[0]).getByText('Flat rent')).toBeTruthy()
      expect(requestCount(fetchMock, 'POST', `${recurringIncomeUrl(salary.id)}/stop`)).toBe(1)
      expect(screen.getAllByRole('dialog')).toHaveLength(1)
      expect(screen.queryByRole('dialog', { name: translated('en', 'budget.income.editTitle') })).toBeNull()
    })

    it.each(languages)('shows a "Stopped" toast with Undo (%s)', async (language) => {
      const { answers } = liveStopAnswers()
      const { sheet } = await openSheetWith([salary, flatRent], language, answers)

      fireEvent.click(blockButton(blocksIn(sheet, language)[0], 'budget.recurring.stop', language))

      await waitFor(() => {
        expect(toastShownWithText(translated(language, 'budget.recurring.stopped'))).toBeTruthy()
      })
      expect(toastShownWithText(translated(language, 'budget.recurring.stopped')).options.action).toMatchObject({
        label: translated(language, 'common.undo'),
      })
    })

    it('brings the income back with Undo: restart, then the catch-up, then the block is back, and says Restored', async () => {
      const { answers } = liveStopAnswers()
      const { sheet, fetchMock } = await openSheetWith([salary, flatRent], 'en', answers)
      fireEvent.click(blockButton(blocksIn(sheet)[0], 'budget.recurring.stop'))
      await waitFor(() => {
        expect(blocksIn(sheet)).toHaveLength(1)
      })

      pressToastAction(toastShownWithText(translated('en', 'budget.recurring.stopped')))

      await waitFor(() => {
        expect(blocksIn(sheet)).toHaveLength(2)
      })
      expect(vi.mocked(toast.success)).toHaveBeenCalledWith(translated('en', 'budget.entries.restored'))
      expect(sentWritesToRecurringIncomes(fetchMock)).toEqual([
        `POST ${listUrl}/catch-up`,
        `POST ${recurringIncomeUrl(salary.id)}/stop`,
        `POST ${recurringIncomeUrl(salary.id)}/restart`,
        `POST ${listUrl}/catch-up`,
      ])
    })

    it('shows the translated error when Undo comes too late', async () => {
      const { answers } = liveStopAnswers()
      const { sheet } = await openSheetWith([salary, flatRent], 'en', {
        ...answers,
        [recurringIncomeRestartPost(salary.id)]: problemAnswer(400, 'RECURRING_INCOME_RESTART_EXPIRED'),
      })
      fireEvent.click(blockButton(blocksIn(sheet)[0], 'budget.recurring.stop'))
      await waitFor(() => {
        expect(toastShownWithText(translated('en', 'budget.recurring.stopped'))).toBeTruthy()
      })

      pressToastAction(toastShownWithText(translated('en', 'budget.recurring.stopped')))

      await waitFor(() => {
        expect(vi.mocked(toast.error)).toHaveBeenCalledWith(translated('en', 'errors.RECURRING_INCOME_RESTART_EXPIRED'))
      })
    })

    it('sends one stop when Stop is tapped twice quickly', async () => {
      const { answers } = liveStopAnswers()
      const { sheet, fetchMock } = await openSheetWith([salary, flatRent], 'en', answers)
      const stop = blockButton(blocksIn(sheet)[0], 'budget.recurring.stop')

      fireEvent.click(stop)
      fireEvent.click(stop)

      await waitFor(() => {
        expect(blocksIn(sheet)).toHaveLength(1)
      })
      expect(requestCount(fetchMock, 'POST', `${recurringIncomeUrl(salary.id)}/stop`)).toBe(1)
    })

    it('shows the error as a toast and asks for the list again when the income no longer exists', async () => {
      const { answers } = liveStopAnswers()
      const { sheet, fetchMock } = await openSheetWith([salary, flatRent], 'en', {
        ...answers,
        [recurringIncomeStopPost(salary.id)]: problemAnswer(404, 'RECURRING_INCOME_NOT_FOUND'),
      })
      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', listUrl)).toBe(2)
      })

      fireEvent.click(blockButton(blocksIn(sheet)[0], 'budget.recurring.stop'))

      await waitFor(() => {
        expect(vi.mocked(toast.error)).toHaveBeenCalledWith(translated('en', 'errors.RECURRING_INCOME_NOT_FOUND'))
      })
      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', listUrl)).toBe(3)
      })
    })

    it('shows the error as a toast and keeps both blocks when the stop fails for another reason', async () => {
      const { answers } = liveStopAnswers()
      const { sheet } = await openSheetWith([salary, flatRent], 'en', {
        ...answers,
        [recurringIncomeStopPost(salary.id)]: problemAnswer(403, 'GUEST_NOT_ALLOWED'),
      })

      fireEvent.click(blockButton(blocksIn(sheet)[0], 'budget.recurring.stop'))

      await waitFor(() => {
        expect(vi.mocked(toast.error)).toHaveBeenCalledWith(translated('en', 'errors.GUEST_NOT_ALLOWED'))
      })
      expect(blocksIn(sheet)).toHaveLength(2)
    })

    it('closes the sheet when the last income is stopped, still shows "Stopped" with Undo, and the row says none yet', async () => {
      const live = liveRecurringList([salary])
      const { sheet } = await openSheetWith([salary], 'en', {
        [recurringIncomesGet]: live.answer,
        [recurringIncomeStopPost(salary.id)]: () => {
          live.set([])
          return new Response(null, { status: 204 })
        },
      })

      fireEvent.click(blockButton(blocksIn(sheet)[0], 'budget.recurring.stop'))

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      expect(toastShownWithText(translated('en', 'budget.recurring.stopped')).options.action).toMatchObject({
        label: translated('en', 'common.undo'),
      })
      await waitFor(() => {
        expect(within(screen.getByRole('button', { name: /^Repeating income/ })).getByText(translated('en', 'budget.recurring.rowNone'))).toBeTruthy()
      })
    })

    it('keeps the sheet open when one of two incomes is stopped', async () => {
      const { answers } = liveStopAnswers()
      const { sheet } = await openSheetWith([salary, flatRent], 'en', answers)

      fireEvent.click(blockButton(blocksIn(sheet)[0], 'budget.recurring.stop'))

      await waitFor(() => {
        expect(blocksIn(sheet)).toHaveLength(1)
      })
      await new Promise((resolve) => setTimeout(resolve, 100))
      expect(screen.getByRole('dialog', { name: translated('en', 'budget.recurring.title') })).toBe(sheet)
    })

    it('updates the row under the sheet after a stop', async () => {
      const { answers } = liveStopAnswers()
      const { sheet } = await openSheetWith([salary, flatRent], 'en', answers)
      fireEvent.click(blockButton(blocksIn(sheet)[0], 'budget.recurring.stop'))
      await waitFor(() => {
        expect(blocksIn(sheet)).toHaveLength(1)
      })

      fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' })

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      expect(within(await findRepeatingRow()).getByText('Repeating income · 1')).toBeTruthy()
    })
  })
})
