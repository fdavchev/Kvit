import { waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { problemResponse, sentRequest, stubFetch } from '@/test/apiTestHelpers'
import { groupOf, testGroup, testGroupId } from '@/test/groupTestData'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { useGroup } from './useGroup'

describe('useGroup', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('loads the group from GET /api/groups/{id}', async () => {
    const fetchMock = stubFetch(Response.json(testGroup))

    const { result } = await renderHookWithProviders(() => useGroup(testGroupId))

    await waitFor(() => {
      expect(result.current.data).toEqual(testGroup)
    })
    expect(sentRequest(fetchMock).url).toBe(`/api/groups/${testGroupId}`)
  })

  it('keeps the group in the cache under the key ["groups", id]', async () => {
    stubFetch(Response.json(testGroup))

    const { result, queryClient } = await renderHookWithProviders(() => useGroup(testGroupId))

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })
    expect(queryClient.getQueryData(['groups', testGroupId])).toEqual(testGroup)
  })

  it('keeps each group under its own key', async () => {
    const otherGroup = groupOf({ id: 'f0e1d2c3-b4a5-4968-8776-655443322110', name: 'Flat 4B' })
    stubFetch(Response.json(otherGroup))

    const { result, queryClient } = await renderHookWithProviders(() => useGroup(otherGroup.id))

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })
    expect(queryClient.getQueryData(['groups', otherGroup.id])).toEqual(otherGroup)
    expect(queryClient.getQueryData(['groups', testGroupId])).toBeUndefined()
  })

  it('exposes the ApiError with its error code when the group is not found', async () => {
    stubFetch(problemResponse(404, 'GROUP_NOT_FOUND'))

    const { result } = await renderHookWithProviders(() => useGroup(testGroupId))

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
    expect(result.current.error).toBeInstanceOf(ApiError)
    expect(result.current.error).toMatchObject({ httpStatus: 404, errorCode: 'GROUP_NOT_FOUND' })
  })

  it('exposes an error naming the field when the answer is malformed', async () => {
    stubFetch(Response.json({ ...testGroup, memberCount: 'many' }))

    const { result } = await renderHookWithProviders(() => useGroup(testGroupId))

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
    expect(result.current.error?.message).toContain('memberCount')
  })
})
