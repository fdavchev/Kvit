import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { deleteSettlement } from '@/core/services/settlements/settlementsService'
import { refreshAfterSettlementChange } from '../../shared/settlementCache'

export function useDeleteSettlement(groupId: string, settlementId: string): UseMutationResult<void, Error, void> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => deleteSettlement(groupId, settlementId),
    onSuccess: () => refreshAfterSettlementChange(queryClient, groupId),
  })
}
