import { useIsFetching, type Query } from '@tanstack/react-query'
import { meQueryKey } from '@/core/auth/useMe'

export function useIsRefreshingSavedData(): boolean {
  return (
    useIsFetching({ queryKey: meQueryKey, exact: true, predicate: isSignedInPersonFromSavedCopy }) > 0
  )
}

function isSignedInPersonFromSavedCopy(query: Query): boolean {
  const { data, dataUpdatedAt, errorUpdatedAt } = query.state
  const pageOpenedAt = performance.timeOrigin
  return (
    data !== null &&
    data !== undefined &&
    dataUpdatedAt < pageOpenedAt &&
    errorUpdatedAt < pageOpenedAt
  )
}
