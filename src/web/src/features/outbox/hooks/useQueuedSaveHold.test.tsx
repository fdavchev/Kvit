import { act } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { outboxQueueAfterMilliseconds } from '@/core/outbox/saveOrQueue'
import { createExpense, type ExpenseInput } from '@/core/services/expenses/expensesService'
import { hotelDetail } from '@/test/expenseTestData'
import { expensesPath } from '@/test/expenseTestHelpers'
import { queuedExpense } from '@/test/outboxScreenTestData'
import { testApiError } from '@/test/outboxTestData'
import { createDeferred } from '@/test/outboxTestHelpers'
import { holdOutboxSender, installFakeTimers, letTimeGoBy, readSavedOutbox } from '@/test/queuedSaveTestHelpers'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { requestCount, stubFetchByRequest } from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { useQueuedSave } from './useQueuedSave'

const expense = queuedExpense('request-1')
const expenseInput = expense.payload as ExpenseInput

async function renderQueuedSave() {
  return renderHookWithProviders(() => useQueuedSave(), { seedCache: seedMe(testMe) })
}

function slowFirstPost() {
  const originalAnswer = createDeferred<Response>()
  let posts = 0
  const fetchMock = stubFetchByRequest({
    [`POST ${expensesPath}`]: () => {
      posts += 1
      return posts === 1 ? originalAnswer.promise : Response.json(hotelDetail)
    },
  })
  return { fetchMock, originalAnswer }
}

function postCount(fetchMock: ReturnType<typeof stubFetchByRequest>): number {
  return requestCount(fetchMock, 'POST', expensesPath)
}

async function startSlowSave(result: { current: ReturnType<typeof useQueuedSave> }) {
  act(() => {
    void result.current.saveOrQueueItem(expense, async (onAnswered) => {
      const created = await createExpense(expense.groupId ?? '', expenseInput)
      onAnswered()
      return created
    })
  })
  await letTimeGoBy(outboxQueueAfterMilliseconds)
}

beforeEach(() => {
  vi.spyOn(console, 'error').mockImplementation(() => {})
})

afterEach(() => {
  vi.useRealTimers()
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe('a save that is slower than 3 seconds, sent through the real sender', () => {
  it('is queued but not sent again while the original request is still running', async () => {
    const { fetchMock } = slowFirstPost()
    const { result, outboxSender } = await renderQueuedSave()
    installFakeTimers()

    await startSlowSave(result)
    await letTimeGoBy(60_000)

    expect(readSavedOutbox().map((item) => item.id)).toEqual(['request-1'])
    expect(postCount(fetchMock)).toBe(1)
    expect(outboxSender.getSnapshot().items.map((item) => item.state)).toEqual(['sending'])
  })

  it('is sent exactly once in all when the original request then succeeds', async () => {
    const { fetchMock, originalAnswer } = slowFirstPost()
    const { result, outboxSender } = await renderQueuedSave()
    installFakeTimers()
    await startSlowSave(result)

    await act(async () => {
      originalAnswer.resolve(Response.json(hotelDetail))
      await vi.advanceTimersByTimeAsync(60_000)
    })

    expect(postCount(fetchMock)).toBe(1)
    expect(outboxSender.getSnapshot().items).toEqual([])
    expect(readSavedOutbox()).toEqual([])
  })

  it('is sent by the sender, once, when the original request then fails with no answer', async () => {
    const { fetchMock, originalAnswer } = slowFirstPost()
    const { result, outboxSender } = await renderQueuedSave()
    installFakeTimers()
    await startSlowSave(result)

    await act(async () => {
      originalAnswer.reject(new TypeError('Failed to fetch'))
      await vi.advanceTimersByTimeAsync(60_000)
    })

    expect(postCount(fetchMock)).toBe(2)
    expect(outboxSender.getSnapshot().items).toEqual([])
    expect(readSavedOutbox()).toEqual([])
  })

  it('is sent by the sender, once, when the original request then gets a 503', async () => {
    const { fetchMock, originalAnswer } = slowFirstPost()
    const { result, outboxSender } = await renderQueuedSave()
    installFakeTimers()
    await startSlowSave(result)

    await act(async () => {
      originalAnswer.resolve(new Response(null, { status: 503 }))
      await vi.advanceTimersByTimeAsync(60_000)
    })

    expect(postCount(fetchMock)).toBe(2)
    expect(outboxSender.getSnapshot().items).toEqual([])
  })
})

describe('useQueuedSave telling the sender to wait for the original request', () => {
  it('holds the queued item for the save that is still running after 3 seconds', async () => {
    const { result, outboxSender } = await renderQueuedSave()
    holdOutboxSender(outboxSender)
    const hold = vi.spyOn(outboxSender, 'holdWhileSaving')
    installFakeTimers()

    act(() => {
      void result.current.saveOrQueueItem(expense, () => new Promise<string>(() => {}))
    })
    await letTimeGoBy(outboxQueueAfterMilliseconds)

    expect(hold).toHaveBeenCalledTimes(1)
    expect(hold).toHaveBeenCalledWith('request-1', expect.any(Promise))
  })

  it('holds the item before it starts the sender', async () => {
    const { result, outboxSender } = await renderQueuedSave()
    const calls: string[] = []
    vi.spyOn(outboxSender, 'holdWhileSaving').mockImplementation(() => {
      calls.push('hold')
    })
    vi.spyOn(outboxSender, 'run').mockImplementation(async () => {
      calls.push('run')
    })
    installFakeTimers()

    act(() => {
      void result.current.saveOrQueueItem(expense, () => new Promise<string>(() => {}))
    })
    await letTimeGoBy(outboxQueueAfterMilliseconds)

    expect(calls).toEqual(['hold', 'run'])
  })

  it('holds nothing when the save already failed with no answer, because nothing is running any more', async () => {
    const { result, outboxSender } = await renderQueuedSave()
    holdOutboxSender(outboxSender)
    const hold = vi.spyOn(outboxSender, 'holdWhileSaving')

    await act(async () => {
      await result.current.saveOrQueueItem(expense, async () => {
        throw testApiError(null)
      })
    })

    expect(hold).not.toHaveBeenCalled()
    expect(readSavedOutbox().map((item) => item.id)).toEqual(['request-1'])
  })

  it('holds nothing when the save answers in time', async () => {
    const { result, outboxSender } = await renderQueuedSave()
    const hold = vi.spyOn(outboxSender, 'holdWhileSaving')

    await act(async () => {
      await result.current.saveOrQueueItem(expense, async () => 'saved')
    })

    expect(hold).not.toHaveBeenCalled()
  })
})
