import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { updateExpense, type ExpenseEdit } from '@/core/services/expenses/expensesService'
import { refreshAfterExpenseChange, refreshWhenExpenseChangedMeanwhile } from '../../shared/expenseCache'

export function useUpdateExpense(
  groupId: string,
  expenseId: string,
): UseMutationResult<void, Error, ExpenseEdit> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (edit: ExpenseEdit) => updateExpense(groupId, expenseId, edit),
    onSuccess: () => refreshAfterExpenseChange(queryClient, groupId),
    onError: (error) => refreshWhenExpenseChangedMeanwhile(queryClient, groupId, error),
  })
}
