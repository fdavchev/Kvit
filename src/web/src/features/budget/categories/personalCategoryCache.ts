import type { QueryClient } from '@tanstack/react-query'

export const personalCategoriesQueryKey = ['personal', 'categories'] as const

export const personalCategoriesWithArchivedQueryKey = ['personal', 'categories', 'withArchived'] as const

export function refreshPersonalCategories(queryClient: QueryClient): Promise<void> {
  return queryClient.invalidateQueries({ queryKey: personalCategoriesQueryKey })
}
