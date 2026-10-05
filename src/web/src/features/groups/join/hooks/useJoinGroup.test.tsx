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
  greeceGroupRow,
  groupListOf,
  testGroupId,
  testInviteToken,
} from '@/test/groupTestData'
import { openInvitePreviewWithNames } from '@/test/inviteTestData'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { useJoinGroup } from './useJoinGroup'

const claimMemberId = openInvitePreviewWithNames.unclaimedNames[0].id

function seedListAndInvite(queryClient: QueryClient): void {
  queryClient.setQueryData(['groups'], groupListOf({ groups: [greeceGroupRow] }))
  queryClient.setQueryData(['invites', testInviteToken], openInvitePreviewWithNames)
  queryClient.setQueryData(['other'], openInvitePreviewWithNames)
}

describe('useJoinGroup', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/invites/join with only the token and gives back the group id', async () => {
    const fetchMock = stubFetch(Response.json({ groupId: testGroupId }))
    const { result } = await renderHookWithProviders(() => useJoinGroup(), {
      seedCache: seedListAndInvite,
    })

    const groupId = await act(() => result.current.mutateAsync({ token: testInviteToken }))

    expect(groupId).toBe(testGroupId)
    const request = sentRequest(fetchMock)
    expect(request).toMatchObject({ url: '/api/invites/join', method: 'POST' })
    expect(Object.keys(request.body as object)).toEqual(['token'])
    expect(request.body).toEqual({ token: testInviteToken })
  })

  it('sends the claimed member id with the token when a name is claimed', async () => {
    const fetchMock = stubFetch(Response.json({ groupId: testGroupId }))
    const { result } = await renderHookWithProviders(() => useJoinGroup(), {
      seedCache: seedListAndInvite,
    })

    await act(() => result.current.mutateAsync({ token: testInviteToken, claimMemberId }))

    expect(sentRequest(fetchMock).body).toEqual({ token: testInviteToken, claimMemberId })
  })

  it('marks the group list as out of date after joining so the new group shows up in it', async () => {
    stubFetch(Response.json({ groupId: testGroupId }))
    const { result, queryClient } = await renderHookWithProviders(() => useJoinGroup(), {
      seedCache: seedListAndInvite,
    })

    await act(() => result.current.mutateAsync({ token: testInviteToken }))

    expect(queryClient.getQueryState(['groups'])?.isInvalidated).toBe(true)
  })

  it('does not touch queries that are not about groups', async () => {
    stubFetch(Response.json({ groupId: testGroupId }))
    const { result, queryClient } = await renderHookWithProviders(() => useJoinGroup(), {
      seedCache: seedListAndInvite,
    })

    await act(() => result.current.mutateAsync({ token: testInviteToken }))

    expect(queryClient.getQueryState(['other'])?.isInvalidated).toBe(false)
  })

  it.each([
    [403, 'INVITE_REMOVED'],
    [404, 'INVITE_NOT_FOUND'],
    [400, 'MEMBER_CANNOT_CLAIM'],
    [429, 'RATE_LIMITED'],
  ])('exposes the ApiError and keeps the group list up to date for the answer %i %s', async (httpStatus, errorCode) => {
    stubFetch(problemResponse(httpStatus, errorCode))
    const { result, queryClient } = await renderHookWithProviders(() => useJoinGroup(), {
      seedCache: seedListAndInvite,
    })

    const error = await act(() =>
      captureError(result.current.mutateAsync({ token: testInviteToken })),
    )

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
    await waitFor(() => {
      expect(result.current.error).toBe(error)
    })
    expect(queryClient.getQueryState(['groups'])?.isInvalidated).toBe(false)
  })
})
