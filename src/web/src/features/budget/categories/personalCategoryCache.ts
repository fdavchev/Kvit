import type { QueryClient } from '@tanstack/react-query'
import { refreshPersonalEntries } from '../entries/personalEntryCache'

export const personalCategoriesQueryKey = ['personal', 'categories'] as const

export const personalCategoriesWithArchivedQueryKey = ['personal', 'categories', 'withArchived'] as const

export async function refreshPersonalCategories(queryClient: QueryClient): Promise<void> {
  await Promise.all([
    queryClient.invalidateQueries({ queryKey: personalCategoriesQueryKey }),
    refreshPersonalEntries(queryClient),
  ])
}
