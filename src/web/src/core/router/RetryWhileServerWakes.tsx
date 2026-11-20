import { useQueryClient, type Query } from '@tanstack/react-query'
import { useEffect } from 'react'
import { useIsWaitingAfterWakeVisit } from '@/core/api/useIsWaitingAfterWakeVisit'

const retryWhileServerWakesMilliseconds = 3000

export function RetryWhileServerWakes(): null {
  const queryClient = useQueryClient()
  const isWaitingAfterWakeVisit = useIsWaitingAfterWakeVisit()

  useEffect(() => {
    if (!isWaitingAfterWakeVisit) {
      return
    }
    const timer = setInterval(() => {
      void queryClient.refetchQueries({ type: 'active', predicate: hasFailedAndStopped }, { cancelRefetch: false })
    }, retryWhileServerWakesMilliseconds)
    return () => clearInterval(timer)
  }, [isWaitingAfterWakeVisit, queryClient])

  return null
}

function hasFailedAndStopped(query: Query): boolean {
  return query.state.status === 'error' && query.state.fetchStatus === 'idle'
}
