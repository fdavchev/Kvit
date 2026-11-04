import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import type { AnsweredSignal } from '@/core/outbox/saveOrQueue'
import {
  createExpense,
  type ExpenseDetail,
  type ExpenseInput,
} from '@/core/services/expenses/expensesService'
import { withAnswered } from '@/features/outbox/withAnswered'
import { refreshAfterExpenseChange } from '../../shared/expenseCache'

export function useCreateExpense(groupId: string): UseMutationResult<ExpenseDetail, Error, ExpenseInput & AnsweredSignal> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: withAnswered<ExpenseInput & AnsweredSignal, ExpenseDetail>((input) => createExpense(groupId, input)),
    onSuccess: () => refreshAfterExpenseChange(queryClient, groupId),
  })
}
