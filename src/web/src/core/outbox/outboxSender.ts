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

export interface OutboxSender {
  getSnapshot: () => OutboxSnapshot
  subscribe: (listener: () => void) => () => void
  enqueue: (item: OutboxItem) => Promise<void>
  run: () => Promise<void>
  retry: (id: string) => Promise<void>
  discard: (id: string) => Promise<boolean>
  pause: () => void
  resume: () => void
  isPaused: () => boolean
}

type SendOutcome = 'sent' | 'failed' | 'retryLater' | 'paused'

const mustChangePasswordCode = 'AUTH_MUST_CHANGE_PASSWORD'
const requestTimeoutStatus = 408
const tooEarlyStatus = 425
const tooManyRequestsStatus = 429
const tryAgainLaterStatuses: readonly number[] = [requestTimeoutStatus, tooEarlyStatus, tooManyRequestsStatus]
const noReason: OutboxFailure = { errorCode: null, httpStatus: null }

export function runWithoutWaiting(sender: Pick<OutboxSender, 'run'>): void {
  sender.run().catch((error: unknown) => {
    console.error('The outbox could not send the waiting changes', error)
  })
}

export function createOutboxSender({ personId, sendItem, afterSent }: OutboxSenderOptions): OutboxSender {
  let snapshot: OutboxSnapshot = { items: [], isSending: false, justFinished: false }
  const listeners = new Set<() => void>()
  let paused = false
  let failedAttempts = 0
  let retryTimer: ReturnType<typeof setTimeout> | undefined
  let running: Promise<void> | undefined
  let loadFailure: Error | undefined
  const loading = loadSavedItems()

  async function loadSavedItems(): Promise<void> {
    try {
      const savedItems = await readOutbox(personId)
      publish({ items: savedItems.map(asWaitingIfLeftSending) })
    } catch (error) {
      loadFailure = new Error(
        `Could not load the outbox of person ${personId} from this browser (IndexedDB): ${messageOf(error)}`,
        { cause: error },
      )
    }
  }

  async function waitUntilLoaded(): Promise<void> {
    await loading
    if (loadFailure !== undefined) {
      throw loadFailure
    }
  }

  function publish(change: Partial<OutboxSnapshot>): void {
    snapshot = { ...snapshot, ...change }
    listeners.forEach((listener) => listener())
  }

  function changeItem(id: string, change: OutboxItemChange): void {
    publish({ items: snapshot.items.map((item) => (item.id === id ? { ...item, ...change } : item)) })
  }

  function forgetItem(id: string): void {
    publish({ items: snapshot.items.filter((item) => item.id !== id) })
  }

  function findItem(id: string): OutboxItem | undefined {
    return snapshot.items.find((item) => item.id === id)
  }

  function nextWaitingItem(): OutboxItem | undefined {
    return snapshot.items.find((item) => item.state === 'waiting')
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

  function run(): Promise<void> {
    running ??= sendWaitingItems().finally(() => {
      running = undefined
    })
    return running
  }

  async function sendWaitingItems(): Promise<void> {
    await waitUntilLoaded()
    if (paused || nextWaitingItem() === undefined) {
      return
    }
    stopRetryTimer()
    let sentCount = 0
    try {
      for (let item = nextWaitingItem(); item !== undefined && !isPaused(); item = nextWaitingItem()) {
        const outcome = await sendOne(item)
        if (outcome === 'sent') {
          sentCount += 1
        }
        if (outcome === 'retryLater') {
          scheduleRetry()
          return
        }
      }
    } finally {
      publish({ isSending: false, justFinished: sentCount > 0 && snapshot.items.length === 0 })
    }
  }

  async function sendOne(item: OutboxItem): Promise<SendOutcome> {
    const sendingItem: OutboxItem = { ...item, state: 'sending' }
    publish({
      items: snapshot.items.map((queued) => (queued.id === item.id ? sendingItem : queued)),
      isSending: true,
      justFinished: false,
    })
    try {
      await sendItem(sendingItem)
    } catch (error) {
      return handleSendFailure(item, error)
    }
    try {
      await afterSent(sendingItem)
    } catch (error) {
      console.error(
        `The ${item.kind} ${item.id} was sent, but refreshing the screens after it failed; the outbox marks it as failed so Retry can send it again with the same id`,
        error,
      )
      await markFailed(item.id, noReason)
      return 'failed'
    }
    forgetItem(item.id)
    await removeOutboxItem(personId, item.id)
    failedAttempts = 0
    return 'sent'
  }

  async function handleSendFailure(item: OutboxItem, error: unknown): Promise<SendOutcome> {
    if (!(error instanceof ApiError)) {
      console.error(
        `Could not send the waiting ${item.kind} ${item.id} because of an error that is not an API error; the outbox marks it as failed`,
        error,
      )
      await markFailed(item.id, noReason)
      return 'failed'
    }
    if (shouldRetryLater(error)) {
      failedAttempts += 1
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
    if (item === undefined || item.state === 'sending') {
      return false
    }
    forgetItem(id)
    await removeOutboxItem(personId, id)
    return true
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
