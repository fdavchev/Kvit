import { act, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import {
  captureError,
  problemResponse,
  sentRequest,
  stubFetch,
} from '@/test/apiTestHelpers'
import { testGroupId } from '@/test/groupTestData'
import { bojanRemoved } from '@/test/memberTestData'
import {
  seedGroupMembersAndList,
  seedGroupMembersListAndOther,
} from '@/test/membersCacheTestHelpers'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { useLetBackIn } from './useLetBackIn'

const memberId = bojanRemoved.id

describe('useLetBackIn', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/groups/{id}/members/{memberId}/let-back-in with no body for the member it is given', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    const { result } = await renderHookWithProviders(() => useLetBackIn(testGroupId), {
      seedCache: seedGroupMembersAndList,
    })

    await act(() => result.current.mutateAsync(memberId))

    expect(sentRequest(fetchMock)).toEqual({
      url: `/api/groups/${testGroupId}/members/${memberId}/let-back-in`,
      method: 'POST',
      contentType: null,
      body: undefined,
    })
  })

  it.each([
    ['the group', ['groups', testGroupId]],
    ['the members of the group', ['groups', testGroupId, 'members']],
    ['the group list', ['groups']],
  ])('marks %s as out of date after the change so it is loaded again', async (_name, queryKey) => {
    stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(() => useLetBackIn(testGroupId), {
      seedCache: seedGroupMembersAndList,
    })

    await act(() => result.current.mutateAsync(memberId))

    expect(queryClient.getQueryState(queryKey)?.isInvalidated).toBe(true)
  })

  it('exposes the ApiError and keeps the cache up to date when the server refuses', async () => {
    stubFetch(problemResponse(404, 'MEMBER_NOT_FOUND'))
    const { result, queryClient } = await renderHookWithProviders(() => useLetBackIn(testGroupId), {
      seedCache: seedGroupMembersAndList,
    })

    const error = await act(() => captureError(result.current.mutateAsync(memberId)))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 404, errorCode: 'MEMBER_NOT_FOUND' })
    await waitFor(() => {
      expect(result.current.error).toBe(error)
    })
    expect(queryClient.getQueryState(['groups', testGroupId])?.isInvalidated).toBe(false)
    expect(queryClient.getQueryState(['groups', testGroupId, 'members'])?.isInvalidated).toBe(false)
    expect(queryClient.getQueryState(['groups'])?.isInvalidated).toBe(false)
  })

  it('does not touch queries that are not about groups', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(() => useLetBackIn(testGroupId), {
      seedCache: seedGroupMembersListAndOther,
    })

    await act(() => result.current.mutateAsync(memberId))

    expect(queryClient.getQueryState(['other'])?.isInvalidated).toBe(false)
  })
})
