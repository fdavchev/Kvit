import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import { getMe, type Me } from '@/core/services/me/meService'

export const meQueryKey = ['me'] as const

export function useMe(): UseQueryResult<Me | null> {
  return useQuery({ queryKey: meQueryKey, queryFn: getMe, staleTime: Infinity })
}
