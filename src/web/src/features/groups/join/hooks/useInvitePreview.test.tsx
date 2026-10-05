import { waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { problemResponse, sentRequest, stubFetch } from '@/test/apiTestHelpers'
import { testInviteToken } from '@/test/groupTestData'
import { alreadyMemberInvitePreview, openInvitePreviewWithNames } from '@/test/inviteTestData'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { useInvitePreview } from './useInvitePreview'

const otherToken = 'another-token_42'

describe('useInvitePreview', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('loads the invite with POST /api/invites/preview and the token in the body, not in the address', async () => {
    const fetchMock = stubFetch(Response.json(openInvitePreviewWithNames))

    const { result } = await renderHookWithProviders(() => useInvitePreview(testInviteToken))

    await waitFor(() => {
      expect(result.current.data).toEqual(openInvitePreviewWithNames)
    })
    expect(sentRequest(fetchMock)).toMatchObject({
      url: '/api/invites/preview',
      method: 'POST',
      body: { token: testInviteToken },
    })
  })

  it('keeps the invite in the cache under the key ["invites", token]', async () => {
    stubFetch(Response.json(alreadyMemberInvitePreview))

    const { result, queryClient } = await renderHookWithProviders(() =>
      useInvitePreview(testInviteToken),
    )

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })
    expect(queryClient.getQueryData(['invites', testInviteToken])).toEqual(alreadyMemberInvitePreview)
  })

  it('keeps each token under its own key', async () => {
    stubFetch(Response.json(openInvitePreviewWithNames))

    const { result, queryClient } = await renderHookWithProviders(() => useInvitePreview(otherToken))

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })
    expect(queryClient.getQueryData(['invites', otherToken])).toEqual(openInvitePreviewWithNames)
    expect(queryClient.getQueryData(['invites', testInviteToken])).toBeUndefined()
  })

  it.each([
    [404, 'INVITE_NOT_FOUND'],
    [429, 'RATE_LIMITED'],
  ])('exposes the ApiError with its error code for the answer %i %s', async (httpStatus, errorCode) => {
    stubFetch(problemResponse(httpStatus, errorCode))

    const { result } = await renderHookWithProviders(() => useInvitePreview(testInviteToken))

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
    expect(result.current.error).toBeInstanceOf(ApiError)
    expect(result.current.error).toMatchObject({ httpStatus, errorCode })
  })

  it('exposes an error naming the field when the answer is malformed', async () => {
    stubFetch(Response.json({ ...openInvitePreviewWithNames, status: 'Closed' }))

    const { result } = await renderHookWithProviders(() => useInvitePreview(testInviteToken))

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
    expect(result.current.error?.message).toContain('status')
  })
})
