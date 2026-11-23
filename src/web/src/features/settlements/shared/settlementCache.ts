import type { QueryClient } from '@tanstack/react-query'
import { hasErrorCode } from '@/core/api/errors'
import { expensesQueryKey } from '@/features/expenses/shared/expenseCache'
import { activityQueryKey, balancesQueryKey, membersQueryKey, refreshGroupAndList } from '@/features/groups/shared/groupCache'

const settlementChangedMeanwhileCodes = ['SETTLEMENT_NOT_PENDING', 'SETTLEMENT_NOT_FOUND', 'SETTLEMENT_NOT_DELETED']

export function settlementsQueryKey(groupId: string): readonly ['groups', string, 'settlements'] {
  return ['groups', groupId, 'settlements']
}

export async function refreshAfterSettlementChange(queryClient: QueryClient, groupId: string): Promise<void> {
  await Promise.all([
    queryClient.invalidateQueries({ queryKey: balancesQueryKey(groupId) }),
    queryClient.invalidateQueries({ queryKey: settlementsQueryKey(groupId) }),
    queryClient.invalidateQueries({ queryKey: activityQueryKey(groupId) }),
    queryClient.invalidateQueries({ queryKey: expensesQueryKey(groupId) }),
    queryClient.invalidateQueries({ queryKey: membersQueryKey(groupId) }),
    refreshGroupAndList(queryClient, groupId),
  ])
}

export function refreshWhenSettlementChangedMeanwhile(queryClient: QueryClient, groupId: string, error: unknown): void {
  if (settlementChangedMeanwhileCodes.some((code) => hasErrorCode(error, code))) {
    void refreshAfterSettlementChange(queryClient, groupId)
  }
}
