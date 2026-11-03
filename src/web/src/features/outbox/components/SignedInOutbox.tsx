import { useQueryClient, type QueryClient } from '@tanstack/react-query'
import { useEffect, useMemo, type ReactNode } from 'react'
import { ApiError } from '@/core/api/apiClient'
import { reactToFailedRequest } from '@/core/api/queryClient'
import { useSignedInMe } from '@/core/auth/useSignedInMe'
import { OutboxProvider } from '@/core/outbox/OutboxProvider'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { createOutboxSender, type OutboxSender } from '@/core/outbox/outboxSender'
import { startOutboxTriggers } from '@/core/outbox/outboxTriggers'
import { refreshAfterQueuedItem, sendQueuedItem } from '../outboxDispatch'

interface SignedInOutboxProps {
  children: ReactNode
}

export function SignedInOutbox({ children }: SignedInOutboxProps) {
  const queryClient = useQueryClient()
  const personId = useSignedInMe().id
  const sender = useMemo(() => createSignedInSender(queryClient, personId), [queryClient, personId])

  useEffect(() => {
    sender.resume()
    const stopTriggers = startOutboxTriggers(sender, { document, window })
    return () => {
      stopTriggers()
      sender.pause()
    }
  }, [sender])

  return <OutboxProvider sender={sender}>{children}</OutboxProvider>
}

function createSignedInSender(queryClient: QueryClient, personId: string): OutboxSender {
  return createOutboxSender({
    personId,
    sendItem: async (item: OutboxItem) => {
      try {
        return await sendQueuedItem(item)
      } catch (error) {
        if (error instanceof ApiError) {
          reactToFailedRequest(queryClient, error)
        }
        throw error
      }
    },
    afterSent: (item) => refreshAfterQueuedItem(queryClient, item),
  })
}
