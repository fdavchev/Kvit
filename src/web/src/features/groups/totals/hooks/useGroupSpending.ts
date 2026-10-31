import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import { getGroupSpending, type GroupSpending } from '@/core/services/groups/groupSpendingService'
import { spendingQueryKey } from '../../shared/groupCache'

export function useGroupSpending(groupId: string): UseQueryResult<GroupSpending> {
  return useQuery({ queryKey: spendingQueryKey(groupId), queryFn: () => getGroupSpending(groupId) })
}
