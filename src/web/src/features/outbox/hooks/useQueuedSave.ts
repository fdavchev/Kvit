import { useState } from 'react'
import { useSignedInMe } from '@/core/auth/useSignedInMe'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { runWithoutWaiting } from '@/core/outbox/outboxSender'
import { saveOrQueue, type SaveOrQueueOutcome, type SaveTellingWhenAnswered } from '@/core/outbox/saveOrQueue'
import { useOutboxSender } from '@/core/outbox/useOutbox'

export interface QueuedSave {
  saveOrQueueItem: <Result>(item: OutboxItem, save: SaveTellingWhenAnswered<Result>) => Promise<SaveOrQueueOutcome<Result>>
  saveOrQueueThen: <Result>(
    item: OutboxItem,
    save: SaveTellingWhenAnswered<Result>,
    onSaved: (outcome: SaveOrQueueOutcome<Result>) => void,
  ) => void
  isPending: boolean
  error: Error | null
}

type SaveAttempt<Result> = { hasFailed: false; outcome: SaveOrQueueOutcome<Result> } | { hasFailed: true; failure: unknown }

export function useQueuedSave(): QueuedSave {
  const sender = useOutboxSender()
  const me = useSignedInMe()
  const [isPending, setIsPending] = useState<boolean>(false)
  const [error, setError] = useState<Error | null>(null)

  async function attemptSave<Result>(item: OutboxItem, save: SaveTellingWhenAnswered<Result>): Promise<SaveAttempt<Result>> {
    setIsPending(true)
    setError(null)
    let saving: Promise<Result> | undefined
    let isStillSaving = true
    const stopSaving = (): void => {
      isStillSaving = false
    }
    try {
      const outcome = await saveOrQueue({
        personId: me.id,
        item,
        save: (onAnswered) => {
          saving = save(onAnswered)
          saving.then(stopSaving, stopSaving)
          return saving
        },
        enqueue: (queuedItem) => sender.enqueue(queuedItem),
        onLateSuccess: async () => {
          await sender.discard(item.id)
        },
      })
      if (outcome.queued) {
        if (isStillSaving && saving !== undefined) {
          sender.holdWhileSaving(item.id, saving)
        }
        runWithoutWaiting(sender, { countsAsRetry: false })
      }
      return { hasFailed: false, outcome }
    } catch (failure) {
      setError(asSaveError(failure))
      return { hasFailed: true, failure }
    } finally {
      setIsPending(false)
    }
  }

  async function saveOrQueueItem<Result>(
    item: OutboxItem,
    save: SaveTellingWhenAnswered<Result>,
  ): Promise<SaveOrQueueOutcome<Result>> {
    const attempt = await attemptSave(item, save)
    if (attempt.hasFailed) {
      throw attempt.failure
    }
    return attempt.outcome
  }

  function saveOrQueueThen<Result>(
    item: OutboxItem,
    save: SaveTellingWhenAnswered<Result>,
    onSaved: (outcome: SaveOrQueueOutcome<Result>) => void,
  ): void {
    attemptSave(item, save)
      .then((attempt) => {
        if (!attempt.hasFailed) {
          onSaved(attempt.outcome)
        }
      })
      .catch((failure: unknown) => {
        console.error(`The ${item.kind} ${item.id} was saved or queued, but finishing the screen after it failed`, failure)
      })
  }

  return { saveOrQueueItem, saveOrQueueThen, isPending, error }
}

export function asSaveError(failure: unknown): Error {
  if (failure instanceof Error) {
    return failure
  }
  return new Error(`The save failed with a value that is not an Error: ${String(failure)}`, { cause: failure })
}
