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
  testGroup,
  testGroupId,
} from '@/test/groupTestData'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { useUpdateGroup } from './useUpdateGroup'

const changes = { name: 'Flat 4B', emoji: '\u{1F3E0}', currency: 'EUR' } as const

function seedGroupAndList(queryClient: QueryClient): void {
  queryClient.setQueryData(['groups', testGroupId], testGroup)
  queryClient.setQueryData(['groups'], groupListOf({ groups: [greeceGroupRow] }))
  queryClient.setQueryData(['groups', testGroupId, 'activity'], [])
}

describe('useUpdateGroup', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends PUT /api/groups/{id} with the name, emoji and currency', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    const { result } = await renderHookWithProviders(() => useUpdateGroup(testGroupId), {
      seedCache: seedGroupAndList,
    })

    await act(() => result.current.mutateAsync(changes))

    expect(sentRequest(fetchMock)).toMatchObject({
      url: `/api/groups/${testGroupId}`,
      method: 'PUT',
      body: changes,
    })
  })

  it('marks the group as out of date after saving so it is loaded again', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(() => useUpdateGroup(testGroupId), {
      seedCache: seedGroupAndList,
    })

    await act(() => result.current.mutateAsync(changes))

    expect(queryClient.getQueryState(['groups', testGroupId])?.isInvalidated).toBe(true)
  })

  it('marks the group list as out of date after saving so the new name is loaded again', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(() => useUpdateGroup(testGroupId), {
      seedCache: seedGroupAndList,
    })

    await act(() => result.current.mutateAsync(changes))

    expect(queryClient.getQueryState(['groups'])?.isInvalidated).toBe(true)
  })

  it('marks the activity of the group as out of date after saving so the new name, emoji or currency shows up in it', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(() => useUpdateGroup(testGroupId), {
      seedCache: seedGroupAndList,
    })

    await act(() => result.current.mutateAsync(changes))

    expect(queryClient.getQueryState(['groups', testGroupId, 'activity'])?.isInvalidated).toBe(true)
  })

  it('exposes the ApiError and keeps the cache up to date when the person is not the owner', async () => {
    stubFetch(problemResponse(403, 'GROUP_NOT_OWNER'))
    const { result, queryClient } = await renderHookWithProviders(() => useUpdateGroup(testGroupId), {
      seedCache: seedGroupAndList,
    })

    const error = await act(() => captureError(result.current.mutateAsync(changes)))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ errorCode: 'GROUP_NOT_OWNER' })
    await waitFor(() => {
      expect(result.current.error).toBe(error)
    })
    expect(queryClient.getQueryState(['groups', testGroupId])?.isInvalidated).toBe(false)
    expect(queryClient.getQueryState(['groups'])?.isInvalidated).toBe(false)
    expect(queryClient.getQueryState(['groups', testGroupId, 'activity'])?.isInvalidated).toBe(false)
  })
})
