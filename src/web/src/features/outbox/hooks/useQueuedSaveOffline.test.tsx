import { useMutation } from '@tanstack/react-query'
import { act } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { startOutboxTriggers } from '@/core/outbox/outboxTriggers'
import { outboxQueueAfterMilliseconds } from '@/core/outbox/saveOrQueue'
import { createExpense, type ExpenseInput } from '@/core/services/expenses/expensesService'
import { hotelDetail } from '@/test/expenseTestData'
import { expensesPath } from '@/test/expenseTestHelpers'
import { answerOnlyWhileOnline, goOffline, goOnline, postedRequestIds } from '@/test/offlineTestHelpers'
import { queuedExpense } from '@/test/outboxScreenTestData'
import { installFakeTimers, letTimeGoBy, readSavedOutbox } from '@/test/queuedSaveTestHelpers'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { jsonAnswer, stubFetchByRequest } from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { testGroupId } from '@/test/groupTestData'
import { useQueuedSave } from './useQueuedSave'

const firstExpense = queuedExpense('first-request')
const secondExpense = queuedExpense('second-request')
const thirdExpense = queuedExpense('third-request')
let stopTriggers: () => void = () => {}

function useSavingExpenses() {
  const createExpenseMutation = useMutation({
    mutationFn: (input: ExpenseInput) => createExpense(testGroupId, input),
  })
  return { queuedSave: useQueuedSave(), createExpenseMutation }
}

async function renderOffline() {
  const fetchMock = stubFetchByRequest({
    [`POST ${expensesPath}`]: answerOnlyWhileOnline(jsonAnswer(hotelDetail)),
  })
  const rendered = await renderHookWithProviders(useSavingExpenses, { seedCache: seedMe(testMe) })
  goOffline()
  installFakeTimers()
  stopTriggers = startOutboxTriggers(rendered.outboxSender, { document, window })
  return { ...rendered, fetchMock }
}

function browserComesBackOnline(): void {
  goOnline()
  window.dispatchEvent(new Event('online'))
}

function startSaving(
  saving: ReturnType<typeof useSavingExpenses>,
  expense: ReturnType<typeof queuedExpense>,
): void {
  void saving.queuedSave.saveOrQueueItem(expense, () => saving.createExpenseMutation.mutateAsync(expense.payload as ExpenseInput))
}

beforeEach(() => {
  vi.spyOn(console, 'error').mockImplementation(() => {})
})

afterEach(() => {
  stopTriggers()
  stopTriggers = () => {}
  goOnline()
  vi.useRealTimers()
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe('saving through the outbox while the browser is offline', () => {
  it('queues each save at once, long before the 3 seconds are over', async () => {
    const { result } = await renderOffline()

    act(() => startSaving(result.current, firstExpense))
    await letTimeGoBy(50)
    act(() => startSaving(result.current, secondExpense))
    await letTimeGoBy(50)

    expect(readSavedOutbox().map((item) => item.id)).toEqual(['first-request', 'second-request'])
  })

  it('sends each queued save exactly once, oldest first, when the browser is online again', async () => {
    const { result, fetchMock } = await renderOffline()
    act(() => startSaving(result.current, firstExpense))
    await letTimeGoBy(50)
    act(() => startSaving(result.current, secondExpense))
    await letTimeGoBy(50)
    act(() => startSaving(result.current, thirdExpense))
    await letTimeGoBy(outboxQueueAfterMilliseconds + 200)
    const callsBeforeOnline = fetchMock.mock.calls.length

    browserComesBackOnline()
    await letTimeGoBy(60000)

    expect(postedRequestIds(fetchMock, expensesPath, callsBeforeOnline)).toEqual([
      'first-request',
      'second-request',
      'third-request',
    ])
  })

  it('has an empty outbox after the queued saves were sent', async () => {
    const { result, outboxSender } = await renderOffline()
    act(() => startSaving(result.current, firstExpense))
    await letTimeGoBy(50)
    act(() => startSaving(result.current, secondExpense))
    await letTimeGoBy(outboxQueueAfterMilliseconds + 200)

    browserComesBackOnline()
    await letTimeGoBy(60000)

    expect(readSavedOutbox()).toEqual([])
    expect(outboxSender.getSnapshot().items).toEqual([])
  })
})
