import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import type { AnsweredSignal } from '@/core/outbox/saveOrQueue'
import {
  recordSettlement,
  type Settlement,
  type SettlementInput,
} from '@/core/services/settlements/settlementsService'
import { withAnswered } from '@/features/outbox/withAnswered'
import { refreshAfterSettlementChange } from '../../shared/settlementCache'

export function useRecordSettlement(groupId: string): UseMutationResult<Settlement, Error, SettlementInput & AnsweredSignal> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: withAnswered<SettlementInput & AnsweredSignal, Settlement>((input) => recordSettlement(groupId, input)),
    onSuccess: () => refreshAfterSettlementChange(queryClient, groupId),
  })
}
