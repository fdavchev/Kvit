import { act, waitFor } from '@testing-library/react'
import { dehydrate, type QueryClient } from '@tanstack/react-query'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { savedQueryCacheVersion } from '@/core/api/savedQueryCache'
import { meQueryKey } from '@/core/auth/useMe'
import type { Me } from '@/core/services/me/meService'
import { captureError, stubFetch } from '@/test/apiTestHelpers'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { testMe } from '@/test/testMe'
import { createTestPersister } from '@/test/testPersister'
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

  it('ends with nobody signed in and no finished log-out left in the mutation cache', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(() => useLogOut(), {
      seedCache: seedSignedInPerson,
    })

    await act(() => result.current.mutateAsync())

    expect(queryClient.getQueryData(meQueryKey)).toBeNull()
    expect(queryClient.getMutationCache().getAll()).toEqual([])
  })

  it('deletes the saved copy from the browser after the server logs the person out', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const persister = createTestPersister()
    const { result, queryClient } = await renderHookWithProviders(() => useLogOut(), {
      persister,
      seedCache: seedSignedInPerson,
    })
    await persister.persistClient({
      timestamp: Date.now(),
      buster: savedQueryCacheVersion,
      clientState: dehydrate(queryClient),
    })

    await act(() => result.current.mutateAsync())

    expect(await persister.restoreClient()).toBeUndefined()
  })

  it('keeps the saved copy when the log-out fails', async () => {
    stubFetch(new Response(null, { status: 500 }))
    const persister = createTestPersister()
    const { result, queryClient } = await renderHookWithProviders(() => useLogOut(), {
      persister,
      seedCache: seedSignedInPerson,
    })
    await persister.persistClient({
      timestamp: Date.now(),
      buster: savedQueryCacheVersion,
      clientState: dehydrate(queryClient),
    })

    await act(() => captureError(result.current.mutateAsync()))

    expect(await persister.restoreClient()).toBeDefined()
  })
})
