import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import { getMe, type Me } from '@/core/services/me/meService'
import { meQueryKey } from './useMe'

export function useCachedMe(): UseQueryResult<Me | null> {
  return useQuery({ queryKey: meQueryKey, queryFn: getMe, staleTime: Infinity, enabled: false })
}
