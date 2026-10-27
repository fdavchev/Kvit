import type { QueryClient } from '@tanstack/react-query'
import {
  activityQueryKey,
  balancesQueryKey,
  refreshGroupAndList,
  spendingQueryKey,
} from '@/features/groups/shared/groupCache'

export const categoriesQueryKey = ['categories'] as const

export function expensesQueryKey(groupId: string): readonly ['groups', string, 'expenses'] {
  return ['groups', groupId, 'expenses']
}

export function expenseQueryKey(
  groupId: string,
  expenseId: string,
): readonly ['groups', string, 'expenses', string] {
  return ['groups', groupId, 'expenses', expenseId]
}

export function deletedExpensesQueryKey(
  groupId: string,
): readonly ['groups', string, 'expenses', 'deleted'] {
  return ['groups', groupId, 'expenses', 'deleted']
}

export async function refreshAfterExpenseChange(queryClient: QueryClient, groupId: string): Promise<void> {
  await Promise.all([
    queryClient.invalidateQueries({ queryKey: expensesQueryKey(groupId) }),
    refreshGroupAfterExpenseChange(queryClient, groupId),
  ])
}

export async function refreshAfterExpenseDelete(queryClient: QueryClient, groupId: string): Promise<void> {
  await Promise.all([
    queryClient.invalidateQueries({ queryKey: expensesQueryKey(groupId), refetchType: 'none' }),
    refreshGroupAfterExpenseChange(queryClient, groupId),
  ])
}

async function refreshGroupAfterExpenseChange(queryClient: QueryClient, groupId: string): Promise<void> {
  await Promise.all([
    queryClient.invalidateQueries({ queryKey: activityQueryKey(groupId) }),
    queryClient.invalidateQueries({ queryKey: balancesQueryKey(groupId) }),
    queryClient.invalidateQueries({ queryKey: spendingQueryKey(groupId) }),
    refreshGroupAndList(queryClient, groupId),
  ])
}
