import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import type { AnsweredSignal } from '@/core/outbox/saveOrQueue'
import {
  createOneBill,
  type CreatedOneBill,
  type OneBillInput,
} from '@/core/services/expenses/expensesService'
import { refreshGroupList } from '@/features/groups/shared/groupCache'
import { withAnswered } from '@/features/outbox/withAnswered'

export function useCreateOneBill(): UseMutationResult<CreatedOneBill, Error, OneBillInput & AnsweredSignal> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: withAnswered<OneBillInput & AnsweredSignal, CreatedOneBill>(createOneBill),
    onSuccess: () => refreshGroupList(queryClient),
  })
}
