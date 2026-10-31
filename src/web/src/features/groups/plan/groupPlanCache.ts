import type { QueryClient } from '@tanstack/react-query'
import type { Group } from '@/core/services/groups/groupsService'
import { groupQueryKey, spendingQueryKey } from '../shared/groupCache'

type GroupPlanFields = Pick<Group, 'planAmountMinor' | 'planCurrency'>

export async function showChangedPlan(queryClient: QueryClient, groupId: string, plan: GroupPlanFields): Promise<void> {
  const group = queryClient.getQueryData<Group>(groupQueryKey(groupId))
  if (group !== undefined) {
    queryClient.setQueryData<Group>(groupQueryKey(groupId), { ...group, ...plan })
  }
  await queryClient.invalidateQueries({ queryKey: spendingQueryKey(groupId), exact: true })
}
