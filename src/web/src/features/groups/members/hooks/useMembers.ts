import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import { getMembers, type GroupMembers } from '@/core/services/groups/membersService'
import { membersQueryKey } from '../../shared/groupCache'

export function useMembers(groupId: string): UseQueryResult<GroupMembers> {
  return useQuery({ queryKey: membersQueryKey(groupId), queryFn: () => getMembers(groupId) })
}
