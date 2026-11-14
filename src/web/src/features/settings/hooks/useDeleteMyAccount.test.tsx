import { act, waitFor } from '@testing-library/react'
import { dehydrate, type QueryClient } from '@tanstack/react-query'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { savedQueryCacheVersion } from '@/core/api/savedQueryCache'
import { meQueryKey } from '@/core/auth/useMe'
import type { Me } from '@/core/services/me/meService'
import { captureError, problemResponse, sentRequest, stubFetch } from '@/test/apiTestHelpers'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { testMe } from '@/test/testMe'
import { createTestPersister } from '@/test/testPersister'
import { useDeleteMyAccount } from './useDeleteMyAccount'

const groupsKey = ['groups']
const groups = ['Greece trip']

function seedSignedInPerson(queryClient: QueryClient): void {
  queryClient.setQueryData<Me | null>(meQueryKey, testMe)
  queryClient.setQueryData(groupsKey, groups)
}

describe('useDeleteMyAccount', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(['KeepName', 'FormerMember'] as const)('sends POST /api/me/delete with the name choice %s', async (nameChoice) => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    const { result } = await renderHookWithProviders(() => useDeleteMyAccount(), {
      seedCache: seedSignedInPerson,
    })

    await act(() => result.current.mutateAsync(nameChoice))

    expect(sentRequest(fetchMock)).toMatchObject({
      url: '/api/me/delete',
      method: 'POST',
      body: { nameChoice },
    })
  })

  it('empties the whole cache and marks the person as signed out after the server deletes the account', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(() => useDeleteMyAccount(), {
      seedCache: seedSignedInPerson,
    })

    await act(() => result.current.mutateAsync('KeepName'))

    expect(queryClient.getQueryData(groupsKey)).toBeUndefined()
    expect(queryClient.getQueryData(meQueryKey)).toBeNull()
  })

  it('ends with nobody signed in and no finished deletion left in the mutation cache', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(() => useDeleteMyAccount(), {
      seedCache: seedSignedInPerson,
    })

    await act(() => result.current.mutateAsync('FormerMember'))

    expect(queryClient.getQueryData(meQueryKey)).toBeNull()
    expect(queryClient.getMutationCache().getAll()).toEqual([])
  })

  it('deletes the saved copy from the browser after the server deletes the account', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const persister = createTestPersister()
    const { result, queryClient } = await renderHookWithProviders(() => useDeleteMyAccount(), {
      persister,
      seedCache: seedSignedInPerson,
    })
    await persister.persistClient({
      timestamp: Date.now(),
      buster: savedQueryCacheVersion,
      clientState: dehydrate(queryClient),
    })

    await act(() => result.current.mutateAsync('KeepName'))

    expect(await persister.restoreClient()).toBeUndefined()
  })

  it('exposes the ApiError and keeps the cache when the server refuses the name choice', async () => {
    stubFetch(problemResponse(400, 'ACCOUNT_DELETE_NAME_CHOICE_INVALID'))
    const { result, queryClient } = await renderHookWithProviders(() => useDeleteMyAccount(), {
      seedCache: seedSignedInPerson,
    })

    const error = await act(() => captureError(result.current.mutateAsync('KeepName')))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 400, errorCode: 'ACCOUNT_DELETE_NAME_CHOICE_INVALID' })
    await waitFor(() => {
      expect(result.current.error).toBe(error)
    })
    expect(queryClient.getQueryData(groupsKey)).toEqual(groups)
    expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
  })

  it('keeps the saved copy when the deletion fails', async () => {
    stubFetch(new Response(null, { status: 500 }))
    const persister = createTestPersister()
    const { result, queryClient } = await renderHookWithProviders(() => useDeleteMyAccount(), {
      persister,
      seedCache: seedSignedInPerson,
    })
    await persister.persistClient({
      timestamp: Date.now(),
      buster: savedQueryCacheVersion,
      clientState: dehydrate(queryClient),
    })

    await act(() => captureError(result.current.mutateAsync('KeepName')))

    expect(await persister.restoreClient()).toBeDefined()
  })

  it('keeps the cache when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))
    const { result, queryClient } = await renderHookWithProviders(() => useDeleteMyAccount(), {
      seedCache: seedSignedInPerson,
    })

    await act(() => captureError(result.current.mutateAsync('KeepName')))

    expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
    expect(queryClient.getQueryData(groupsKey)).toEqual(groups)
  })
})
