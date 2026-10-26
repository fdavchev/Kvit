import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import { getPersonalSummary, type PersonalSummary } from '@/core/services/personalSummary/personalSummaryService'
import { personalSummaryQueryKey } from '../personalSummaryCache'

export function usePersonalSummary(month: string): UseQueryResult<PersonalSummary> {
  return useQuery({ queryKey: personalSummaryQueryKey(month), queryFn: () => getPersonalSummary(month) })
}
