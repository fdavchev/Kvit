import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import {
  getPersonalEntries,
  type PersonalEntryMonth,
} from '@/core/services/personalEntries/personalEntriesService'
import { personalEntriesQueryKey } from '../personalEntryCache'

export function usePersonalEntries(month: string): UseQueryResult<PersonalEntryMonth> {
  return useQuery({ queryKey: personalEntriesQueryKey(month), queryFn: () => getPersonalEntries(month) })
}
