import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import type { AnsweredSignal } from '@/core/outbox/saveOrQueue'
import {
  recordSettlement,
  type Settlement,
  type SettlementInput,
} from '@/core/services/settlements/settlementsService'
import { refreshAfterSettlementChange } from '../../shared/settlementCache'

export function useRecordSettlement(groupId: string): UseMutationResult<Settlement, Error, SettlementInput & AnsweredSignal> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ onAnswered, ...input }: SettlementInput & AnsweredSignal) => {
      const recorded = await recordSettlement(groupId, input)
      onAnswered?.()
      return recorded
    },
    onSuccess: () => refreshAfterSettlementChange(queryClient, groupId),
  })
}
