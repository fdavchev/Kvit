import { act, waitFor } from '@testing-library/react'
import type { QueryClient } from '@tanstack/react-query'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { stubFetch } from '@/test/apiTestHelpers'
import { coffeeCategory } from '@/test/personalCategoryTestData'
import { coffeeEntry, entryAnswerRow } from '@/test/personalEntryTestData'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import type { SaveOnce } from '../../categories/hooks/useSaveOnce'
import { personalSummaryQueryKey } from '../../summary/personalSummaryCache'
import { personalEntriesQueryKey } from '../personalEntryCache'
import { useAddPersonalEntry } from './useAddPersonalEntry'

const newCoffee = {
  kind: 'Spending',
  categoryId: coffeeCategory.id,
  amountMinor: 12000,
  currency: 'MKD',
  entryDate: '2026-10-19',
  note: null,
  clientRequestId: 'c0000000-0000-4000-8000-000000000001',
} as const

function seedEntriesAndSummaries(queryClient: QueryClient): void {
  queryClient.setQueryData(personalEntriesQueryKey('2026-10'), 'the October answer')
  queryClient.setQueryData(personalSummaryQueryKey('2026-10'), 'the October summary')
  queryClient.setQueryData(personalSummaryQueryKey('2026-09'), 'the September summary')
}

function freeToSave(): SaveOnce {
  return { tryStart: vi.fn<() => boolean>(() => true), finish: vi.fn<() => void>() }
}

function isInvalidated(queryClient: QueryClient, key: readonly unknown[]): boolean {
  return queryClient.getQueryState(key)?.isInvalidated === true
}

describe('useAddPersonalEntry', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('marks the cached entries and every cached summary as out of date once the server accepted the entry', async () => {
    stubFetch(Response.json(entryAnswerRow(coffeeEntry)))
    const { result, queryClient } = await renderHookWithProviders(() => useAddPersonalEntry(freeToSave()), {
      seedCache: seedEntriesAndSummaries,
    })
    const onDone = vi.fn<() => void>()

    act(() => {
      result.current.run(newCoffee, onDone, vi.fn())
    })

    await waitFor(() => {
      expect(onDone).toHaveBeenCalledOnce()
    })
    expect(isInvalidated(queryClient, personalEntriesQueryKey('2026-10'))).toBe(true)
    expect(isInvalidated(queryClient, personalSummaryQueryKey('2026-10'))).toBe(true)
    expect(isInvalidated(queryClient, personalSummaryQueryKey('2026-09'))).toBe(true)
  })
})
