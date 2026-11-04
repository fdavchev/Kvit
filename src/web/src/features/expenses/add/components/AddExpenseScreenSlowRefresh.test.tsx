import { fireEvent, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { outboxQueueAfterMilliseconds } from '@/core/outbox/saveOrQueue'
import { routes } from '@/core/router/routes'
import { answerFirstRequestAtOnceAndHoldTheRest, renderReadyAddExpense } from '@/test/addExpenseScreenTestHelpers'
import { testNow } from '@/test/expenseTestData'
import { isDisabled, saveButton, typeAmount } from '@/test/expenseFormTestHelpers'
import { expensesPath, freezeTime, groupPath, unfreezeTime } from '@/test/expenseTestHelpers'
import { groupOf, testGroupId } from '@/test/groupTestData'
import { postCount } from '@/test/offlineTestHelpers'
import { installFakeTimers, letTimeGoBy, readSavedOutbox } from '@/test/queuedSaveTestHelpers'
import { neverAnswer, requestCount } from '@/test/requestTestHelpers'
import { shownToastTexts } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const slowRefreshMilliseconds = outboxQueueAfterMilliseconds + 1500

function savedToastShown(): boolean {
  return shownToastTexts().includes(translated('en', 'expenses.saved'))
}

describe('AddExpenseScreen when the server answered the save but the refresh after it is slow', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    freezeTime()
  })

  afterEach(() => {
    unfreezeTime()
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  async function saveWithSlowRefresh() {
    const group = answerFirstRequestAtOnceAndHoldTheRest(groupOf({ memberCount: 5 }))
    const rendered = await renderReadyAddExpense({ groupAnswer: group.answer })
    typeAmount('1200')
    installFakeTimers(testNow)
    fireEvent.click(saveButton())
    return { ...rendered, group }
  }

  it('does not queue the expense although more than 3 seconds pass before the refresh is done', async () => {
    const { run } = await saveWithSlowRefresh()

    await letTimeGoBy(slowRefreshMilliseconds)

    expect(readSavedOutbox()).toEqual([])
    expect(run).not.toHaveBeenCalled()
  })

  it('stays on the screen with Save disabled while the refresh is still running', async () => {
    const { router } = await saveWithSlowRefresh()

    await letTimeGoBy(slowRefreshMilliseconds)

    expect(router.state.location.pathname).toBe(routes.groupExpenseNew(testGroupId))
    expect(isDisabled(saveButton())).toBe(true)
  })

  it('shows the Expense saved toast and goes to the group, replacing the history entry, once the refresh is done', async () => {
    const { router, group } = await saveWithSlowRefresh()
    await letTimeGoBy(slowRefreshMilliseconds)

    group.release(groupOf({ memberCount: 5 }))
    await letTimeGoBy(0)

    expect(router.state.location.pathname).toBe(routes.group(testGroupId))
    expect(router.state.historyAction).toBe('REPLACE')
    expect(savedToastShown()).toBe(true)
    expect(screen.getByText('group page')).toBeTruthy()
  })

  it('queues nothing and sends nothing a second time, also after the refresh is done', async () => {
    const { fetchMock, group, run } = await saveWithSlowRefresh()
    await letTimeGoBy(slowRefreshMilliseconds)

    group.release(groupOf({ memberCount: 5 }))
    await letTimeGoBy(60000)

    expect(readSavedOutbox()).toEqual([])
    expect(run).not.toHaveBeenCalled()
    expect(postCount(fetchMock, expensesPath)).toBe(1)
  })

  it('asks the server for the group again only once, for the refresh', async () => {
    const { fetchMock } = await saveWithSlowRefresh()

    await letTimeGoBy(slowRefreshMilliseconds)

    expect(requestCount(fetchMock, 'GET', groupPath)).toBe(2)
  })
})

describe('AddExpenseScreen when the server does not answer the save while the refresh would be fast', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    freezeTime()
  })

  afterEach(() => {
    unfreezeTime()
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('still waits the full 3 seconds and then queues the expense', async () => {
    const { router, run } = await renderReadyAddExpense({ postAnswer: neverAnswer() })
    typeAmount('1200')
    installFakeTimers(testNow)

    fireEvent.click(saveButton())
    await letTimeGoBy(outboxQueueAfterMilliseconds - 1)

    expect(readSavedOutbox()).toEqual([])
    expect(router.state.location.pathname).toBe(routes.groupExpenseNew(testGroupId))
    await letTimeGoBy(1)
    expect(readSavedOutbox()).toHaveLength(1)
    expect(router.state.location.pathname).toBe(routes.group(testGroupId))
    expect(savedToastShown()).toBe(false)
    expect(run).toHaveBeenCalledTimes(1)
  })
})
