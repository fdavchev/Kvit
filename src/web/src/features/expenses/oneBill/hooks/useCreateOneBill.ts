import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import {
  createOneBill,
  type CreatedOneBill,
  type OneBillInput,
} from '@/core/services/expenses/expensesService'
import { groupsQueryKey } from '@/features/groups/shared/groupCache'

export function useCreateOneBill(): UseMutationResult<CreatedOneBill, Error, OneBillInput> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: createOneBill,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: groupsQueryKey, exact: true })
    },
  })
}
