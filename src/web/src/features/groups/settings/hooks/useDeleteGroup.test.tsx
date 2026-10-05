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
import { useDeleteGroup } from './useDeleteGroup'

const otherGroup = groupOf({ id: flatGroupRow.id, name: flatGroupRow.name })

function seedTwoGroupsAndList(queryClient: QueryClient): void {
  queryClient.setQueryData(['groups', testGroupId], testGroup)
  queryClient.setQueryData(['groups', otherGroup.id], otherGroup)
  queryClient.setQueryData(['groups'], groupListOf({ groups: [greeceGroupRow, flatGroupRow] }))
}

describe('useDeleteGroup', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends DELETE /api/groups/{id} with no body', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    const { result } = await renderHookWithProviders(() => useDeleteGroup(testGroupId), {
      seedCache: seedTwoGroupsAndList,
    })

    await act(() => result.current.mutateAsync())

    expect(sentRequest(fetchMock)).toEqual({
      url: `/api/groups/${testGroupId}`,
      method: 'DELETE',
      contentType: null,
      body: undefined,
    })
  })

  it('removes the deleted group from the cache so it is never shown again', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(() => useDeleteGroup(testGroupId), {
      seedCache: seedTwoGroupsAndList,
    })

    await act(() => result.current.mutateAsync())

    expect(queryClient.getQueryState(['groups', testGroupId])).toBeUndefined()
  })

  it('keeps the cache of the other groups', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(() => useDeleteGroup(testGroupId), {
      seedCache: seedTwoGroupsAndList,
    })

    await act(() => result.current.mutateAsync())

    expect(queryClient.getQueryData(['groups', otherGroup.id])).toEqual(otherGroup)
  })

  it('marks the group list as out of date so the deleted group moves to Recently deleted', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(() => useDeleteGroup(testGroupId), {
      seedCache: seedTwoGroupsAndList,
    })

    await act(() => result.current.mutateAsync())

    expect(queryClient.getQueryState(['groups'])?.isInvalidated).toBe(true)
  })

  it('exposes the ApiError and keeps the group and the list when the person is not the owner', async () => {
    stubFetch(problemResponse(403, 'GROUP_NOT_OWNER'))
    const { result, queryClient } = await renderHookWithProviders(() => useDeleteGroup(testGroupId), {
      seedCache: seedTwoGroupsAndList,
    })

    const error = await act(() => captureError(result.current.mutateAsync()))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ errorCode: 'GROUP_NOT_OWNER' })
    await waitFor(() => {
      expect(result.current.error).toBe(error)
    })
    expect(queryClient.getQueryData(['groups', testGroupId])).toEqual(testGroup)
    expect(queryClient.getQueryState(['groups'])?.isInvalidated).toBe(false)
  })
})
