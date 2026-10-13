import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { restoreSettlement } from '@/core/services/settlements/settlementsService'
import { refreshAfterSettlementChange } from '../../shared/settlementCache'

export function useRestoreSettlement(groupId: string): UseMutationResult<void, Error, string> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (settlementId: string) => restoreSettlement(groupId, settlementId),
    onSuccess: () => refreshAfterSettlementChange(queryClient, groupId),
  })
}
