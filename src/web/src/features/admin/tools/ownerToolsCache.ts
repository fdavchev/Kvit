import type { QueryClient } from '@tanstack/react-query'

export const ownerToolsPeopleQueryKey = ['ownerTools', 'people'] as const
export const ownerToolsGroupQueryKey = ['ownerTools', 'group'] as const
export const ownerToolsLogQueryKey = ['ownerTools', 'log'] as const

export async function refreshPeopleAndLog(queryClient: QueryClient): Promise<void> {
  await Promise.all([
    queryClient.invalidateQueries({ queryKey: ownerToolsPeopleQueryKey }),
    queryClient.invalidateQueries({ queryKey: ownerToolsLogQueryKey }),
  ])
}

export async function refreshGroupAndLog(queryClient: QueryClient): Promise<void> {
  await Promise.all([
    queryClient.invalidateQueries({ queryKey: ownerToolsGroupQueryKey }),
    queryClient.invalidateQueries({ queryKey: ownerToolsLogQueryKey }),
  ])
}
