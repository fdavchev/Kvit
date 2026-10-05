import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import { getGroups, type GroupList } from '@/core/services/groups/groupsService'
import { groupsQueryKey } from '../../shared/groupCache'

export function useGroups(): UseQueryResult<GroupList> {
  return useQuery({ queryKey: groupsQueryKey, queryFn: getGroups })
}
