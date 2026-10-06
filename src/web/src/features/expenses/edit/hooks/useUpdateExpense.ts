import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { updateExpense, type ExpenseChanges } from '@/core/services/expenses/expensesService'
import { refreshAfterExpenseChange } from '../../shared/expenseCache'

export function useUpdateExpense(
  groupId: string,
  expenseId: string,
): UseMutationResult<void, Error, ExpenseChanges> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (changes: ExpenseChanges) => updateExpense(groupId, expenseId, changes),
    onSuccess: () => refreshAfterExpenseChange(queryClient, groupId),
  })
}
