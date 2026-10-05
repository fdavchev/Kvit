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
import { emptyGroupList, testGroup } from '@/test/groupTestData'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { useCreateGroup } from './useCreateGroup'

const groupInput = { name: 'Greece trip', emoji: testGroup.emoji, currency: 'MKD' } as const

function seedGroupList(queryClient: QueryClient): void {
  queryClient.setQueryData(['groups'], emptyGroupList)
}

describe('useCreateGroup', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/groups with the name, emoji and currency', async () => {
    const fetchMock = stubFetch(Response.json(testGroup))
    const { result } = await renderHookWithProviders(() => useCreateGroup(), {
      seedCache: seedGroupList,
    })

    await act(() => result.current.mutateAsync(groupInput))

    expect(sentRequest(fetchMock)).toMatchObject({
      url: '/api/groups',
      method: 'POST',
      body: groupInput,
    })
  })

  it('resolves with the new group so the screen can open it', async () => {
    stubFetch(Response.json(testGroup))
    const { result } = await renderHookWithProviders(() => useCreateGroup(), {
      seedCache: seedGroupList,
    })

    const created = await act(() => result.current.mutateAsync(groupInput))

    expect(created).toEqual(testGroup)
  })

  it('marks the group list as out of date after creating so it is loaded again', async () => {
    stubFetch(Response.json(testGroup))
    const { result, queryClient } = await renderHookWithProviders(() => useCreateGroup(), {
      seedCache: seedGroupList,
    })

    await act(() => result.current.mutateAsync(groupInput))

    expect(queryClient.getQueryState(['groups'])?.isInvalidated).toBe(true)
  })

  it('exposes the ApiError and keeps the group list up to date when the server refuses the name', async () => {
    stubFetch(problemResponse(400, 'GROUP_NAME_INVALID'))
    const { result, queryClient } = await renderHookWithProviders(() => useCreateGroup(), {
      seedCache: seedGroupList,
    })

    const error = await act(() => captureError(result.current.mutateAsync(groupInput)))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ errorCode: 'GROUP_NAME_INVALID' })
    await waitFor(() => {
      expect(result.current.error).toBe(error)
    })
    expect(queryClient.getQueryState(['groups'])?.isInvalidated).toBe(false)
  })
})
