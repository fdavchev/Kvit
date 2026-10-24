import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import type { PersonalEntryMonth } from '@/core/services/personalEntries/personalEntriesService'
import { budgetNow, findEntryList, findTotalCard, monthButton, personalEntriesGet, renderBudget } from '@/test/budgetTestHelpers'
import { isDisabled } from '@/test/expenseFormTestHelpers'
import { monthAnswerBody, octoberMonth, octoberWithSalary } from '@/test/personalEntryTestData'
import { findIncomeEditSheet } from '@/test/personalIncomeTestHelpers'
import { blockButton, blocksIn, findRepeatingRow, findRepeatingSheet, startChip } from '@/test/recurringIncomeTestHelpers'
import {
  catchUpAnswer,
  catchUpPost,
  liveRecurringList,
  recurringIncomesGet,
  recurringListAnswerBody,
  salaryRecurringIncome,
} from '@/test/recurringIncomeTestData'
import { networkFailureAnswer, neverAnswer, problemAnswer, requestCount, requestsOf } from '@/test/requestTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const catchUpUrl = '/api/personal/recurring-incomes/catch-up'
const octoberUrl = '/api/personal/entries?month=2026-10'
const listUrl = '/api/personal/recurring-incomes'
const salaryWithEntries = { ...salaryRecurringIncome, hasEntries: true }

function liveOctober(): { answer: () => Response; next: (month: PersonalEntryMonth) => void } {
  let current = octoberMonth
  return {
    answer: () => Response.json(monthAnswerBody(current)),
    next: (month) => {
      current = month
    },
  }
}

async function settle(): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve, 50))
}

async function openSalaryEdit(): Promise<HTMLElement> {
  fireEvent.click(await findRepeatingRow())
  fireEvent.click(blockButton(blocksIn(await findRepeatingSheet())[0], 'expense.edit'))
  return findIncomeEditSheet()
}

describe('BudgetScreen, the catch-up of repeating income', () => {
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

  describe('when it is sent', () => {
    it('sends one POST to the catch-up with no body when the Budget tab opens', async () => {
      const { fetchMock } = await renderBudget()
      await findTotalCard()

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'POST', catchUpUrl)).toHaveLength(1)
      })
      expect(requestsOf(fetchMock, 'POST', catchUpUrl)[0].body).toBeUndefined()
    })

    it('sends it once, not again when another month is picked', async () => {
      const { fetchMock } = await renderBudget({ months: [octoberMonth, { ...octoberMonth, month: '2026-09' }] })
      await findTotalCard()

      fireEvent.click(monthButton('Sep'))
      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', '/api/personal/entries?month=2026-09')).toBe(1)
      })
      fireEvent.click(monthButton('Oct'))
      await settle()

      expect(requestCount(fetchMock, 'POST', catchUpUrl)).toBe(1)
    })

    it('sends it once for one opening of the tab even when React starts the screen twice', async () => {
      const { fetchMock } = await renderBudget({ isStrictMode: true })
      await findTotalCard()
      await settle()

      expect(requestCount(fetchMock, 'POST', catchUpUrl)).toBe(1)
    })

    it('does not make the month wait for the catch-up', async () => {
      await renderBudget({ answers: { [catchUpPost]: neverAnswer() } })

      expect(await findTotalCard()).toBeTruthy()
      expect(await findEntryList()).toBeTruthy()
      expect(screen.queryByRole('status')).toBeNull()
    })

    it('does not make the repeating income row wait for the catch-up', async () => {
      await renderBudget({ recurringIncomes: [salaryRecurringIncome], answers: { [catchUpPost]: neverAnswer() } })

      expect(within(await findRepeatingRow()).getByText('Repeating income · 1')).toBeTruthy()
    })
  })

  describe('what it does to the screen', () => {
    it('asks for the month again and shows the new entries when it made some', async () => {
      const october = liveOctober()
      const { fetchMock } = await renderBudget({
        answers: {
          [personalEntriesGet('2026-10')]: october.answer,
          [catchUpPost]: async () => {
            await settle()
            october.next(octoberWithSalary)
            return Response.json({ created: 2 })
          },
        },
      })

      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', octoberUrl)).toBe(2)
      })
      await waitFor(() => {
        expect(within(screen.getByRole('list', { name: translated('en', 'budget.screen.entries') })).getAllByRole('listitem')).toHaveLength(3)
      })
    })

    it('shows the new entries even when the catch-up is done while the first answer for the month is still on its way', async () => {
      let current = octoberMonth
      const { fetchMock } = await renderBudget({
        answers: {
          [personalEntriesGet('2026-10')]: async () => {
            const body = monthAnswerBody(current)
            await new Promise((resolve) => setTimeout(resolve, 150))
            return Response.json(body)
          },
          [catchUpPost]: () => {
            current = octoberWithSalary
            return Response.json({ created: 1 })
          },
        },
      })

      await waitFor(() => {
        expect(within(screen.getByRole('list', { name: translated('en', 'budget.screen.entries') })).getAllByRole('listitem')).toHaveLength(3)
      })
      expect(requestCount(fetchMock, 'GET', octoberUrl)).toBeGreaterThanOrEqual(2)
    })

    it('asks for the month and the repeating income list only once when it made nothing', async () => {
      const { fetchMock } = await renderBudget({ recurringIncomes: [salaryRecurringIncome], answers: { [catchUpPost]: catchUpAnswer(0) } })
      await findTotalCard()
      await waitFor(() => {
        expect(requestsOf(fetchMock, 'POST', catchUpUrl)).toHaveLength(1)
      })
      await settle()

      expect(requestCount(fetchMock, 'GET', octoberUrl)).toBe(1)
      expect(requestCount(fetchMock, 'GET', listUrl)).toBe(1)
    })

    it('asks for the repeating income list again when it made entries, so the start date of that income is locked', async () => {
      const live = liveRecurringList([salaryRecurringIncome])
      const { fetchMock } = await renderBudget({
        answers: {
          [recurringIncomesGet]: live.answer,
          [catchUpPost]: async () => {
            await settle()
            live.set([salaryWithEntries])
            return Response.json({ created: 2 })
          },
        },
      })

      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', listUrl)).toBe(2)
      })
      const editSheet = await openSalaryEdit()

      expect(isDisabled(startChip(editSheet, '1 Aug'))).toBe(true)
    })

    it('locks the start date even when the catch-up is done while the first answer for the list is still on its way', async () => {
      let current = [salaryRecurringIncome]
      const { fetchMock } = await renderBudget({
        answers: {
          [recurringIncomesGet]: async () => {
            const body = recurringListAnswerBody(current)
            await new Promise((resolve) => setTimeout(resolve, 150))
            return Response.json(body)
          },
          [catchUpPost]: () => {
            current = [salaryWithEntries]
            return Response.json({ created: 1 })
          },
        },
      })

      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', listUrl)).toBeGreaterThanOrEqual(2)
      })
      const editSheet = await openSalaryEdit()

      expect(isDisabled(startChip(editSheet, '1 Aug'))).toBe(true)
    })

    it('shows no message of its own when it made entries', async () => {
      await renderBudget({ answers: { [catchUpPost]: catchUpAnswer(3) } })
      await findTotalCard()
      await settle()

      expect(vi.mocked(toast)).not.toHaveBeenCalled()
      expect(vi.mocked(toast.success)).not.toHaveBeenCalled()
      expect(vi.mocked(toast.error)).not.toHaveBeenCalled()
    })
  })

  describe('when it fails', () => {
    it.each([
      ['the server cannot be reached', networkFailureAnswer()],
      ['the server refuses it', problemAnswer(403, 'GUEST_NOT_ALLOWED')],
      ['the server breaks', problemAnswer(500, 'SOMETHING_UNKNOWN')],
    ])('keeps the month, the entries and the row working when %s', async (_name, failure) => {
      vi.spyOn(console, 'error').mockImplementation(() => undefined)
      await renderBudget({ recurringIncomes: [salaryRecurringIncome], answers: { [catchUpPost]: failure } })

      expect(await findTotalCard()).toBeTruthy()
      expect(await findEntryList()).toBeTruthy()
      expect(within(await findRepeatingRow()).getByText('Repeating income · 1')).toBeTruthy()
      expect(screen.queryByRole('alert')).toBeNull()
    })

    it.each(languages)('logs the failure with console.error and says so in an error toast (%s)', async (language) => {
      const logged = vi.spyOn(console, 'error').mockImplementation(() => undefined)
      await renderBudget({ language, answers: { [catchUpPost]: networkFailureAnswer() } })
      await findTotalCard(language)

      await waitFor(() => {
        expect(vi.mocked(toast.error)).toHaveBeenCalledWith(translated(language, 'budget.recurring.catchUpFailed'))
      })
      expect(logged).toHaveBeenCalled()
      expect(vi.mocked(toast)).not.toHaveBeenCalled()
    })

    it('shows the error toast once for one opening of the tab, even when React starts the screen twice', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => undefined)
      await renderBudget({ isStrictMode: true, answers: { [catchUpPost]: networkFailureAnswer() } })
      await findTotalCard()
      await waitFor(() => {
        expect(vi.mocked(toast.error)).toHaveBeenCalled()
      })
      await settle()

      expect(vi.mocked(toast.error)).toHaveBeenCalledOnce()
    })

    it('does not ask for the month again after a failure', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => undefined)
      const { fetchMock } = await renderBudget({ answers: { [catchUpPost]: networkFailureAnswer() } })
      await findTotalCard()
      await settle()

      expect(requestCount(fetchMock, 'GET', octoberUrl)).toBe(1)
    })
  })
})
