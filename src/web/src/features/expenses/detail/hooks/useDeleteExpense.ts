import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { deleteExpense } from '@/core/services/expenses/expensesService'
import { activityQueryKey } from '@/features/groups/shared/groupCache'
import { expensesQueryKey } from '../../shared/expenseCache'

export function useDeleteExpense(groupId: string, expenseId: string): UseMutationResult<void, Error, void> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => deleteExpense(groupId, expenseId),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: expensesQueryKey(groupId), refetchType: 'none' }),
        queryClient.invalidateQueries({ queryKey: activityQueryKey(groupId) }),
      ])
    },
  })
}
