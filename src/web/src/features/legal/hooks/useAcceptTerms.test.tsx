import { act, waitFor } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { meQueryKey } from '@/core/auth/useMe'
import { legalVersion } from '@/core/legal/legalInfo'
import { captureError, problemResponse, sentRequest, stubFetch } from '@/test/apiTestHelpers'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { seedMe, testGuestMe, testMeWithMaterialTerms, testMeWithOldTerms } from '@/test/testMe'
import { translated } from '@/test/translated'
import { useAcceptTerms } from './useAcceptTerms'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

describe('useAcceptTerms', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/me/terms/accept', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    const { result } = await renderHookWithProviders(() => useAcceptTerms(), {
      seedCache: seedMe(testMeWithOldTerms),
    })

    await act(() => result.current.mutateAsync())

    expect(sentRequest(fetchMock)).toMatchObject({ url: '/api/me/terms/accept', method: 'POST' })
  })

  it.each([
    ['a person with an account', testMeWithOldTerms],
    ['a guest', { ...testGuestMe, termsVersion: null, termsUpToDate: false }],
    ['a person who must agree to a material change', testMeWithMaterialTerms],
  ])('marks the terms of %s as up to date and not material in the me cache once the server answered', async (_name, me) => {
    stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(() => useAcceptTerms(), {
      seedCache: seedMe(me),
    })

    await act(() => result.current.mutateAsync())

    expect(queryClient.getQueryData(meQueryKey)).toMatchObject({ termsUpToDate: true, termsUpdateIsMaterial: false })
  })

  it('puts the current version of the legal texts in the me cache and leaves every other field alone', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(() => useAcceptTerms(), {
      seedCache: seedMe(testMeWithOldTerms),
    })

    await act(() => result.current.mutateAsync())

    expect(queryClient.getQueryData(meQueryKey)).toEqual({
      ...testMeWithOldTerms,
      termsVersion: legalVersion,
      termsUpToDate: true,
      termsUpdateIsMaterial: false,
    })
  })

  it('leaves the me cache as it was before the server answered', async () => {
    const { result, queryClient } = await renderHookWithProviders(() => useAcceptTerms(), {
      seedCache: seedMe(testMeWithOldTerms),
    })
    let cachedWhenAsked: unknown
    vi.stubGlobal(
      'fetch',
      vi.fn<typeof fetch>(async () => {
        cachedWhenAsked = queryClient.getQueryData(meQueryKey)
        return new Response(null, { status: 204 })
      }),
    )

    await act(() => result.current.mutateAsync())

    expect(cachedWhenAsked).toEqual(testMeWithOldTerms)
  })

  it('shows no error toast when the server accepts', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { result } = await renderHookWithProviders(() => useAcceptTerms(), {
      seedCache: seedMe(testMeWithOldTerms),
    })

    await act(() => result.current.mutateAsync())

    expect(toast.error).not.toHaveBeenCalled()
  })

  it('keeps the me cache and exposes the ApiError when the server refuses', async () => {
    stubFetch(problemResponse(429, 'RATE_LIMITED'))
    const { result, queryClient } = await renderHookWithProviders(() => useAcceptTerms(), {
      seedCache: seedMe(testMeWithOldTerms),
    })

    const error = await act(() => captureError(result.current.mutateAsync()))

    expect(error).toBeInstanceOf(ApiError)
    await waitFor(() => {
      expect(result.current.error).toBe(error)
    })
    expect(queryClient.getQueryData(meQueryKey)).toEqual(testMeWithOldTerms)
  })

  it.each([
    ['the server refuses', () => problemResponse(429, 'RATE_LIMITED'), 'errors.RATE_LIMITED'],
    ['the network fails', () => new TypeError('Failed to fetch'), 'errors.network'],
  ])('shows the normal error toast and keeps the me cache when %s', async (_name, failure, textKey) => {
    stubFetch(failure())
    const { result, queryClient } = await renderHookWithProviders(() => useAcceptTerms(), {
      seedCache: seedMe(testMeWithOldTerms),
    })

    await act(() => captureError(result.current.mutateAsync()))

    expect(toast.error).toHaveBeenCalledWith(translated('en', textKey))
    expect(queryClient.getQueryData(meQueryKey)).toEqual(testMeWithOldTerms)
  })
})
