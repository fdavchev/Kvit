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
import {
  flatGroupRow,
  greeceGroupRow,
  groupListOf,
  groupOf,
  testGroup,
  testGroupId,
} from '@/test/groupTestData'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { useLeaveGroup } from './useLeaveGroup'

const otherGroup = groupOf({ id: flatGroupRow.id, name: flatGroupRow.name })

function seedTwoGroupsAndList(queryClient: QueryClient): void {
  queryClient.setQueryData(['groups', testGroupId], testGroup)
  queryClient.setQueryData(['groups', otherGroup.id], otherGroup)
  queryClient.setQueryData(['groups'], groupListOf({ groups: [greeceGroupRow, flatGroupRow] }))
}

describe('useLeaveGroup', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/groups/{id}/leave with no body', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    const { result } = await renderHookWithProviders(() => useLeaveGroup(testGroupId), {
      seedCache: seedTwoGroupsAndList,
    })

    await act(() => result.current.mutateAsync())

    expect(sentRequest(fetchMock)).toEqual({
      url: `/api/groups/${testGroupId}/leave`,
      method: 'POST',
      contentType: null,
      body: undefined,
    })
  })

  it('removes the group the person left from the cache because it now answers "not found"', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(() => useLeaveGroup(testGroupId), {
      seedCache: seedTwoGroupsAndList,
    })

    await act(() => result.current.mutateAsync())

    expect(queryClient.getQueryState(['groups', testGroupId])).toBeUndefined()
  })

  it('keeps the cache of the other groups', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(() => useLeaveGroup(testGroupId), {
      seedCache: seedTwoGroupsAndList,
    })

    await act(() => result.current.mutateAsync())

    expect(queryClient.getQueryData(['groups', otherGroup.id])).toEqual(otherGroup)
  })

  it('marks the group list as out of date so the group disappears from it', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(() => useLeaveGroup(testGroupId), {
      seedCache: seedTwoGroupsAndList,
    })

    await act(() => result.current.mutateAsync())

    expect(queryClient.getQueryState(['groups'])?.isInvalidated).toBe(true)
  })

  it('exposes the ApiError and keeps the group and the list when the owner tries to leave', async () => {
    stubFetch(problemResponse(400, 'MEMBER_OWNER_CANNOT_LEAVE'))
    const { result, queryClient } = await renderHookWithProviders(() => useLeaveGroup(testGroupId), {
      seedCache: seedTwoGroupsAndList,
    })

    const error = await act(() => captureError(result.current.mutateAsync()))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ errorCode: 'MEMBER_OWNER_CANNOT_LEAVE' })
    await waitFor(() => {
      expect(result.current.error).toBe(error)
    })
    expect(queryClient.getQueryData(['groups', testGroupId])).toEqual(testGroup)
    expect(queryClient.getQueryState(['groups'])?.isInvalidated).toBe(false)
  })
})
