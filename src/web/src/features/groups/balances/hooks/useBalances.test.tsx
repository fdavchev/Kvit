import { waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { problemResponse, sentRequest, stubFetch } from '@/test/apiTestHelpers'
import { balancesPath, greeceTripBalances } from '@/test/balancesTestData'
import { testGroupId } from '@/test/groupTestData'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { useBalances } from './useBalances'

const otherGroupId = 'f0e1d2c3-b4a5-4968-8776-655443322110'

describe('useBalances', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('loads the balances from GET /api/groups/{id}/balances', async () => {
    const fetchMock = stubFetch(Response.json(greeceTripBalances))

    const { result } = await renderHookWithProviders(() => useBalances(testGroupId))

    await waitFor(() => {
      expect(result.current.data).toEqual(greeceTripBalances)
    })
    expect(sentRequest(fetchMock).url).toBe(balancesPath)
  })

  it('keeps the balances in the cache under the key ["groups", id, "balances"]', async () => {
    stubFetch(Response.json(greeceTripBalances))

    const { result, queryClient } = await renderHookWithProviders(() => useBalances(testGroupId))

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })
    expect(queryClient.getQueryData(['groups', testGroupId, 'balances'])).toEqual(greeceTripBalances)
  })

  it('keeps each group under its own key', async () => {
    stubFetch(Response.json(greeceTripBalances))

    const { result, queryClient } = await renderHookWithProviders(() => useBalances(otherGroupId))

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })
    expect(queryClient.getQueryData(['groups', otherGroupId, 'balances'])).toEqual(greeceTripBalances)
    expect(queryClient.getQueryData(['groups', testGroupId, 'balances'])).toBeUndefined()
  })

  it('is pending before the server has answered', async () => {
    stubFetch(Response.json(greeceTripBalances))

    const { result } = await renderHookWithProviders(() => useBalances(testGroupId))

    expect(result.current.isPending).toBe(true)
    expect(result.current.data).toBeUndefined()
  })

  it('shows the balances that are already in the cache straight away', async () => {
    stubFetch(Response.json(greeceTripBalances))

    const { result } = await renderHookWithProviders(() => useBalances(testGroupId), {
      seedCache: (queryClient) => {
        queryClient.setQueryData(['groups', testGroupId, 'balances'], greeceTripBalances)
      },
    })

    expect(result.current.data).toEqual(greeceTripBalances)
  })

  it('exposes the ApiError with its error code when the group is not found', async () => {
    stubFetch(problemResponse(404, 'GROUP_NOT_FOUND'))

    const { result } = await renderHookWithProviders(() => useBalances(testGroupId))

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
    expect(result.current.error).toBeInstanceOf(ApiError)
    expect(result.current.error).toMatchObject({ httpStatus: 404, errorCode: 'GROUP_NOT_FOUND' })
  })

  it('exposes an error naming the field when the answer is malformed', async () => {
    stubFetch(Response.json({ ...greeceTripBalances, isEveryoneKvit: 'maybe' }))

    const { result } = await renderHookWithProviders(() => useBalances(testGroupId))

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
    expect(result.current.error?.message).toContain('isEveryoneKvit')
  })
})
