import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import { getGroup, type Group } from '@/core/services/groups/groupsService'
import { groupQueryKey } from '../../shared/groupCache'

export function useGroup(groupId: string): UseQueryResult<Group> {
  return useQuery({ queryKey: groupQueryKey(groupId), queryFn: () => getGroup(groupId) })
}
