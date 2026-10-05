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
  emptyGroupList,
  greeceGroupRow,
  groupListOf,
  testGroup,
  testGroupId,
} from '@/test/groupTestData'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { useAddMember } from './useAddMember'

const addedMember = {
  id: '1f2e3d4c-5b6a-4789-8a9b-0c1d2e3f4a5b',
  name: 'Grandma',
  displayName: 'Grandma',
  userId: null,
  isOwner: false,
  isYou: false,
  isNameOnly: true,
  claimedName: null,
}

function seedGroupAndList(queryClient: QueryClient): void {
  queryClient.setQueryData(['groups', testGroupId], testGroup)
  queryClient.setQueryData(['groups'], groupListOf({ groups: [greeceGroupRow] }))
}

describe('useAddMember', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/groups/{id}/members with the name', async () => {
    const fetchMock = stubFetch(Response.json(addedMember))
    const { result } = await renderHookWithProviders(() => useAddMember(testGroupId), {
      seedCache: seedGroupAndList,
    })

    await act(() => result.current.mutateAsync('Grandma'))

    expect(sentRequest(fetchMock)).toMatchObject({
      url: `/api/groups/${testGroupId}/members`,
      method: 'POST',
      body: { name: 'Grandma' },
    })
  })

  it('marks the group as out of date after adding so its member count is loaded again', async () => {
    stubFetch(Response.json(addedMember))
    const { result, queryClient } = await renderHookWithProviders(() => useAddMember(testGroupId), {
      seedCache: seedGroupAndList,
    })

    await act(() => result.current.mutateAsync('Grandma'))

    expect(queryClient.getQueryState(['groups', testGroupId])?.isInvalidated).toBe(true)
  })

  it('marks the group list as out of date after adding so its member count is loaded again', async () => {
    stubFetch(Response.json(addedMember))
    const { result, queryClient } = await renderHookWithProviders(() => useAddMember(testGroupId), {
      seedCache: seedGroupAndList,
    })

    await act(() => result.current.mutateAsync('Grandma'))

    expect(queryClient.getQueryState(['groups'])?.isInvalidated).toBe(true)
  })

  it('exposes the ApiError and keeps the cache up to date when the name is already in the group', async () => {
    stubFetch(problemResponse(400, 'MEMBER_NAME_TAKEN'))
    const { result, queryClient } = await renderHookWithProviders(() => useAddMember(testGroupId), {
      seedCache: seedGroupAndList,
    })

    const error = await act(() => captureError(result.current.mutateAsync('Marko')))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ errorCode: 'MEMBER_NAME_TAKEN' })
    await waitFor(() => {
      expect(result.current.error).toBe(error)
    })
    expect(queryClient.getQueryState(['groups', testGroupId])?.isInvalidated).toBe(false)
    expect(queryClient.getQueryState(['groups'])?.isInvalidated).toBe(false)
  })

  it('does not touch queries that are not about groups', async () => {
    stubFetch(Response.json(addedMember))
    const { result, queryClient } = await renderHookWithProviders(() => useAddMember(testGroupId), {
      seedCache: (client) => {
        seedGroupAndList(client)
        client.setQueryData(['other'], emptyGroupList)
      },
    })

    await act(() => result.current.mutateAsync('Grandma'))

    expect(queryClient.getQueryState(['other'])?.isInvalidated).toBe(false)
  })
})
