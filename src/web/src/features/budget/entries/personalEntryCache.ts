import type { QueryClient } from '@tanstack/react-query'

const personalEntriesFamilyKey = ['personal', 'entries'] as const

export function personalEntriesQueryKey(month: string): readonly ['personal', 'entries', string] {
  return [...personalEntriesFamilyKey, month]
}

export function refreshPersonalEntries(queryClient: QueryClient): Promise<void> {
  return queryClient.invalidateQueries({ queryKey: personalEntriesFamilyKey })
}
