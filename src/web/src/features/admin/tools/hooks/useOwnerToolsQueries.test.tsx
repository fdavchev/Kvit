import { dehydrate, type UseQueryResult } from '@tanstack/react-query'
import { waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { savedQueryCacheOptions } from '@/core/api/savedQueryCache'
import { meQueryKey } from '@/core/auth/useMe'
import { problemResponse, requestCountTo, stubFetch, stubFetchByPath } from '@/test/apiTestHelpers'
import { restoreRealRetryPolicy } from '@/test/expenseTestHelpers'
import { anaPerson, logEntries, tripGroup } from '@/test/ownerToolsTestData'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { seedMe, testAdminMe } from '@/test/testMe'
import { createTestPersister } from '@/test/testPersister'
import { useOwnerToolsGroup } from './useOwnerToolsGroup'
import { useOwnerToolsLog } from './useOwnerToolsLog'
import { useOwnerToolsPeople } from './useOwnerToolsPeople'

interface QueryCase {
  name: string
  useQueryHook: () => UseQueryResult<unknown>
  url: string
  body: unknown
  brokenBody: unknown
  brokenField: string
}

const peopleUrl = '/api/admin/tools/people?query=ana'
const groupUrl = `/api/admin/tools/groups/${tripGroup.id}`
const logUrl = '/api/admin/tools/log?limit=50'

const queryCases: QueryCase[] = [
  {
    name: 'useOwnerToolsPeople',
    useQueryHook: () => useOwnerToolsPeople('ana'),
    url: peopleUrl,
    body: [anaPerson],
    brokenBody: [{ ...anaPerson, groupCount: 'x' }],
    brokenField: 'groupCount',
  },
  {
    name: 'useOwnerToolsGroup',
    useQueryHook: () => useOwnerToolsGroup(tripGroup.id),
    url: groupUrl,
    body: tripGroup,
    brokenBody: { ...tripGroup, memberCount: 'x' },
    brokenField: 'memberCount',
  },
  {
    name: 'useOwnerToolsLog',
    useQueryHook: () => useOwnerToolsLog(),
    url: logUrl,
    body: logEntries,
    brokenBody: [{ ...logEntries[0], action: 'Deleted' }],
    brokenField: 'action',
  },
]

describe.each(queryCases)('$name', ({ useQueryHook, url, body, brokenBody, brokenField }) => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('is pending first and then holds the answer', async () => {
    stubFetch(Response.json(body))

    const { result } = await renderHookWithProviders(useQueryHook)

    expect(result.current.isPending).toBe(true)
    await waitFor(() => {
      expect(result.current.data).toEqual(body)
    })
  })

  it('asks the server once, for its own address', async () => {
    const fetchMock = stubFetchByPath({ [url]: Response.json(body) })

    const { result } = await renderHookWithProviders(useQueryHook)
    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(requestCountTo(fetchMock, url)).toBe(1)
    expect(fetchMock).toHaveBeenCalledOnce()
  })

  it('holds the error with its code when the server refuses', async () => {
    stubFetch(problemResponse(404, 'OWNER_GROUP_NOT_FOUND'))

    const { result } = await renderHookWithProviders(useQueryHook)

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
    expect(result.current.error).toMatchObject({ httpStatus: 404, errorCode: 'OWNER_GROUP_NOT_FOUND' })
  })

  it('holds the error naming the field when the answer cannot be read', async () => {
    stubFetch(Response.json(brokenBody))

    const { result } = await renderHookWithProviders(useQueryHook)

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
    expect(result.current.error?.message).toContain(brokenField)
  })

  it.each([
    [400, 'OWNER_REASON_INVALID'],
    [403, null],
    [404, 'OWNER_GROUP_NOT_FOUND'],
  ])('asks only once and shows the error at once for a %s answer, with the retry policy of the app', async (status, errorCode) => {
    const fetchMock = stubFetch(errorCode === null ? new Response(null, { status }) : problemResponse(status, errorCode))

    const { result } = await renderHookWithProviders(useQueryHook, { seedCache: restoreRealRetryPolicy })

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
    expect(requestCountTo(fetchMock, url)).toBe(1)
  })

  it('is left out of the saved copy in the browser, because it holds other people\'s emails, while the signed-in person is saved', async () => {
    stubFetch(Response.json(body))
    const { result, queryClient } = await renderHookWithProviders(useQueryHook, {
      seedCache: seedMe(testAdminMe),
    })
    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    const options = savedQueryCacheOptions(queryClient, createTestPersister())
    const savedKeys = dehydrate(queryClient, options.dehydrateOptions).queries.map((query) => query.queryKey)

    expect(queryClient.getQueryCache().getAll()).toHaveLength(2)
    expect(savedKeys).toEqual([meQueryKey])
  })
})

describe('useOwnerToolsPeople with no search yet', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('asks nothing and stays pending while the query is null', async () => {
    const fetchMock = stubFetch(Response.json([]))

    const { result } = await renderHookWithProviders(() => useOwnerToolsPeople(null))

    expect(result.current.isPending).toBe(true)
    expect(result.current.fetchStatus).toBe('idle')
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('asks again for another query and keeps the two answers apart', async () => {
    const fetchMock = stubFetchByPath({
      [peopleUrl]: Response.json([anaPerson]),
      '/api/admin/tools/people?query=petar': Response.json([]),
    })

    const { result } = await renderHookWithProviders(() => useOwnerToolsPeople('petar'))
    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(result.current.data).toEqual([])
    expect(requestCountTo(fetchMock, peopleUrl)).toBe(0)
  })
})

describe('useOwnerToolsGroup with no group number yet', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('asks nothing and stays pending while the group number is null', async () => {
    const fetchMock = stubFetch(Response.json(tripGroup))

    const { result } = await renderHookWithProviders(() => useOwnerToolsGroup(null))

    expect(result.current.isPending).toBe(true)
    expect(result.current.fetchStatus).toBe('idle')
    expect(fetchMock).not.toHaveBeenCalled()
  })
})
