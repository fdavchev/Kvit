import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { cancelSettlement } from '@/core/services/settlements/settlementsService'
import { refreshAfterSettlementChange } from '../../shared/settlementCache'

export function useCancelSettlement(groupId: string, settlementId: string): UseMutationResult<void, Error, void> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => cancelSettlement(groupId, settlementId),
    onSuccess: () => refreshAfterSettlementChange(queryClient, groupId),
  })
}
