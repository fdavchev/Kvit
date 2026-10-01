import { waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import type { Me } from '@/core/services/me/meService'
import { problemResponse, stubFetch } from '@/test/apiTestHelpers'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { testMe } from '@/test/testMe'
import { meQueryKey, useMe } from './useMe'

describe('useMe', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('loads the signed-in person and keeps them in the me cache', async () => {
    stubFetch(Response.json(testMe))

    const { result, queryClient } = await renderHookWithProviders(() => useMe())

    await waitFor(() => {
      expect(result.current.data).toEqual(testMe)
    })
    expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
  })

  it('gives null when the server answers 401', async () => {
    stubFetch(problemResponse(401, 'AUTH_NOT_SIGNED_IN'))

    const { result } = await renderHookWithProviders(() => useMe())

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })
    expect(result.current.data).toBeNull()
  })

  it('exposes the ApiError when the server fails with anything but 401', async () => {
    stubFetch(new Response(null, { status: 500 }))

    const { result } = await renderHookWithProviders(() => useMe())

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
    expect(result.current.error).toBeInstanceOf(ApiError)
    expect(result.current.data).toBeUndefined()
  })

  it('does not ask the server again when me is already in the cache', async () => {
    const fetchMock = stubFetch(Response.json(testMe))
    const { result, queryClient } = await renderHookWithProviders(() => useMe(), {
      seedCache: (client) => client.setQueryData<Me | null>(meQueryKey, testMe),
    })

    await waitFor(() => {
      expect(result.current.data).toEqual(testMe)
    })
    expect(fetchMock).not.toHaveBeenCalled()
    expect(queryClient.getQueryState(meQueryKey)?.fetchStatus).toBe('idle')
  })
})
