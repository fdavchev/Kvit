import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { budgetNow, findTotalCard, plusButton, renderBudget } from '@/test/budgetTestHelpers'
import { saveButton, sentBody, typeAmount } from '@/test/expenseFormTestHelpers'
import { entryAnswerRow, salaryIncomeEntry } from '@/test/personalEntryTestData'
import { personalEntriesPost, waitForEveryDialogToClose } from '@/test/personalEntryTestHelpers'
import { chooseIncome } from '@/test/personalIncomeTestHelpers'
import {
  chooseEnds,
  findRepeatingRow,
  repeatingRowName,
  repeatsChipIn,
  sentWritesToRecurringIncomes,
  tapRepeats,
} from '@/test/recurringIncomeTestHelpers'
import {
  catchUpAnswer,
  catchUpPost,
  liveRecurringList,
  recurringIncomeAnswerRow,
  recurringIncomesGet,
  recurringIncomesPost,
  salaryRecurringIncome,
} from '@/test/recurringIncomeTestData'
import { jsonAnswer, requestCount, requestsOf } from '@/test/requestTestHelpers'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const createUrl = '/api/personal/recurring-incomes'

function liveCreateAnswers() {
  const live = liveRecurringList([])
  return {
    [recurringIncomesGet]: live.answer,
    [recurringIncomesPost]: () => {
      live.set([salaryRecurringIncome])
      return Response.json(recurringIncomeAnswerRow(salaryRecurringIncome))
    },
    [catchUpPost]: catchUpAnswer(0),
  }
}

describe('BudgetScreen, tap counts of repeating income, counted from the open "What do you add?" sheet', () => {
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

  it('adds a monthly repeating income in 3 taps and 1 field: Income, Repeats, Save', async () => {
    const { fetchMock } = await renderBudget({ answers: liveCreateAnswers() })
    await findTotalCard()
    fireEvent.click(plusButton())
    let taps = 0

    const sheet = await chooseIncome()
    taps += 1
    tapRepeats(sheet, 1)
    taps += 1
    typeAmount('30000')
    fireEvent.click(saveButton())
    taps += 1

    await waitFor(() => {
      expect(requestsOf(fetchMock, 'POST', createUrl)).toHaveLength(1)
    })
    expect(taps).toBe(3)
    expect(sentBody(fetchMock, 'POST', createUrl)).toMatchObject({ frequency: 'Monthly', amountMinor: 3000000, endsOn: null })
    expect(requestCount(fetchMock, 'POST', '/api/personal/entries')).toBe(0)
  })

  it('adds a weekly repeating income in 4 taps and 1 field: Income, Repeats, Repeats, Save', async () => {
    const { fetchMock } = await renderBudget({ answers: liveCreateAnswers() })
    await findTotalCard()
    fireEvent.click(plusButton())
    let taps = 0

    const sheet = await chooseIncome()
    taps += 1
    tapRepeats(sheet, 2)
    taps += 2
    typeAmount('1500')
    fireEvent.click(saveButton())
    taps += 1

    await waitFor(() => {
      expect(requestsOf(fetchMock, 'POST', createUrl)).toHaveLength(1)
    })
    expect(taps).toBe(4)
    expect(sentBody(fetchMock, 'POST', createUrl)).toMatchObject({ frequency: 'Weekly', dayOfMonth: null, dayOfWeek: 1 })
  })

  it('adds a monthly repeating income that ends in 6 months in 5 taps and 1 field: Income, Repeats, Ends, In 6 months, Save', async () => {
    const { fetchMock } = await renderBudget({ answers: liveCreateAnswers() })
    await findTotalCard()
    fireEvent.click(plusButton())
    let taps = 0

    const sheet = await chooseIncome()
    taps += 1
    tapRepeats(sheet, 1)
    taps += 1
    await chooseEnds(sheet, 'endsIn6Months')
    taps += 2
    typeAmount('30000')
    fireEvent.click(saveButton())
    taps += 1

    await waitFor(() => {
      expect(requestsOf(fetchMock, 'POST', createUrl)).toHaveLength(1)
    })
    expect(taps).toBe(5)
    expect(sentBody(fetchMock, 'POST', createUrl)).toMatchObject({ frequency: 'Monthly', endsOn: '2027-04-19' })
  })

  it('still adds income once in 2 taps and 1 field, Income and Save, with the chip left on Never and no repeating call', async () => {
    const { fetchMock } = await renderBudget({
      answers: { ...liveCreateAnswers(), [personalEntriesPost]: jsonAnswer(entryAnswerRow(salaryIncomeEntry)) },
    })
    await findTotalCard()
    fireEvent.click(plusButton())
    let taps = 0

    const sheet = await chooseIncome()
    taps += 1
    expect(repeatsChipIn(sheet, 'Never')).toBeTruthy()
    typeAmount('65000')
    fireEvent.click(saveButton())
    taps += 1

    await waitFor(() => {
      expect(requestsOf(fetchMock, 'POST', '/api/personal/entries')).toHaveLength(1)
    })
    expect(taps).toBe(2)
    expect(requestCount(fetchMock, 'POST', createUrl)).toBe(0)
  })

  it('shows the new repeating income in the row and asks for the month again after the save', async () => {
    const { fetchMock } = await renderBudget({ answers: liveCreateAnswers() })
    await findTotalCard()
    expect(within(await findRepeatingRow()).getByText('Repeating income · none yet')).toBeTruthy()
    fireEvent.click(plusButton())
    const sheet = await chooseIncome()

    tapRepeats(sheet, 1)
    typeAmount('30000')
    fireEvent.click(saveButton())
    await waitForEveryDialogToClose()

    await waitFor(() => {
      expect(within(screen.getByRole('button', { name: repeatingRowName() })).getByText('Repeating income · 1')).toBeTruthy()
    })
    expect(within(await findRepeatingRow()).getByText('Salary')).toBeTruthy()
    expect(requestCount(fetchMock, 'GET', '/api/personal/entries?month=2026-10')).toBe(2)
    expect(sentWritesToRecurringIncomes(fetchMock)).toEqual([`POST ${createUrl}/catch-up`, `POST ${createUrl}`, `POST ${createUrl}/catch-up`])
  })
})
