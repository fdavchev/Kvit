import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import { getSettlements, type Settlement } from '@/core/services/settlements/settlementsService'
import { settlementsQueryKey } from '../../shared/settlementCache'

export function useSettlements(groupId: string): UseQueryResult<Settlement[]> {
  return useQuery({ queryKey: settlementsQueryKey(groupId), queryFn: () => getSettlements(groupId) })
}
