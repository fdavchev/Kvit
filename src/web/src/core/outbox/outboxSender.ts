import { ApiError, forbiddenStatus, unauthorizedStatus } from '@/core/api/apiClient'
import { isServerNotReachedError } from '@/core/api/errors'
import { backoffMilliseconds } from './backoff'
import type { OutboxFailure, OutboxItem, OutboxItemChange } from './outboxItem'
import { addOutboxItem, readOutbox, removeOutboxItem, updateOutboxItem } from './outboxStorage'

export interface OutboxSnapshot {
  items: readonly OutboxItem[]
  isSending: boolean
  justFinished: boolean
}

export interface OutboxSenderOptions {
  personId: string
  sendItem: (item: OutboxItem) => Promise<unknown>
  afterSent: (item: OutboxItem) => Promise<void>
}

export interface OutboxRunOptions {
  countsAsRetry?: boolean
}

export interface OutboxSender {
  getSnapshot: () => OutboxSnapshot
  subscribe: (listener: () => void) => () => void
  enqueue: (item: OutboxItem) => Promise<void>
  run: (options?: OutboxRunOptions) => Promise<void>
  retry: (id: string) => Promise<void>
  discard: (id: string) => Promise<boolean>
  holdWhileSaving: (id: string, saving: Promise<unknown>) => void
  pause: () => void
  resume: () => void
  isPaused: () => boolean
}

type SendOutcome = 'sent' | 'failed' | 'retryLater' | 'paused'

export const outboxSendTimeoutMilliseconds = 30_000

const mustChangePasswordCode = 'AUTH_MUST_CHANGE_PASSWORD'
const requestTimeoutStatus = 408
const tooEarlyStatus = 425
const tooManyRequestsStatus = 429
const gatewayStatuses: readonly number[] = [520, 521, 522, 523, 524]
const tryAgainLaterStatuses: readonly number[] = [
  requestTimeoutStatus,
  tooEarlyStatus,
  tooManyRequestsStatus,
  ...gatewayStatuses,
]
const noReason: OutboxFailure = { errorCode: null, httpStatus: null }

export function runWithoutWaiting(sender: Pick<OutboxSender, 'run'>, options?: OutboxRunOptions): void {
  sender.run(options).catch((error: unknown) => {
    console.error('The outbox could not send the waiting changes', error)
  })
}

export function createOutboxSender({ personId, sendItem, afterSent }: OutboxSenderOptions): OutboxSender {
  let snapshot: OutboxSnapshot = { items: [], isSending: false, justFinished: false }
  const listeners = new Set<() => void>()
  const heldIds = new Set<string>()
  const savedByOriginalIds = new Set<string>()
  let paused = false
  let failedAttempts = 0
  let retryTimer: ReturnType<typeof setTimeout> | undefined
  let running: Promise<void> | undefined
  let isRunEnding = false
  let loading: Promise<void> | undefined

  waitUntilLoaded().catch((error: unknown) => {
    console.error('The outbox loads the saved changes again on the next save, send, retry or discard', error)
  })

  function waitUntilLoaded(): Promise<void> {
    loading ??= loadSavedItems()
    return loading
  }

  async function loadSavedItems(): Promise<void> {
    try {
      const savedItems = await readOutbox(personId)
      publish({ items: savedItems.map(asWaitingIfLeftSending) })
    } catch (error) {
      loading = undefined
      throw new Error(
        `Could not load the outbox of person ${personId} from this browser (IndexedDB): ${messageOf(error)}`,
        { cause: error },
      )
    }
  }

  function publish(change: Partial<OutboxSnapshot>): void {
    snapshot = { ...snapshot, ...change }
    listeners.forEach((listener) => listener())
  }

  function changeItem(id: string, change: OutboxItemChange & Pick<OutboxItem, 'isSaved'>): void {
    publish({ items: snapshot.items.map((item) => (item.id === id ? { ...item, ...change } : item)) })
  }

  function forgetItem(id: string): void {
    publish({ items: snapshot.items.filter((item) => item.id !== id) })
  }

  function findItem(id: string): OutboxItem | undefined {
    return snapshot.items.find((item) => item.id === id)
  }

  function nextWaitingItem(): OutboxItem | undefined {
    return snapshot.items.find(
      (item) => item.state === 'waiting' && !heldIds.has(item.id) && !savedByOriginalIds.has(item.id),
    )
  }

  function subscribe(listener: () => void): () => void {
    listeners.add(listener)
    return () => {
      listeners.delete(listener)
    }
  }

  async function enqueue(item: OutboxItem): Promise<void> {
    await waitUntilLoaded()
    await addOutboxItem(personId, item)
    if (findItem(item.id) !== undefined) {
      return
    }
    publish({ items: [...snapshot.items, item], justFinished: false })
  }

  function run({ countsAsRetry = true }: OutboxRunOptions = {}): Promise<void> {
    if (running !== undefined && !isRunEnding) {
      return running
    }
    isRunEnding = false
    const thisRun: Promise<void> = sendWaitingItems(countsAsRetry).finally(() => {
      if (running === thisRun) {
        running = undefined
      }
    })
    running = thisRun
    return thisRun
  }

  async function sendWaitingItems(countsAsRetry: boolean): Promise<void> {
    let hasStartedSending = false
    try {
      await waitUntilLoaded()
      if (paused || nextWaitingItem() === undefined) {
        return
      }
      stopRetryTimer()
      hasStartedSending = true
      for (let item = nextWaitingItem(); item !== undefined && !isPaused(); item = nextWaitingItem()) {
        const outcome = await sendOne(item, countsAsRetry)
        if (outcome === 'retryLater') {
          scheduleRetry()
          return
        }
      }
    } finally {
      isRunEnding = true
      if (hasStartedSending) {
        publish({ isSending: false })
      }
    }
  }

  async function sendOne(item: OutboxItem, countsAsRetry: boolean): Promise<SendOutcome> {
    const sendingItem: OutboxItem = { ...item, state: 'sending' }
    publish({
      items: snapshot.items.map((queued) => (queued.id === item.id ? sendingItem : queued)),
      isSending: true,
      justFinished: false,
    })
    try {
      await sendWithinTimeLimit(sendingItem)
    } catch (error) {
      return handleSendFailure(item, error, countsAsRetry)
    }
    changeItem(item.id, { isSaved: true })
    try {
      await afterSent(sendingItem)
    } catch (error) {
      console.error(
        `The ${item.kind} ${item.id} was sent, but refreshing the screens after it failed; the outbox marks it as failed so Retry can send it again with the same id`,
        error,
      )
      changeItem(item.id, { isSaved: false })
      await markFailed(item.id, noReason)
      return 'failed'
    }
    const itemsLeft = snapshot.items.filter((queued) => queued.id !== item.id)
    publish({ items: itemsLeft, justFinished: itemsLeft.length === 0 })
    await removeOutboxItem(personId, item.id)
    failedAttempts = 0
    return 'sent'
  }

  function sendWithinTimeLimit(item: OutboxItem): Promise<unknown> {
    let timer: ReturnType<typeof setTimeout> | undefined
    const timeLimit = new Promise<never>((_resolve, reject) => {
      timer = setTimeout(() => {
        reject(
          new ApiError(
            `Sending the waiting ${item.kind} ${item.id} got no answer within ${outboxSendTimeoutMilliseconds} ms`,
            { httpStatus: null },
          ),
        )
      }, outboxSendTimeoutMilliseconds)
    })
    return Promise.race([sendItem(item), timeLimit]).finally(() => {
      clearTimeout(timer)
    })
  }

  async function handleSendFailure(item: OutboxItem, error: unknown, countsAsRetry: boolean): Promise<SendOutcome> {
    if (!(error instanceof ApiError)) {
      console.error(
        `Could not send the waiting ${item.kind} ${item.id} because of an error that is not an API error; the outbox marks it as failed`,
        error,
      )
      await markFailed(item.id, noReason)
      return 'failed'
    }
    if (shouldRetryLater(error)) {
      failedAttempts = countsAsRetry ? failedAttempts + 1 : Math.max(failedAttempts, 1)
      changeItem(item.id, { state: 'waiting', failure: null })
      return 'retryLater'
    }
    if (needsSignIn(error)) {
      paused = true
      changeItem(item.id, { state: 'waiting', failure: null })
      return 'paused'
    }
    await markFailed(item.id, { errorCode: error.errorCode, httpStatus: error.httpStatus })
    return 'failed'
  }

  async function markFailed(id: string, failure: OutboxFailure): Promise<void> {
    changeItem(id, { state: 'failed', failure })
    await updateOutboxItem(personId, id, { state: 'failed', failure })
  }

  function scheduleRetry(): void {
    retryTimer = setTimeout(() => {
      retryTimer = undefined
      if (canSendInBackground()) {
        runWithoutWaiting({ run })
      }
    }, backoffMilliseconds(failedAttempts))
  }

  function stopRetryTimer(): void {
    clearTimeout(retryTimer)
    retryTimer = undefined
  }

  async function retry(id: string): Promise<void> {
    await waitUntilLoaded()
    if (findItem(id)?.state !== 'failed') {
      return
    }
    await updateOutboxItem(personId, id, { state: 'waiting', failure: null })
    changeItem(id, { state: 'waiting', failure: null })
    runWithoutWaiting({ run })
  }

  async function discard(id: string): Promise<boolean> {
    await waitUntilLoaded()
    const item = findItem(id)
    if (item === undefined || item.state === 'sending' || heldIds.has(id)) {
      return false
    }
    savedByOriginalIds.delete(id)
    forgetItem(id)
    await removeOutboxItem(personId, id)
    return true
  }

  function holdWhileSaving(id: string, saving: Promise<unknown>): void {
    if (findItem(id) === undefined) {
      throw new Error(
        `The outbox of person ${personId} cannot wait for the original save of ${id}, because ${id} is not in the queue`,
      )
    }
    heldIds.add(id)
    changeItem(id, { state: 'sending' })
    saving.then(
      () => {
        heldIds.delete(id)
        savedByOriginalIds.add(id)
        changeItem(id, { state: 'waiting' })
      },
      () => {
        heldIds.delete(id)
        changeItem(id, { state: 'waiting' })
        runWithoutWaiting({ run }, { countsAsRetry: false })
      },
    )
  }

  function pause(): void {
    paused = true
    stopRetryTimer()
  }

  function resume(): void {
    paused = false
  }

  function isPaused(): boolean {
    return paused
  }

  return {
    getSnapshot: () => snapshot,
    subscribe,
    enqueue,
    run,
    retry,
    discard,
    holdWhileSaving,
    pause,
    resume,
    isPaused,
  }
}

function asWaitingIfLeftSending(item: OutboxItem): OutboxItem {
  return item.state === 'sending' ? { ...item, state: 'waiting' } : item
}

function shouldRetryLater(error: ApiError): boolean {
  return (
    isServerNotReachedError(error) ||
    (error.httpStatus !== null && tryAgainLaterStatuses.includes(error.httpStatus))
  )
}

function needsSignIn(error: ApiError): boolean {
  return (
    error.httpStatus === unauthorizedStatus ||
    (error.httpStatus === forbiddenStatus && error.errorCode === mustChangePasswordCode)
  )
}

function canSendInBackground(): boolean {
  return document.visibilityState === 'visible' && navigator.onLine
}

function messageOf(error: unknown): string {
  return error instanceof Error ? error.message : String(error)
}
