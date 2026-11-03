import { fireEvent, screen } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { routes } from '@/core/router/routes'
import { renderReadyAddExpense } from '@/test/addExpenseScreenTestHelpers'
import { hotelDetail, testNow } from '@/test/expenseTestData'
import { saveButton, sentBody, typeAmount } from '@/test/expenseFormTestHelpers'
import { expensesPath, freezeTime, unfreezeTime } from '@/test/expenseTestHelpers'
import { testGroupId } from '@/test/groupTestData'
import { answerOnlyWhileOnline, goOffline, goOnline, postCount } from '@/test/offlineTestHelpers'
import { installFakeTimers, letTimeGoBy, readSavedOutbox } from '@/test/queuedSaveTestHelpers'
import { jsonAnswer } from '@/test/requestTestHelpers'
import { shownToastTexts } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

describe('AddExpenseScreen saving while the browser is offline', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    freezeTime()
  })

  afterEach(() => {
    goOnline()
    unfreezeTime()
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('queues the expense at once, long before the 3 seconds are over', async () => {
    const { router, run } = await renderReadyAddExpense({ postAnswer: answerOnlyWhileOnline(jsonAnswer(hotelDetail)) })
    typeAmount('1200')
    goOffline()
    installFakeTimers(testNow)

    fireEvent.click(saveButton())
    await letTimeGoBy(100)

    expect(readSavedOutbox()).toHaveLength(1)
    expect(router.state.location.pathname).toBe(routes.group(testGroupId))
    expect(router.state.historyAction).toBe('REPLACE')
    expect(run).toHaveBeenCalledTimes(1)
  })

  it('shows no Expense saved toast and no error while it queues the expense', async () => {
    const { router } = await renderReadyAddExpense({ postAnswer: answerOnlyWhileOnline(jsonAnswer(hotelDetail)) })
    typeAmount('1200')
    goOffline()
    installFakeTimers(testNow)

    fireEvent.click(saveButton())
    await letTimeGoBy(100)

    expect(router.state.location.pathname).toBe(routes.group(testGroupId))
    expect(shownToastTexts().includes(translated('en', 'expenses.saved'))).toBe(false)
    expect(vi.mocked(toast.error)).not.toHaveBeenCalled()
    expect(screen.queryByRole('alert')).toBeNull()
  })

  it('stores the expense with the request id of the one request the screen made', async () => {
    const { fetchMock } = await renderReadyAddExpense({ postAnswer: answerOnlyWhileOnline(jsonAnswer(hotelDetail)) })
    typeAmount('1200')
    goOffline()
    installFakeTimers(testNow)

    fireEvent.click(saveButton())
    await letTimeGoBy(100)

    expect(postCount(fetchMock, expensesPath)).toBe(1)
    expect(readSavedOutbox()[0].id).toBe(sentBody(fetchMock, 'POST', expensesPath).clientRequestId)
  })
})
