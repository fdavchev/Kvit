import { act, waitFor } from '@testing-library/react'
import type { QueryClient } from '@tanstack/react-query'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import {
  captureError,
  problemResponse,
  sentRequest,
  stubFetch,
} from '@/test/apiTestHelpers'
import { groupOf, testGroup, testGroupId } from '@/test/groupTestData'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { useChangeCountsInBudget } from './useChangeCountsInBudget'

const otherGroup = groupOf({ id: 'b5a1c2d3-0000-4000-8000-000000000009', name: 'Flat 4B' })

function seedTwoGroups(queryClient: QueryClient): void {
  queryClient.setQueryData(['groups', testGroupId], testGroup)
  queryClient.setQueryData(['groups', otherGroup.id], otherGroup)
}

describe('useChangeCountsInBudget', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends PUT /api/groups/{id}/members/me/counts-in-budget with the chosen answer', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    const { result } = await renderHookWithProviders(() => useChangeCountsInBudget(testGroupId), {
      seedCache: seedTwoGroups,
    })

    await act(() => result.current.mutateAsync(false))

    expect(sentRequest(fetchMock)).toMatchObject({
      url: `/api/groups/${testGroupId}/members/me/counts-in-budget`,
      method: 'PUT',
      body: { countsInBudget: false },
    })
  })

  it.each([false, true])('puts countsInBudget %j in the cached group once the server accepted it, leaving the other fields alone', async (countsInBudget) => {
    stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(
      () => useChangeCountsInBudget(testGroupId),
      {
        seedCache: (client) => {
          client.setQueryData(['groups', testGroupId], { ...testGroup, countsInBudget: !countsInBudget })
        },
      },
    )

    await act(() => result.current.mutateAsync(countsInBudget))

    expect(queryClient.getQueryData(['groups', testGroupId])).toEqual({ ...testGroup, countsInBudget })
  })

  it('leaves the cached group of another group alone', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(() => useChangeCountsInBudget(testGroupId), {
      seedCache: seedTwoGroups,
    })

    await act(() => result.current.mutateAsync(false))

    expect(queryClient.getQueryData(['groups', otherGroup.id])).toEqual(otherGroup)
  })

  it('leaves the cached group as it was before the server answered', async () => {
    const { result, queryClient } = await renderHookWithProviders(() => useChangeCountsInBudget(testGroupId), {
      seedCache: seedTwoGroups,
    })
    let cachedWhenAsked: unknown
    vi.stubGlobal(
      'fetch',
      vi.fn<typeof fetch>(async () => {
        cachedWhenAsked = queryClient.getQueryData(['groups', testGroupId])
        return new Response(null, { status: 204 })
      }),
    )

    await act(() => result.current.mutateAsync(false))

    expect(cachedWhenAsked).toEqual(testGroup)
  })

  it('keeps the cached group and exposes the ApiError when the server refuses', async () => {
    stubFetch(problemResponse(400, 'GROUP_FINISHED'))
    const { result, queryClient } = await renderHookWithProviders(() => useChangeCountsInBudget(testGroupId), {
      seedCache: seedTwoGroups,
    })

    const error = await act(() => captureError(result.current.mutateAsync(false)))

    expect(error).toBeInstanceOf(ApiError)
    await waitFor(() => {
      expect(result.current.error).toBe(error)
    })
    expect(queryClient.getQueryData(['groups', testGroupId])).toEqual(testGroup)
  })
})
