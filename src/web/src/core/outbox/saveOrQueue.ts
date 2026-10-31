import { isServerNotReachedError } from '@/core/api/errors'
import type { OutboxItem } from './outboxItem'

export const outboxQueueAfterMilliseconds = 3000

export type SaveOrQueueOutcome<Result> = { queued: false; result: Result } | { queued: true }

export interface SaveOrQueueOptions<Result> {
  personId: string
  item: OutboxItem
  save: () => Promise<Result>
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
    const saving = save()
    let hasTimedOut = false

    const followLateSave = (): void => {
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

    const timer = setTimeout(() => {
      hasTimedOut = true
      enqueue(item).then(() => {
        resolve({ queued: true })
        followLateSave()
      }, reject)
    }, queueAfterMilliseconds)

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
        if (!isServerNotReachedError(error)) {
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
