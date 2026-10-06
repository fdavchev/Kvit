import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { restoreExpense } from '@/core/services/expenses/expensesService'
import { refreshExpenses } from '../expenseCache'

export function useRestoreExpense(groupId: string): UseMutationResult<void, Error, string> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (expenseId: string) => restoreExpense(groupId, expenseId),
    onSuccess: () => refreshExpenses(queryClient, groupId),
  })
}
