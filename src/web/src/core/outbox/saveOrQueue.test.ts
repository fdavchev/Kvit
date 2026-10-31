import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createDeferred } from '@/test/outboxTestHelpers'
import { testApiError, testOutboxItem } from '@/test/outboxTestData'
import type { OutboxItem } from './outboxItem'
import { outboxQueueAfterMilliseconds, saveOrQueue } from './saveOrQueue'

const personId = 'person-1'
const item = testOutboxItem('request-1')

function newCall<T>(save: () => Promise<T>, queueAfterMilliseconds?: number) {
  const enqueue = vi.fn<(queued: OutboxItem) => Promise<void>>(async () => undefined)
  const onLateSuccess = vi.fn<() => Promise<void>>(async () => undefined)
  const outcome = saveOrQueue({
    personId,
    item,
    save,
    enqueue,
    onLateSuccess,
    ...(queueAfterMilliseconds === undefined ? {} : { queueAfterMilliseconds }),
  })
  return { outcome, enqueue, onLateSuccess }
}

async function settle(): Promise<void> {
  await vi.advanceTimersByTimeAsync(0)
}

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] })
})

afterEach(() => {
  vi.useRealTimers()
  vi.restoreAllMocks()
})

describe('outboxQueueAfterMilliseconds', () => {
  it('is 3 seconds', () => {
    expect(outboxQueueAfterMilliseconds).toBe(3000)
  })
})

describe('saveOrQueue when the save answers in time', () => {
  it('gives back the result of the save and queues nothing', async () => {
    const { outcome, enqueue, onLateSuccess } = newCall(async () => 'saved expense')

    await expect(outcome).resolves.toEqual({ queued: false, result: 'saved expense' })

    expect(enqueue).not.toHaveBeenCalled()
    expect(onLateSuccess).not.toHaveBeenCalled()
  })

  it('gives back a save that answers just before the time is over', async () => {
    const save = createDeferred<string>()
    const { outcome, enqueue } = newCall(() => save.promise)

    await vi.advanceTimersByTimeAsync(outboxQueueAfterMilliseconds - 1)
    save.resolve('saved late but in time')

    await expect(outcome).resolves.toEqual({ queued: false, result: 'saved late but in time' })
    expect(enqueue).not.toHaveBeenCalled()
  })

  it('calls the save exactly once', async () => {
    const save = vi.fn(async () => 'saved')
    const { outcome } = newCall(save)
    await outcome
    await vi.advanceTimersByTimeAsync(10000)

    expect(save).toHaveBeenCalledTimes(1)
  })

  it('stops its timer so nothing is queued later', async () => {
    const { outcome, enqueue } = newCall(async () => 'saved')
    await outcome

    await vi.advanceTimersByTimeAsync(10000)

    expect(vi.getTimerCount()).toBe(0)
    expect(enqueue).not.toHaveBeenCalled()
  })
})

describe('saveOrQueue when the server cannot be reached', () => {
  const queueableFailures: [string, number | null][] = [
    ['a network error', null],
    ['status 502', 502],
    ['status 503', 503],
    ['status 504', 504],
  ]

  it.each(queueableFailures)('queues the item at once on %s', async (_name, httpStatus) => {
    const { outcome, enqueue } = newCall(async () => {
      throw testApiError(httpStatus)
    })

    await expect(outcome).resolves.toEqual({ queued: true })

    expect(enqueue).toHaveBeenCalledTimes(1)
    expect(enqueue).toHaveBeenCalledWith(item)
  })

  it('does not queue the item a second time when the 3 seconds pass afterwards', async () => {
    const { outcome, enqueue } = newCall(async () => {
      throw testApiError(null)
    })
    await outcome

    await vi.advanceTimersByTimeAsync(10000)

    expect(enqueue).toHaveBeenCalledTimes(1)
    expect(vi.getTimerCount()).toBe(0)
  })

  it('does not call onLateSuccess for a failure that happened in time', async () => {
    const { outcome, onLateSuccess } = newCall(async () => {
      throw testApiError(503)
    })
    await outcome

    await vi.advanceTimersByTimeAsync(10000)

    expect(onLateSuccess).not.toHaveBeenCalled()
  })

  it('fails with the storage error when the item could not be queued', async () => {
    const storageFailure = new Error('IndexedDB write failed')
    const enqueue = vi.fn<(queued: OutboxItem) => Promise<void>>().mockRejectedValue(storageFailure)

    const outcome = saveOrQueue({
      personId,
      item,
      save: async () => {
        throw testApiError(null)
      },
      enqueue,
      onLateSuccess: async () => undefined,
    })

    await expect(outcome).rejects.toBe(storageFailure)
  })
})

describe('saveOrQueue when the server refused the save', () => {
  const refusals: [string, number, string | null][] = [
    ['400 with a code', 400, 'EXPENSE_AMOUNT_INVALID'],
    ['400 without a code', 400, null],
    ['401', 401, null],
    ['403', 403, 'SETTLEMENT_NOT_ALLOWED'],
    ['404', 404, 'GROUP_NOT_FOUND'],
    ['429', 429, null],
    ['500', 500, null],
  ]

  it.each(refusals)('throws the same error for the form on %s', async (_name, httpStatus, errorCode) => {
    const refusal = testApiError(httpStatus, errorCode)
    const { outcome } = newCall(async () => {
      throw refusal
    })

    await expect(outcome).rejects.toBe(refusal)
  })

  it.each(refusals)('queues nothing on %s', async (_name, httpStatus, errorCode) => {
    const { outcome, enqueue, onLateSuccess } = newCall(async () => {
      throw testApiError(httpStatus, errorCode)
    })
    await outcome.catch(() => undefined)

    await vi.advanceTimersByTimeAsync(10000)

    expect(enqueue).not.toHaveBeenCalled()
    expect(onLateSuccess).not.toHaveBeenCalled()
    expect(vi.getTimerCount()).toBe(0)
  })

  it('throws an error that is not an ApiError as it is and queues nothing', async () => {
    const bug = new TypeError('input is undefined')
    const { outcome, enqueue } = newCall(async () => {
      throw bug
    })

    await expect(outcome).rejects.toBe(bug)
    expect(enqueue).not.toHaveBeenCalled()
  })

  it('throws a refusal that comes just before the time is over', async () => {
    const save = createDeferred<string>()
    const refusal = testApiError(400, 'EXPENSE_AMOUNT_INVALID')
    const { outcome, enqueue } = newCall(() => save.promise)
    const failure = expect(outcome).rejects.toBe(refusal)

    await vi.advanceTimersByTimeAsync(outboxQueueAfterMilliseconds - 1)
    save.reject(refusal)

    await failure
    await vi.advanceTimersByTimeAsync(10000)
    expect(enqueue).not.toHaveBeenCalled()
  })
})

describe('saveOrQueue when the save has not answered in time', () => {
  it('waits for the save for the full 3 seconds by default', async () => {
    const save = createDeferred<string>()
    const { outcome, enqueue } = newCall(() => save.promise)
    const onSettled = vi.fn()
    void outcome.then(onSettled)

    await vi.advanceTimersByTimeAsync(2999)

    expect(onSettled).not.toHaveBeenCalled()
    expect(enqueue).not.toHaveBeenCalled()
  })

  it('queues the item and says it was queued once 3 seconds have passed', async () => {
    const save = createDeferred<string>()
    const { outcome, enqueue } = newCall(() => save.promise)

    await vi.advanceTimersByTimeAsync(3000)

    await expect(outcome).resolves.toEqual({ queued: true })
    expect(enqueue).toHaveBeenCalledTimes(1)
    expect(enqueue).toHaveBeenCalledWith(item)
  })

  it('uses the time it is given instead of 3 seconds', async () => {
    const save = createDeferred<string>()
    const { outcome, enqueue } = newCall(() => save.promise, 500)
    const onSettled = vi.fn()
    void outcome.then(onSettled)

    await vi.advanceTimersByTimeAsync(499)
    expect(onSettled).not.toHaveBeenCalled()
    await vi.advanceTimersByTimeAsync(1)

    await expect(outcome).resolves.toEqual({ queued: true })
    expect(enqueue).toHaveBeenCalledTimes(1)
  })

  it('does not call the save a second time', async () => {
    const save = vi.fn(() => new Promise<string>(() => {}))
    const { outcome } = newCall(save)

    await vi.advanceTimersByTimeAsync(3000)
    await outcome

    expect(save).toHaveBeenCalledTimes(1)
  })

  it('fails with the storage error when the item could not be queued', async () => {
    const storageFailure = new Error('IndexedDB write failed')
    const enqueue = vi.fn<(queued: OutboxItem) => Promise<void>>().mockRejectedValue(storageFailure)
    const outcome = saveOrQueue({
      personId,
      item,
      save: () => new Promise<string>(() => {}),
      enqueue,
      onLateSuccess: async () => undefined,
    })
    const failure = expect(outcome).rejects.toBe(storageFailure)

    await vi.advanceTimersByTimeAsync(3000)

    await failure
  })
})

describe('saveOrQueue when the save finishes after the item was queued', () => {
  async function queuedWithSaveStillRunning() {
    const save = createDeferred<string>()
    const call = newCall(() => save.promise)
    await vi.advanceTimersByTimeAsync(outboxQueueAfterMilliseconds)
    await call.outcome
    return { save, ...call }
  }

  it('does not call onLateSuccess while the save is still running', async () => {
    const { onLateSuccess } = await queuedWithSaveStillRunning()

    await vi.advanceTimersByTimeAsync(60000)

    expect(onLateSuccess).not.toHaveBeenCalled()
  })

  it('calls onLateSuccess once with no arguments when the save succeeds later', async () => {
    const { save, onLateSuccess } = await queuedWithSaveStillRunning()

    save.resolve('saved late')
    await settle()

    expect(onLateSuccess).toHaveBeenCalledTimes(1)
    expect(onLateSuccess).toHaveBeenCalledWith()
  })

  it('has queued the item before it reports the late success', async () => {
    const { save, enqueue, onLateSuccess } = await queuedWithSaveStillRunning()

    save.resolve('saved late')
    await settle()

    expect(enqueue.mock.invocationCallOrder[0]).toBeLessThan(
      onLateSuccess.mock.invocationCallOrder[0],
    )
  })

  it('calls nothing and queues nothing more when the save fails later', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    const { save, enqueue, onLateSuccess } = await queuedWithSaveStillRunning()

    save.reject(testApiError(400, 'GROUP_FINISHED'))
    await settle()

    expect(onLateSuccess).not.toHaveBeenCalled()
    expect(enqueue).toHaveBeenCalledTimes(1)
  })

  it('calls nothing and queues nothing more when a network failure comes later', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    const { save, enqueue, onLateSuccess } = await queuedWithSaveStillRunning()

    save.reject(testApiError(null))
    await settle()

    expect(onLateSuccess).not.toHaveBeenCalled()
    expect(enqueue).toHaveBeenCalledTimes(1)
  })

  it('causes no unhandled rejection when the save fails later, which Vitest would report as an error', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    const { save } = await queuedWithSaveStillRunning()

    save.reject(testApiError(400, 'GROUP_FINISHED'))
    vi.useRealTimers()
    await new Promise<void>((resolve) => setTimeout(resolve, 0))
  })
})
