import { waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { problemResponse, sentRequest, stubFetch } from '@/test/apiTestHelpers'
import {
  anaPaidFilipPending,
  markoPaidFilipPending,
  settlementsPath,
} from '@/test/balancesTestData'
import { testGroupId } from '@/test/groupTestData'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { useSettlements } from './useSettlements'

const otherGroupId = 'f0e1d2c3-b4a5-4968-8776-655443322110'
const settlements = [markoPaidFilipPending, anaPaidFilipPending]

describe('useSettlements', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('loads the payments from GET /api/groups/{id}/settlements', async () => {
    const fetchMock = stubFetch(Response.json({ settlements }))

    const { result } = await renderHookWithProviders(() => useSettlements(testGroupId))

    await waitFor(() => {
      expect(result.current.data).toEqual(settlements)
    })
    expect(sentRequest(fetchMock).url).toBe(settlementsPath)
  })

  it('keeps the list of payments in the cache under the key ["groups", id, "settlements"]', async () => {
    stubFetch(Response.json({ settlements }))

    const { result, queryClient } = await renderHookWithProviders(() => useSettlements(testGroupId))

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })
    expect(queryClient.getQueryData(['groups', testGroupId, 'settlements'])).toEqual(settlements)
  })

  it('keeps each group under its own key', async () => {
    stubFetch(Response.json({ settlements }))

    const { result, queryClient } = await renderHookWithProviders(() => useSettlements(otherGroupId))

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })
    expect(queryClient.getQueryData(['groups', otherGroupId, 'settlements'])).toEqual(settlements)
    expect(queryClient.getQueryData(['groups', testGroupId, 'settlements'])).toBeUndefined()
  })

  it('is pending before the server has answered', async () => {
    stubFetch(Response.json({ settlements }))

    const { result } = await renderHookWithProviders(() => useSettlements(testGroupId))

    expect(result.current.isPending).toBe(true)
    expect(result.current.data).toBeUndefined()
  })

  it('shows the payments that are already in the cache straight away', async () => {
    stubFetch(Response.json({ settlements }))

    const { result } = await renderHookWithProviders(() => useSettlements(testGroupId), {
      seedCache: (queryClient) => {
        queryClient.setQueryData(['groups', testGroupId, 'settlements'], settlements)
      },
    })

    expect(result.current.data).toEqual(settlements)
  })

  it('exposes the ApiError with its error code when the group is not found', async () => {
    stubFetch(problemResponse(404, 'GROUP_NOT_FOUND'))

    const { result } = await renderHookWithProviders(() => useSettlements(testGroupId))

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
    expect(result.current.error).toBeInstanceOf(ApiError)
    expect(result.current.error).toMatchObject({ httpStatus: 404, errorCode: 'GROUP_NOT_FOUND' })
  })

  it('exposes an error naming the field when the answer is malformed', async () => {
    stubFetch(Response.json({ settlements: [{ ...markoPaidFilipPending, status: 'Done' }] }))

    const { result } = await renderHookWithProviders(() => useSettlements(testGroupId))

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
    expect(result.current.error?.message).toContain('status')
  })
})
