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
  dinnerDeletedRow,
  groupListOf,
  testGroupId,
} from '@/test/groupTestData'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { useRestoreGroup } from './useRestoreGroup'

const listWithDeletedGroup = groupListOf({ recentlyDeleted: [dinnerDeletedRow] })

function seedGroupList(queryClient: QueryClient): void {
  queryClient.setQueryData(['groups'], listWithDeletedGroup)
}

describe('useRestoreGroup', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/groups/{id}/restore for the group it is given', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    const { result } = await renderHookWithProviders(() => useRestoreGroup(), {
      seedCache: seedGroupList,
    })

    await act(() => result.current.mutateAsync(dinnerDeletedRow.id))

    expect(sentRequest(fetchMock)).toMatchObject({
      url: `/api/groups/${dinnerDeletedRow.id}/restore`,
      method: 'POST',
    })
  })

  it('marks the group list as out of date after the restore so it is loaded again', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(() => useRestoreGroup(), {
      seedCache: seedGroupList,
    })

    await act(() => result.current.mutateAsync(dinnerDeletedRow.id))

    expect(queryClient.getQueryState(['groups'])?.isInvalidated).toBe(true)
  })

  it('exposes the ApiError and keeps the group list up to date when the restore time has passed', async () => {
    stubFetch(problemResponse(400, 'GROUP_RESTORE_EXPIRED'))
    const { result, queryClient } = await renderHookWithProviders(() => useRestoreGroup(), {
      seedCache: seedGroupList,
    })

    const error = await act(() => captureError(result.current.mutateAsync(testGroupId)))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ errorCode: 'GROUP_RESTORE_EXPIRED' })
    await waitFor(() => {
      expect(result.current.error).toBe(error)
    })
    expect(queryClient.getQueryState(['groups'])?.isInvalidated).toBe(false)
  })
})
