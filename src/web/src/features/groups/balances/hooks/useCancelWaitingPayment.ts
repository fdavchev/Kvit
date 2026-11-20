import { useQueryClient } from '@tanstack/react-query'
import { useCancelSettlement } from '@/features/settlements/pending/hooks/useCancelSettlement'
import { refreshWhenSettlementChangedMeanwhile } from '@/features/settlements/shared/settlementCache'

export interface CancelWaitingPayment {
  cancel: () => void
  isPending: boolean
  error: Error | null
}

export function useCancelWaitingPayment(groupId: string, settlementId: string): CancelWaitingPayment {
  const queryClient = useQueryClient()
  const cancelSettlement = useCancelSettlement(groupId, settlementId)

  function cancel(): void {
    cancelSettlement.mutate(undefined, {
      onError: (error) => refreshWhenSettlementChangedMeanwhile(queryClient, groupId, error),
    })
  }

  return { cancel, isPending: cancelSettlement.isPending, error: cancelSettlement.error }
}
