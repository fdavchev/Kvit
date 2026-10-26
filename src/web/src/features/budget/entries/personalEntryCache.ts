import type { QueryClient } from '@tanstack/react-query'
import { personalSummaryFamilyKey } from '../summary/personalSummaryCache'

export const personalEntriesFamilyKey =['personal', 'entries'] as const

export function personalEntriesQueryKey(month: string): readonly ['personal', 'entries', string] {
  return [...personalEntriesFamilyKey, month]
}

export async function refreshPersonalEntries(queryClient: QueryClient): Promise<void> {
  await Promise.all([
    queryClient.invalidateQueries({ queryKey: personalEntriesFamilyKey }),
    queryClient.invalidateQueries({ queryKey: personalSummaryFamilyKey }),
  ])
}
