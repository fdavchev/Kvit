import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { confirmSettlement, rejectSettlement } from '@/core/services/settlements/settlementsService'
import { refreshAfterSettlementChange } from '../../shared/settlementCache'

export type SettlementAnswer = 'confirm' | 'reject'

export function useAnswerSettlement(
  groupId: string,
  settlementId: string,
): UseMutationResult<void, Error, SettlementAnswer> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (answer: SettlementAnswer) =>
      answer === 'confirm' ? confirmSettlement(groupId, settlementId) : rejectSettlement(groupId, settlementId),
    onSuccess: () => refreshAfterSettlementChange(queryClient, groupId),
  })
}
