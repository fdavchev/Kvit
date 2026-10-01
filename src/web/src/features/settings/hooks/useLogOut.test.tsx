import { act, waitFor } from '@testing-library/react'
import type { QueryClient } from '@tanstack/react-query'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { meQueryKey } from '@/core/auth/useMe'
import type { Me } from '@/core/services/me/meService'
import { captureError, stubFetch } from '@/test/apiTestHelpers'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { testMe } from '@/test/testMe'
import { useLogOut } from './useLogOut'

const groupsKey = ['groups']
const groups = ['Greece trip']

function seedSignedInPerson(queryClient: QueryClient): void {
  queryClient.setQueryData<Me | null>(meQueryKey, testMe)
  queryClient.setQueryData(groupsKey, groups)
}

describe('useLogOut', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('empties the whole cache and marks the person as signed out after the server logs them out', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(() => useLogOut(), {
      seedCache: seedSignedInPerson,
    })

    await act(() => result.current.mutateAsync())

    expect(queryClient.getQueryData(groupsKey)).toBeUndefined()
    expect(queryClient.getQueryData(meQueryKey)).toBeNull()
  })

  it('exposes the ApiError and keeps the cache when the log-out fails', async () => {
    stubFetch(new Response(null, { status: 500 }))
    const { result, queryClient } = await renderHookWithProviders(() => useLogOut(), {
      seedCache: seedSignedInPerson,
    })

    const error = await act(() => captureError(result.current.mutateAsync()))

    expect(error).toBeInstanceOf(ApiError)
    await waitFor(() => {
      expect(result.current.error).toBe(error)
    })
    expect(queryClient.getQueryData(groupsKey)).toEqual(groups)
    expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
  })
})
