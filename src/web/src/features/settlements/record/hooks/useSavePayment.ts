import { useState } from 'react'
import type { GroupMember } from '@/core/services/groups/membersService'
import type { SettlementInput } from '@/core/services/settlements/settlementsService'
import { leaveErrorToQueuedSave, useQueuedSave } from '@/features/outbox/hooks/useQueuedSave'
import { buildSettlementOutboxItem } from '@/features/outbox/queuedItems'
import { countsRightAway } from '../paymentToRecord'
import { useRecordSettlement } from './useRecordSettlement'

export interface PaymentNames {
  payerName: string
  receiverName: string
}

export interface PaymentSave {
  save: (input: SettlementInput, names: PaymentNames, onSaved: () => void) => void
  isPending: boolean
  error: Error | null
}

export function useSavePayment(groupId: string, members: readonly GroupMember[]): PaymentSave {
  const recordSettlement = useRecordSettlement(groupId)
  const queuedSave = useQueuedSave()
  const [isThroughOutbox, setIsThroughOutbox] = useState<boolean>(false)

  function save(input: SettlementInput, names: PaymentNames, onSaved: () => void): void {
    const canWaitInOutbox = countsRightAway(input.fromMemberId, input.toMemberId, members)
    setIsThroughOutbox(canWaitInOutbox)
    if (!canWaitInOutbox) {
      recordSettlement.mutate(input, { onSuccess: onSaved })
      return
    }
    const item = buildSettlementOutboxItem({ groupId, input, ...names })
    void queuedSave
      .saveOrQueueItem(item, () => recordSettlement.mutateAsync(input))
      .then(onSaved, leaveErrorToQueuedSave)
  }

  return {
    save,
    isPending: isThroughOutbox ? queuedSave.isPending : recordSettlement.isPending,
    error: isThroughOutbox ? queuedSave.error : recordSettlement.error,
  }
}
