import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import {
  createExpense,
  type ExpenseDetail,
  type ExpenseInput,
} from '@/core/services/expenses/expensesService'
import { refreshExpenses } from '../../shared/expenseCache'

export function useCreateExpense(groupId: string): UseMutationResult<ExpenseDetail, Error, ExpenseInput> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: ExpenseInput) => createExpense(groupId, input),
    onSuccess: () => refreshExpenses(queryClient, groupId),
  })
}
