import { keepPreviousData, useQuery, type UseQueryResult } from '@tanstack/react-query'
import { keptOutOfSavedCopy } from '@/core/api/savedQueryCache'
import { getAdminStats, type AdminStats, type AdminStatsQuery } from '@/core/services/adminStats/adminStatsService'

export function useAdminStats(query: AdminStatsQuery): UseQueryResult<AdminStats> {
  return useQuery({
    queryKey: ['adminStats', query.from ?? null, query.to ?? null, query.by],
    queryFn: () => getAdminStats(query),
    placeholderData: keepPreviousData,
    meta: keptOutOfSavedCopy,
  })
}
