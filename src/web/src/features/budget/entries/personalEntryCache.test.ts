import { QueryClient } from '@tanstack/react-query'
import { describe, expect, it } from 'vitest'
import {
  personalCategoriesQueryKey,
  personalCategoriesWithArchivedQueryKey,
  refreshPersonalCategories,
} from '../categories/personalCategoryCache'
import { personalEntriesQueryKey, refreshPersonalEntries } from './personalEntryCache'

function cacheWithEverything(): QueryClient {
  const queryClient = new QueryClient()
  queryClient.setQueryData(personalEntriesQueryKey('2026-10'), 'october')
  queryClient.setQueryData(personalEntriesQueryKey('2026-09'), 'september')
  queryClient.setQueryData(personalCategoriesQueryKey, 'categories')
  queryClient.setQueryData(personalCategoriesWithArchivedQueryKey, 'categories with archived')
  queryClient.setQueryData(['groups'], 'groups')
  return queryClient
}

function isStale(queryClient: QueryClient, queryKey: readonly unknown[]): boolean {
  return queryClient.getQueryState(queryKey)?.isInvalidated === true
}

describe('the personal entries cache', () => {
  it('keeps one month per key under personal, entries', () => {
    expect(personalEntriesQueryKey('2026-10')).toEqual(['personal', 'entries', '2026-10'])
  })

  it('refreshes every month of entries and nothing else', async () => {
    const queryClient = cacheWithEverything()

    await refreshPersonalEntries(queryClient)

    expect(isStale(queryClient, personalEntriesQueryKey('2026-10'))).toBe(true)
    expect(isStale(queryClient, personalEntriesQueryKey('2026-09'))).toBe(true)
    expect(isStale(queryClient, personalCategoriesQueryKey)).toBe(false)
    expect(isStale(queryClient, ['groups'])).toBe(false)
  })
})

describe('refreshPersonalCategories', () => {
  it('also refreshes every month of entries, because each entry carries a copy of its category', async () => {
    const queryClient = cacheWithEverything()

    await refreshPersonalCategories(queryClient)

    expect(isStale(queryClient, personalCategoriesQueryKey)).toBe(true)
    expect(isStale(queryClient, personalCategoriesWithArchivedQueryKey)).toBe(true)
    expect(isStale(queryClient, personalEntriesQueryKey('2026-10'))).toBe(true)
    expect(isStale(queryClient, personalEntriesQueryKey('2026-09'))).toBe(true)
    expect(isStale(queryClient, ['groups'])).toBe(false)
  })
})
