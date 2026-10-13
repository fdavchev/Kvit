import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import { getBalances, type GroupBalances } from '@/core/services/balances/balancesService'
import { balancesQueryKey } from '../../shared/groupCache'

export function useBalances(groupId: string): UseQueryResult<GroupBalances> {
  return useQuery({ queryKey: balancesQueryKey(groupId), queryFn: () => getBalances(groupId) })
}
