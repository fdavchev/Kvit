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
import {
  seedGroupMembersAndList,
  seedGroupMembersListAndOther,
} from '@/test/membersCacheTestHelpers'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { useUndoResetInviteLink } from './useUndoResetInviteLink'

const newInviteToken = 'Zt7-newTokenFromTheServer_0123456789abcdefghijk'

describe('useUndoResetInviteLink', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/groups/{id}/invite/undo-reset with no body and gives back the invite token', async () => {
    const fetchMock = stubFetch(Response.json({ inviteToken: newInviteToken }))
    const { result } = await renderHookWithProviders(() => useUndoResetInviteLink(testGroupId), {
      seedCache: seedGroupMembersAndList,
    })

    const token = await act(() => result.current.mutateAsync())

    expect(token).toBe(newInviteToken)
    expect(sentRequest(fetchMock)).toEqual({
      url: `/api/groups/${testGroupId}/invite/undo-reset`,
      method: 'POST',
      contentType: null,
      body: undefined,
    })
  })

  it.each([
    ['the group', ['groups', testGroupId]],
    ['the members of the group', ['groups', testGroupId, 'members']],
    ['the group list', ['groups']],
  ])('marks %s as out of date after the change so the new link is loaded', async (_name, queryKey) => {
    stubFetch(Response.json({ inviteToken: newInviteToken }))
    const { result, queryClient } = await renderHookWithProviders(() => useUndoResetInviteLink(testGroupId), {
      seedCache: seedGroupMembersAndList,
    })

    await act(() => result.current.mutateAsync())

    expect(queryClient.getQueryState(queryKey)?.isInvalidated).toBe(true)
  })

  it('exposes the ApiError and keeps the cache up to date when the server refuses', async () => {
    stubFetch(problemResponse(403, 'GROUP_NOT_OWNER'))
    const { result, queryClient } = await renderHookWithProviders(() => useUndoResetInviteLink(testGroupId), {
      seedCache: seedGroupMembersAndList,
    })

    const error = await act(() => captureError(result.current.mutateAsync()))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 403, errorCode: 'GROUP_NOT_OWNER' })
    await waitFor(() => {
      expect(result.current.error).toBe(error)
    })
    expect(queryClient.getQueryState(['groups', testGroupId])?.isInvalidated).toBe(false)
    expect(queryClient.getQueryState(['groups', testGroupId, 'members'])?.isInvalidated).toBe(false)
    expect(queryClient.getQueryState(['groups'])?.isInvalidated).toBe(false)
  })

  it('does not touch queries that are not about groups', async () => {
    stubFetch(Response.json({ inviteToken: newInviteToken }))
    const { result, queryClient } = await renderHookWithProviders(() => useUndoResetInviteLink(testGroupId), {
      seedCache: seedGroupMembersListAndOther,
    })

    await act(() => result.current.mutateAsync())

    expect(queryClient.getQueryState(['other'])?.isInvalidated).toBe(false)
  })
})
