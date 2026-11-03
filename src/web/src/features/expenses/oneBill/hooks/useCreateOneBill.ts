import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import type { AnsweredSignal } from '@/core/outbox/saveOrQueue'
import {
  createOneBill,
  type CreatedOneBill,
  type OneBillInput,
} from '@/core/services/expenses/expensesService'
import { refreshGroupList } from '@/features/groups/shared/groupCache'

export function useCreateOneBill(): UseMutationResult<CreatedOneBill, Error, OneBillInput & AnsweredSignal> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ onAnswered, ...input }: OneBillInput & AnsweredSignal) => {
      const created = await createOneBill(input)
      onAnswered?.()
      return created
    },
    onSuccess: () => refreshGroupList(queryClient),
  })
}
