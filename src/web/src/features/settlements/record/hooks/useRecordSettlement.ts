import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import {
  recordSettlement,
  type Settlement,
  type SettlementInput,
} from '@/core/services/settlements/settlementsService'
import { refreshAfterSettlementChange } from '../../shared/settlementCache'

export function useRecordSettlement(groupId: string): UseMutationResult<Settlement, Error, SettlementInput> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: SettlementInput) => recordSettlement(groupId, input),
    onSuccess: () => refreshAfterSettlementChange(queryClient, groupId),
  })
}
