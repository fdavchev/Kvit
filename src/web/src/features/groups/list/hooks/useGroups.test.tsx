import { waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { problemResponse, sentRequest, stubFetch } from '@/test/apiTestHelpers'
import {
  dinnerDeletedRow,
  emptyGroupList,
  flatGroupRow,
  greeceGroupRow,
  groupListOf,
  summerFinishedRow,
} from '@/test/groupTestData'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { useGroups } from './useGroups'

const fullList = groupListOf({
  groups: [greeceGroupRow, flatGroupRow],
  finishedGroups: [summerFinishedRow],
  recentlyDeleted: [dinnerDeletedRow],
})

describe('useGroups', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('loads the three group lists from GET /api/groups', async () => {
    const fetchMock = stubFetch(Response.json(fullList))

    const { result } = await renderHookWithProviders(() => useGroups())

    await waitFor(() => {
      expect(result.current.data).toEqual(fullList)
    })
    expect(sentRequest(fetchMock).url).toBe('/api/groups')
  })

  it('keeps the lists in the cache under the key ["groups"]', async () => {
    stubFetch(Response.json(fullList))

    const { result, queryClient } = await renderHookWithProviders(() => useGroups())

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })
    expect(queryClient.getQueryData(['groups'])).toEqual(fullList)
  })

  it('is pending before the server has answered', async () => {
    stubFetch(Response.json(emptyGroupList))

    const { result } = await renderHookWithProviders(() => useGroups())

    expect(result.current.isPending).toBe(true)
    expect(result.current.data).toBeUndefined()
  })

  it('shows the lists that are already in the cache straight away', async () => {
    stubFetch(Response.json(fullList))
    const { result } = await renderHookWithProviders(() => useGroups(), {
      seedCache: (client) => client.setQueryData(['groups'], fullList),
    })

    expect(result.current.isPending).toBe(false)
    expect(result.current.data).toEqual(fullList)
  })

  it('exposes the ApiError when the server fails', async () => {
    stubFetch(problemResponse(500, 'SOMETHING_BROKE'))

    const { result } = await renderHookWithProviders(() => useGroups())

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
    expect(result.current.error).toBeInstanceOf(ApiError)
    expect(result.current.data).toBeUndefined()
  })

  it('exposes an error naming the field when the answer is malformed', async () => {
    stubFetch(Response.json({ finishedGroups: [], recentlyDeleted: [] }))

    const { result } = await renderHookWithProviders(() => useGroups())

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
    expect(result.current.error?.message).toContain('groups')
  })
})
