import type { QueryClient } from '@tanstack/react-query'

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

export async function refreshExpenses(queryClient: QueryClient, groupId: string): Promise<void> {
  await queryClient.invalidateQueries({ queryKey: expensesQueryKey(groupId) })
}
