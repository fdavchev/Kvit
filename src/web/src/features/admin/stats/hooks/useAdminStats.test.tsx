import { dehydrate } from '@tanstack/react-query'
import { waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { savedQueryCacheOptions } from '@/core/api/savedQueryCache'
import { meQueryKey } from '@/core/auth/useMe'
import { allTimeStats, novemberStats } from '@/test/adminStatsTestData'
import { problemResponse, requestCountTo, stubFetch, stubFetchByPath } from '@/test/apiTestHelpers'
import { restoreRealRetryPolicy } from '@/test/expenseTestHelpers'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { seedMe, testAdminMe } from '@/test/testMe'
import { createTestPersister } from '@/test/testPersister'
import { useAdminStats } from './useAdminStats'

const novemberUrl = '/api/admin/stats?from=2026-11-01&to=2026-11-04&by=day'
const novemberRequest = { from: '2026-11-01', to: '2026-11-04', by: 'day' } as const

describe('useAdminStats', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('is pending first and then holds the statistics', async () => {
    stubFetch(Response.json(novemberStats))

    const { result } = await renderHookWithProviders(() => useAdminStats(novemberRequest))

    expect(result.current.isPending).toBe(true)
    await waitFor(() => {
      expect(result.current.data).toEqual(novemberStats)
    })
  })

  it('asks the server once, for the range and the grouping it is given', async () => {
    const fetchMock = stubFetchByPath({
      [novemberUrl]: Response.json(novemberStats),
      '/api/admin/stats?by=month': Response.json(allTimeStats),
    })

    const { result } = await renderHookWithProviders(() => useAdminStats(novemberRequest))
    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(requestCountTo(fetchMock, novemberUrl)).toBe(1)
    expect(requestCountTo(fetchMock, '/api/admin/stats?by=month')).toBe(0)
  })

  it('asks for the grouping alone when no range is given', async () => {
    const fetchMock = stubFetchByPath({ '/api/admin/stats?by=month': Response.json(allTimeStats) })

    const { result } = await renderHookWithProviders(() => useAdminStats({ by: 'month' }))
    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(requestCountTo(fetchMock, '/api/admin/stats?by=month')).toBe(1)
  })

  it('holds the error with its code when the range is refused', async () => {
    stubFetch(problemResponse(400, 'ADMIN_STATS_DAY_RANGE_TOO_LONG'))

    const { result } = await renderHookWithProviders(() => useAdminStats(novemberRequest))

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
    expect(result.current.error).toMatchObject({ httpStatus: 400, errorCode: 'ADMIN_STATS_DAY_RANGE_TOO_LONG' })
  })

  it('holds the error naming the field when the answer cannot be read', async () => {
    stubFetch(Response.json({ ...novemberStats, buckets: null }))

    const { result } = await renderHookWithProviders(() => useAdminStats(novemberRequest))

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
    expect(result.current.error?.message).toContain('buckets')
  })

  it.each([
    [400, 'ADMIN_STATS_DAY_RANGE_TOO_LONG'],
    [403, null],
  ])('asks only once and shows the error at once for a %s answer, with the retry policy of the app', async (status, errorCode) => {
    const fetchMock = stubFetch(errorCode === null ? new Response(null, { status }) : problemResponse(status, errorCode))

    const { result } = await renderHookWithProviders(() => useAdminStats(novemberRequest), {
      seedCache: restoreRealRetryPolicy,
    })

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
    expect(requestCountTo(fetchMock, novemberUrl)).toBe(1)
  })

  it('is left out of the saved copy in the browser, while the signed-in person is saved', async () => {
    stubFetch(Response.json(novemberStats))
    const { result, queryClient } = await renderHookWithProviders(() => useAdminStats(novemberRequest), {
      seedCache: seedMe(testAdminMe),
    })
    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    const options = savedQueryCacheOptions(queryClient, createTestPersister())
    const savedKeys = dehydrate(queryClient, options.dehydrateOptions).queries.map((query) => query.queryKey)

    expect(queryClient.getQueryCache().getAll()).toHaveLength(2)
    expect(savedKeys).toEqual([meQueryKey])
  })
})
