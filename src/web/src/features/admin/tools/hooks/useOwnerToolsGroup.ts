import { skipToken, useQuery, type UseQueryResult } from '@tanstack/react-query'
import { keptOutOfSavedCopy } from '@/core/api/savedQueryCache'
import { findGroup, type OwnerToolsGroup } from '@/core/services/ownerTools/ownerToolsService'
import { ownerToolsGroupQueryKey } from '../ownerToolsCache'

export function useOwnerToolsGroup(groupId: string | null): UseQueryResult<OwnerToolsGroup> {
  return useQuery({
    queryKey: [...ownerToolsGroupQueryKey, groupId],
    queryFn: groupId === null ? skipToken : () => findGroup(groupId),
    meta: keptOutOfSavedCopy,
  })
}
