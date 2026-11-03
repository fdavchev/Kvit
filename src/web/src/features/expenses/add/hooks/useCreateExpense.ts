import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import type { AnsweredSignal } from '@/core/outbox/saveOrQueue'
import {
  createExpense,
  type ExpenseDetail,
  type ExpenseInput,
} from '@/core/services/expenses/expensesService'
import { refreshAfterExpenseChange } from '../../shared/expenseCache'

export function useCreateExpense(groupId: string): UseMutationResult<ExpenseDetail, Error, ExpenseInput & AnsweredSignal> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ onAnswered, ...input }: ExpenseInput & AnsweredSignal) => {
      const created = await createExpense(groupId, input)
      onAnswered?.()
      return created
    },
    onSuccess: () => refreshAfterExpenseChange(queryClient, groupId),
  })
}
