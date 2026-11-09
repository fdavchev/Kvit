import { skipToken, useQuery, type UseQueryResult } from '@tanstack/react-query'
import { keptOutOfSavedCopy } from '@/core/api/savedQueryCache'
import { findPeople, type OwnerToolsPerson } from '@/core/services/ownerTools/ownerToolsService'
import { ownerToolsPeopleQueryKey } from '../ownerToolsCache'

export function useOwnerToolsPeople(query: string | null): UseQueryResult<OwnerToolsPerson[]> {
  return useQuery({
    queryKey: [...ownerToolsPeopleQueryKey, query],
    queryFn: query === null ? skipToken : () => findPeople(query),
    meta: keptOutOfSavedCopy,
  })
}
