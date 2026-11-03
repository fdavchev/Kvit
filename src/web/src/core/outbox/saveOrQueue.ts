import { isServerNotReachedError } from '@/core/api/errors'
import type { OutboxItem } from './outboxItem'

export const outboxQueueAfterMilliseconds = 3000

export type SaveOrQueueOutcome<Result> = { queued: false; result: Result } | { queued: true }

export type SaveTellingWhenAnswered<Result> = (onAnswered: () => void) => Promise<Result>

export interface AnsweredSignal {
  onAnswered?: () => void
}

export interface SaveOrQueueOptions<Result> {
  personId: string
  item: OutboxItem
  save: SaveTellingWhenAnswered<Result>
  enqueue: (item: OutboxItem) => Promise<void>
  onLateSuccess: () => Promise<void>
  queueAfterMilliseconds?: number
}

export function saveOrQueue<Result>({
  personId,
  item,
  save,
  enqueue,
  onLateSuccess,
  queueAfterMilliseconds = outboxQueueAfterMilliseconds,
}: SaveOrQueueOptions<Result>): Promise<SaveOrQueueOutcome<Result>> {
  return new Promise((resolve, reject) => {
    let hasTimedOut = false
    let hasAnswered = false

    const followLateSave = (saving: Promise<Result>): void => {
      saving.then(
        () =>
          onLateSuccess().catch((error: unknown) => {
            console.error(
              `The save of the queued ${item.kind} ${item.id} of person ${personId} answered late, but finishing it failed`,
              error,
            )
          }),
        (error: unknown) => {
          console.error(
            `The save of the queued ${item.kind} ${item.id} of person ${personId} failed after it was queued; the outbox sends it again`,
            error,
          )
        },
      )
    }

    let timer: ReturnType<typeof setTimeout> | undefined

    const saving = save(() => {
      if (hasTimedOut) {
        return
      }
      hasAnswered = true
      clearTimeout(timer)
    })

    if (!hasAnswered) {
      timer = setTimeout(() => {
        hasTimedOut = true
        enqueue(item).then(() => {
          resolve({ queued: true })
          followLateSave(saving)
        }, reject)
      }, queueAfterMilliseconds)
    }

    saving.then(
      (result) => {
        if (hasTimedOut) {
          return
        }
        clearTimeout(timer)
        resolve({ queued: false, result })
      },
      (error: unknown) => {
        if (hasTimedOut) {
          return
        }
        clearTimeout(timer)
        if (hasAnswered || !isServerNotReachedError(error)) {
          reject(error)
          return
        }
        enqueue(item).then(() => {
          resolve({ queued: true })
        }, reject)
      },
    )
  })
}
