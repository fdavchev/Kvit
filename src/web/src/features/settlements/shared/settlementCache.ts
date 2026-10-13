import type { QueryClient } from '@tanstack/react-query'
import { expensesQueryKey } from '@/features/expenses/shared/expenseCache'
import { activityQueryKey, balancesQueryKey, membersQueryKey } from '@/features/groups/shared/groupCache'

export function settlementsQueryKey(groupId: string): readonly ['groups', string, 'settlements'] {
  return ['groups', groupId, 'settlements']
}

export async function refreshAfterSettlementChange(queryClient: QueryClient, groupId: string): Promise<void> {
  await Promise.all([
    queryClient.invalidateQueries({ queryKey: balancesQueryKey(groupId) }),
    queryClient.invalidateQueries({ queryKey: settlementsQueryKey(groupId) }),
    queryClient.invalidateQueries({ queryKey: activityQueryKey(groupId) }),
    queryClient.invalidateQueries({ queryKey: expensesQueryKey(groupId) }),
    queryClient.invalidateQueries({ queryKey: membersQueryKey(groupId) }),
  ])
}
