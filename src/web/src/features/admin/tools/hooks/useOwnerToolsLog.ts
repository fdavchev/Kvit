import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import { keptOutOfSavedCopy } from '@/core/api/savedQueryCache'
import { loadLog, type OwnerToolsLogEntry } from '@/core/services/ownerTools/ownerToolsService'
import { ownerToolsLogQueryKey } from '../ownerToolsCache'

export function useOwnerToolsLog(): UseQueryResult<OwnerToolsLogEntry[]> {
  return useQuery({
    queryKey: ownerToolsLogQueryKey,
    queryFn: loadLog,
    meta: keptOutOfSavedCopy,
  })
}
