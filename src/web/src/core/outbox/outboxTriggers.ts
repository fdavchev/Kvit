import { runWithoutWaiting, type OutboxSender } from './outboxSender'

export interface OutboxTriggerTargets {
  document: Document
  window: Window
}

export function startOutboxTriggers(
  sender: Pick<OutboxSender, 'run'>,
  { document, window }: OutboxTriggerTargets,
): () => void {
  const runSender = (): void => {
    runWithoutWaiting(sender)
  }
  const runSenderWhenVisible = (): void => {
    if (document.visibilityState === 'visible') {
      runSender()
    }
  }

  window.addEventListener('online', runSender)
  document.addEventListener('visibilitychange', runSenderWhenVisible)
  runSender()

  return () => {
    window.removeEventListener('online', runSender)
    document.removeEventListener('visibilitychange', runSenderWhenVisible)
  }
}
