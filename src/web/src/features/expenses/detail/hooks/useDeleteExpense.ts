import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { deleteExpense } from '@/core/services/expenses/expensesService'
import { refreshAfterExpenseDelete } from '../../shared/expenseCache'

export function useDeleteExpense(groupId: string, expenseId: string): UseMutationResult<void, Error, void> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => deleteExpense(groupId, expenseId),
    onSuccess: () => refreshAfterExpenseDelete(queryClient, groupId),
  })
}
