import { useState } from 'react'
import { useSignedInMe } from '@/core/auth/useSignedInMe'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { runWithoutWaiting } from '@/core/outbox/outboxSender'
import { saveOrQueue, type SaveOrQueueOutcome } from '@/core/outbox/saveOrQueue'
import { useOutboxSender } from '@/core/outbox/useOutbox'

export interface QueuedSave {
  saveOrQueueItem: <Result>(item: OutboxItem, save: () => Promise<Result>) => Promise<SaveOrQueueOutcome<Result>>
  isPending: boolean
  error: Error | null
}

export function useQueuedSave(): QueuedSave {
  const sender = useOutboxSender()
  const me = useSignedInMe()
  const [isPending, setIsPending] = useState<boolean>(false)
  const [error, setError] = useState<Error | null>(null)

  async function saveOrQueueItem<Result>(
    item: OutboxItem,
    save: () => Promise<Result>,
  ): Promise<SaveOrQueueOutcome<Result>> {
    setIsPending(true)
    setError(null)
    try {
      const outcome = await saveOrQueue({
        personId: me.id,
        item,
        save,
        enqueue: (queuedItem) => sender.enqueue(queuedItem),
        onLateSuccess: async () => {
          await sender.discard(item.id)
        },
      })
      if (outcome.queued) {
        runWithoutWaiting(sender)
      }
      return outcome
    } catch (failure) {
      setError(asSaveError(failure))
      throw failure
    } finally {
      setIsPending(false)
    }
  }

  return { saveOrQueueItem, isPending, error }
}

export function asSaveError(failure: unknown): Error {
  if (failure instanceof Error) {
    return failure
  }
  return new Error(`The save failed with a value that is not an Error: ${String(failure)}`, { cause: failure })
}

export function leaveErrorToQueuedSave(): void {}
