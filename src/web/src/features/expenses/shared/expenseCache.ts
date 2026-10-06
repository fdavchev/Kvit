import type { QueryClient } from '@tanstack/react-query'
import { activityQueryKey } from '@/features/groups/shared/groupCache'

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
    queryClient.invalidateQueries({ queryKey: activityQueryKey(groupId) }),
  ])
}
