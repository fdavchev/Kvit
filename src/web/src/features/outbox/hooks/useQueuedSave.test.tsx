import { act } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { outboxQueueAfterMilliseconds } from '@/core/outbox/saveOrQueue'
import { testApiError, testOutboxItem } from '@/test/outboxTestData'
import { createDeferred, storedIds } from '@/test/outboxTestHelpers'
import {
  holdOutboxSender,
  letTimeGoBy,
  readSavedOutbox,
  installFakeTimers,
} from '@/test/queuedSaveTestHelpers'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { seedMe, testMe } from '@/test/testMe'
import { useQueuedSave } from './useQueuedSave'

const item = testOutboxItem('request-1')

async function renderQueuedSave() {
  return renderHookWithProviders(() => useQueuedSave(), { seedCache: seedMe(testMe) })
}

beforeEach(() => {
  vi.spyOn(console, 'error').mockImplementation(() => {})
})

afterEach(() => {
  vi.useRealTimers()
  vi.restoreAllMocks()
})

describe('useQueuedSave when the save answers in time', () => {
  it('gives back the result of the save and queues nothing', async () => {
    const { result, outboxSender } = await renderQueuedSave()
    const run = holdOutboxSender(outboxSender)

    let outcome: unknown
    await act(async () => {
      outcome = await result.current.saveOrQueueItem(item, async () => 'saved expense')
    })

    expect(outcome).toEqual({ queued: false, result: 'saved expense' })
    expect(readSavedOutbox()).toEqual([])
    expect(outboxSender.getSnapshot().items).toEqual([])
    expect(run).not.toHaveBeenCalled()
  })

  it('calls the save exactly once', async () => {
    const { result } = await renderQueuedSave()
    const save = vi.fn(async () => 'saved')

    await act(async () => {
      await result.current.saveOrQueueItem(item, save)
    })

    expect(save).toHaveBeenCalledTimes(1)
  })

  it('starts with no error and not pending', async () => {
    const { result } = await renderQueuedSave()

    expect(result.current.isPending).toBe(false)
    expect(result.current.error).toBeNull()
  })

  it('is pending from the call until the save has answered', async () => {
    const { result } = await renderQueuedSave()
    const answer = createDeferred<string>()
    let outcome: Promise<unknown> = Promise.resolve()

    act(() => {
      outcome = result.current.saveOrQueueItem(item, () => answer.promise)
    })

    expect(result.current.isPending).toBe(true)
    await act(async () => {
      answer.resolve('saved')
      await outcome
    })
    expect(result.current.isPending).toBe(false)
    expect(result.current.error).toBeNull()
  })
})

describe('useQueuedSave when the server cannot be reached', () => {
  it.each([
    ['a network error', null],
    ['status 502', 502],
    ['status 503', 503],
    ['status 504', 504],
  ])('queues the item at once on %s and says it was queued', async (_name, httpStatus) => {
    const { result, outboxSender } = await renderQueuedSave()
    holdOutboxSender(outboxSender)

    let outcome: unknown
    await act(async () => {
      outcome = await result.current.saveOrQueueItem(item, async () => {
        throw testApiError(httpStatus)
      })
    })

    expect(outcome).toEqual({ queued: true })
    expect(readSavedOutbox()).toEqual([item])
    expect(outboxSender.getSnapshot().items).toEqual([item])
  })

  it('starts the sender once after the item is queued', async () => {
    const { result, outboxSender } = await renderQueuedSave()
    const run = holdOutboxSender(outboxSender)

    await act(async () => {
      await result.current.saveOrQueueItem(item, async () => {
        throw testApiError(null)
      })
    })

    expect(run).toHaveBeenCalledTimes(1)
  })

  it('starts the sender with an attempt that does not count as a retry, because a new save caused it', async () => {
    const { result, outboxSender } = await renderQueuedSave()
    const run = holdOutboxSender(outboxSender)

    await act(async () => {
      await result.current.saveOrQueueItem(item, async () => {
        throw testApiError(null)
      })
    })

    expect(run).toHaveBeenCalledWith({ countsAsRetry: false })
  })

  it('has stored the item before it starts the sender', async () => {
    const { result, outboxSender } = await renderQueuedSave()
    const itemsWhenTheSenderStarts: string[][] = []
    vi.spyOn(outboxSender, 'run').mockImplementation(async () => {
      itemsWhenTheSenderStarts.push(storedIds(testMe.id))
    })

    await act(async () => {
      await result.current.saveOrQueueItem(item, async () => {
        throw testApiError(null)
      })
    })

    expect(itemsWhenTheSenderStarts).toEqual([['request-1']])
  })

  it('does not wait for the sender to finish sending before it answers', async () => {
    const { result, outboxSender } = await renderQueuedSave()
    vi.spyOn(outboxSender, 'run').mockImplementation(() => new Promise<void>(() => {}))

    let outcome: unknown
    await act(async () => {
      outcome = await result.current.saveOrQueueItem(item, async () => {
        throw testApiError(null)
      })
    })

    expect(outcome).toEqual({ queued: true })
  })

  it('shows no error and is not pending afterwards', async () => {
    const { result, outboxSender } = await renderQueuedSave()
    holdOutboxSender(outboxSender)

    await act(async () => {
      await result.current.saveOrQueueItem(item, async () => {
        throw testApiError(503)
      })
    })

    expect(result.current.error).toBeNull()
    expect(result.current.isPending).toBe(false)
  })

  it('does not queue the same item twice when it is called again with the same item', async () => {
    const { result, outboxSender } = await renderQueuedSave()
    holdOutboxSender(outboxSender)
    const failing = async (): Promise<never> => {
      throw testApiError(null)
    }

    await act(async () => {
      await result.current.saveOrQueueItem(item, failing)
      await result.current.saveOrQueueItem(item, failing)
    })

    expect(storedIds(testMe.id)).toEqual(['request-1'])
  })
})

describe('useQueuedSave when the server does not answer for 3 seconds', () => {
  it('is still pending and has queued nothing just before the time is over', async () => {
    const { result } = await renderQueuedSave()
    installFakeTimers()

    act(() => {
      void result.current.saveOrQueueItem(item, () => new Promise<string>(() => {}))
    })
    await letTimeGoBy(outboxQueueAfterMilliseconds - 1)

    expect(result.current.isPending).toBe(true)
    expect(readSavedOutbox()).toEqual([])
  })

  it('queues the item, says so, starts the sender and ends the pending state when the time is over', async () => {
    const { result, outboxSender } = await renderQueuedSave()
    const run = holdOutboxSender(outboxSender)
    installFakeTimers()
    let outcome: Promise<unknown> = Promise.resolve()
    act(() => {
      outcome = result.current.saveOrQueueItem(item, () => new Promise<string>(() => {}))
    })

    await letTimeGoBy(outboxQueueAfterMilliseconds)

    await expect(outcome).resolves.toEqual({ queued: true })
    expect(readSavedOutbox()).toEqual([item])
    expect(run).toHaveBeenCalledTimes(1)
    expect(result.current.isPending).toBe(false)
    expect(result.current.error).toBeNull()
  })
})

describe('useQueuedSave when the server answered the save but finishing it takes long', () => {
  function savingInTwoSteps() {
    const finished = createDeferred<string>()
    let tellThatTheServerAnswered: (() => void) | undefined
    const save = (onAnswered?: () => void): Promise<string> => {
      tellThatTheServerAnswered = onAnswered
      return finished.promise
    }
    function answer(): void {
      if (tellThatTheServerAnswered === undefined) {
        throw new Error('saveOrQueueItem did not give the save an onAnswered function to call')
      }
      tellThatTheServerAnswered()
    }
    return { save, answer, finished }
  }

  it('does not queue the item and starts no sender although finishing takes much longer than 3 seconds', async () => {
    const { result, outboxSender } = await renderQueuedSave()
    const run = holdOutboxSender(outboxSender)
    const saving = savingInTwoSteps()
    installFakeTimers()
    act(() => {
      void result.current.saveOrQueueItem(item, saving.save)
    })

    saving.answer()
    await letTimeGoBy(outboxQueueAfterMilliseconds * 3)

    expect(readSavedOutbox()).toEqual([])
    expect(run).not.toHaveBeenCalled()
    expect(result.current.isPending).toBe(true)
  })

  it('gives back the result and ends the pending state when the save has finished', async () => {
    const { result } = await renderQueuedSave()
    const saving = savingInTwoSteps()
    installFakeTimers()
    let outcome: Promise<unknown> = Promise.resolve()
    act(() => {
      outcome = result.current.saveOrQueueItem(item, saving.save)
    })
    saving.answer()
    await letTimeGoBy(outboxQueueAfterMilliseconds * 3)

    await act(async () => {
      saving.finished.resolve('saved and refreshed')
      await vi.advanceTimersByTimeAsync(0)
    })

    await expect(outcome).resolves.toEqual({ queued: false, result: 'saved and refreshed' })
    expect(result.current.isPending).toBe(false)
    expect(result.current.error).toBeNull()
  })

  it('still queues the item when the server has not answered within 3 seconds', async () => {
    const { result, outboxSender } = await renderQueuedSave()
    const run = holdOutboxSender(outboxSender)
    const saving = savingInTwoSteps()
    installFakeTimers()
    let outcome: Promise<unknown> = Promise.resolve()
    act(() => {
      outcome = result.current.saveOrQueueItem(item, saving.save)
    })

    await letTimeGoBy(outboxQueueAfterMilliseconds)

    await expect(outcome).resolves.toEqual({ queued: true })
    expect(readSavedOutbox()).toEqual([item])
    expect(run).toHaveBeenCalledTimes(1)
  })
})

describe('useQueuedSave when the save answers after the item was queued', () => {
  async function queuedWithSaveStillRunning() {
    const rendered = await renderQueuedSave()
    const run = holdOutboxSender(rendered.outboxSender)
    const lateAnswer = createDeferred<string>()
    installFakeTimers()
    act(() => {
      void rendered.result.current.saveOrQueueItem(item, () => lateAnswer.promise)
    })
    await letTimeGoBy(outboxQueueAfterMilliseconds)
    return { ...rendered, run, lateAnswer }
  }

  it('removes the queued item from the outbox and from the browser when the save succeeds late', async () => {
    const { lateAnswer, outboxSender } = await queuedWithSaveStillRunning()
    expect(readSavedOutbox()).toEqual([item])

    await act(async () => {
      lateAnswer.resolve('saved late')
      await vi.advanceTimersByTimeAsync(0)
    })

    expect(readSavedOutbox()).toEqual([])
    expect(outboxSender.getSnapshot().items).toEqual([])
  })

  it('discards the item by its id', async () => {
    const { lateAnswer, outboxSender } = await queuedWithSaveStillRunning()
    const discard = vi.spyOn(outboxSender, 'discard')

    await act(async () => {
      lateAnswer.resolve('saved late')
      await vi.advanceTimersByTimeAsync(0)
    })

    expect(discard).toHaveBeenCalledTimes(1)
    expect(discard).toHaveBeenCalledWith('request-1')
  })

  it('does nothing more when the sender is already sending the item and so refuses to discard it', async () => {
    const { lateAnswer, outboxSender } = await queuedWithSaveStillRunning()
    const discard = vi.spyOn(outboxSender, 'discard').mockResolvedValue(false)

    await act(async () => {
      lateAnswer.resolve('saved late')
      await vi.advanceTimersByTimeAsync(0)
    })

    expect(discard).toHaveBeenCalledTimes(1)
    expect(readSavedOutbox()).toEqual([item])
  })

  it('does not send the item again by itself', async () => {
    const { lateAnswer, run } = await queuedWithSaveStillRunning()
    run.mockClear()

    await act(async () => {
      lateAnswer.resolve('saved late')
      await vi.advanceTimersByTimeAsync(0)
    })

    expect(run).not.toHaveBeenCalled()
  })

  it('keeps the item queued and shows no error when the save fails late', async () => {
    const { result, lateAnswer, outboxSender } = await queuedWithSaveStillRunning()
    const discard = vi.spyOn(outboxSender, 'discard')

    await act(async () => {
      lateAnswer.reject(testApiError(400, 'GROUP_FINISHED'))
      await vi.advanceTimersByTimeAsync(0)
    })

    expect(readSavedOutbox()).toEqual([item])
    expect(discard).not.toHaveBeenCalled()
    expect(result.current.error).toBeNull()
  })
})

describe('useQueuedSave when the server refused the save', () => {
  const refusals: [string, number, string | null][] = [
    ['400 with a code', 400, 'EXPENSE_AMOUNT_NOT_POSITIVE'],
    ['404', 404, 'GROUP_NOT_FOUND'],
    ['403', 403, 'SETTLEMENT_NOT_ALLOWED'],
    ['429', 429, 'RATE_LIMITED'],
    ['500', 500, null],
  ]

  it.each(refusals)('fails with the same error on %s and queues nothing', async (_name, httpStatus, errorCode) => {
    const { result, outboxSender } = await renderQueuedSave()
    const run = holdOutboxSender(outboxSender)
    const refusal = testApiError(httpStatus, errorCode)
    let failure: unknown

    await act(async () => {
      failure = await result.current
        .saveOrQueueItem(item, async () => {
          throw refusal
        })
        .catch((error: unknown) => error)
    })

    expect(failure).toBe(refusal)
    expect(readSavedOutbox()).toEqual([])
    expect(run).not.toHaveBeenCalled()
  })

  it('keeps the error of the refusal and ends the pending state', async () => {
    const { result } = await renderQueuedSave()
    const refusal = testApiError(400, 'EXPENSE_AMOUNT_NOT_POSITIVE')

    await act(async () => {
      await result.current
        .saveOrQueueItem(item, async () => {
          throw refusal
        })
        .catch(() => undefined)
    })

    expect(result.current.error).toBe(refusal)
    expect(result.current.isPending).toBe(false)
  })

  it('keeps the error of an error that is not an ApiError too', async () => {
    const { result } = await renderQueuedSave()
    const bug = new TypeError('input is undefined')

    await act(async () => {
      await result.current
        .saveOrQueueItem(item, async () => {
          throw bug
        })
        .catch(() => undefined)
    })

    expect(result.current.error).toBe(bug)
  })

  it('clears the error at the start of the next call and keeps it clear when that call succeeds', async () => {
    const { result } = await renderQueuedSave()
    await act(async () => {
      await result.current
        .saveOrQueueItem(item, async () => {
          throw testApiError(400, 'EXPENSE_AMOUNT_NOT_POSITIVE')
        })
        .catch(() => undefined)
    })
    expect(result.current.error).not.toBeNull()
    const answer = createDeferred<string>()
    let secondCall: Promise<unknown> = Promise.resolve()

    act(() => {
      secondCall = result.current.saveOrQueueItem(item, () => answer.promise)
    })

    expect(result.current.error).toBeNull()
    expect(result.current.isPending).toBe(true)
    await act(async () => {
      answer.resolve('saved')
      await secondCall
    })
    expect(result.current.error).toBeNull()
  })

  it('replaces the error with the error of the next refusal', async () => {
    const { result } = await renderQueuedSave()
    const first = testApiError(400, 'EXPENSE_AMOUNT_NOT_POSITIVE')
    const second = testApiError(404, 'GROUP_NOT_FOUND')
    await act(async () => {
      await result.current.saveOrQueueItem(item, () => Promise.reject(first)).catch(() => undefined)
    })

    await act(async () => {
      await result.current.saveOrQueueItem(item, () => Promise.reject(second)).catch(() => undefined)
    })

    expect(result.current.error).toBe(second)
  })

  it('clears the error when the next call is queued', async () => {
    const { result, outboxSender } = await renderQueuedSave()
    holdOutboxSender(outboxSender)
    await act(async () => {
      await result.current.saveOrQueueItem(item, () => Promise.reject(testApiError(400, 'GROUP_FINISHED'))).catch(() => undefined)
    })

    await act(async () => {
      await result.current.saveOrQueueItem(item, () => Promise.reject(testApiError(null)))
    })

    expect(result.current.error).toBeNull()
  })
})

describe('useQueuedSave when the item could not be stored', () => {
  it('fails with the storage error, keeps it as the error and starts no sender', async () => {
    const { result, outboxSender } = await renderQueuedSave()
    const run = holdOutboxSender(outboxSender)
    const storageFailure = new Error('IndexedDB write failed')
    vi.spyOn(outboxSender, 'enqueue').mockRejectedValue(storageFailure)
    let failure: unknown

    await act(async () => {
      failure = await result.current
        .saveOrQueueItem(item, async () => {
          throw testApiError(null)
        })
        .catch((error: unknown) => error)
    })

    expect(failure).toBe(storageFailure)
    expect(result.current.error).toBe(storageFailure)
    expect(result.current.isPending).toBe(false)
    expect(run).not.toHaveBeenCalled()
  })

  it('fails with the storage error when the 3 seconds are over too', async () => {
    const { result, outboxSender } = await renderQueuedSave()
    const storageFailure = new Error('IndexedDB write failed')
    vi.spyOn(outboxSender, 'enqueue').mockRejectedValue(storageFailure)
    installFakeTimers()
    let outcome: Promise<unknown> = Promise.resolve()
    act(() => {
      outcome = result.current.saveOrQueueItem(item, () => new Promise<string>(() => {}))
    })
    const failure = expect(outcome).rejects.toBe(storageFailure)

    await letTimeGoBy(outboxQueueAfterMilliseconds)

    await failure
    expect(result.current.error).toBe(storageFailure)
  })
})
