import { waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { problemResponse, sentRequest, stubFetch } from '@/test/apiTestHelpers'
import { testGroupId } from '@/test/groupTestData'
import { anaViewMembers, ownerViewMembers } from '@/test/memberTestData'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { useMembers } from './useMembers'

const otherGroupId = 'f0e1d2c3-b4a5-4968-8776-655443322110'

describe('useMembers', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('loads the members from GET /api/groups/{id}/members', async () => {
    const fetchMock = stubFetch(Response.json(ownerViewMembers))

    const { result } = await renderHookWithProviders(() => useMembers(testGroupId))

    await waitFor(() => {
      expect(result.current.data).toEqual(ownerViewMembers)
    })
    expect(sentRequest(fetchMock).url).toBe(`/api/groups/${testGroupId}/members`)
  })

  it('keeps the members in the cache under the key ["groups", id, "members"]', async () => {
    stubFetch(Response.json(anaViewMembers))

    const { result, queryClient } = await renderHookWithProviders(() => useMembers(testGroupId))

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })
    expect(queryClient.getQueryData(['groups', testGroupId, 'members'])).toEqual(anaViewMembers)
  })

  it('keeps each group under its own key', async () => {
    stubFetch(Response.json(ownerViewMembers))

    const { result, queryClient } = await renderHookWithProviders(() => useMembers(otherGroupId))

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })
    expect(queryClient.getQueryData(['groups', otherGroupId, 'members'])).toEqual(ownerViewMembers)
    expect(queryClient.getQueryData(['groups', testGroupId, 'members'])).toBeUndefined()
  })

  it('exposes the ApiError with its error code when the group is not found', async () => {
    stubFetch(problemResponse(404, 'GROUP_NOT_FOUND'))

    const { result } = await renderHookWithProviders(() => useMembers(testGroupId))

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
    expect(result.current.error).toBeInstanceOf(ApiError)
    expect(result.current.error).toMatchObject({ httpStatus: 404, errorCode: 'GROUP_NOT_FOUND' })
  })

  it('exposes an error naming the field when the answer is malformed', async () => {
    stubFetch(Response.json({ ...ownerViewMembers, canClaimNames: 'maybe' }))

    const { result } = await renderHookWithProviders(() => useMembers(testGroupId))

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
    expect(result.current.error?.message).toContain('canClaimNames')
  })
})
