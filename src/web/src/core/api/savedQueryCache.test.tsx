import { dehydrate, onlineManager, QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { renderHook, waitFor } from '@testing-library/react'
import { del, get } from 'idb-keyval'
import { createElement, type ReactNode } from 'react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { meQueryKey, useMe } from '@/core/auth/useMe'
import type { Me } from '@/core/services/me/meService'
import { captureError, requestCountTo, stubFetch } from '@/test/apiTestHelpers'
import { testMe } from '@/test/testMe'
import { createTestPersister } from '@/test/testPersister'
import { endpoints } from './endpoints'
import { createQueryClient } from './queryClient'
import {
  checkRestoredMeWithServer,
  createSavedQueryCachePersister,
  reportRestoreFailure,
  savedCopyReadTimeLimitMs,
  savedQueryCacheMaxAge,
  savedQueryCacheOptions,
  savedQueryCacheVersion,
} from './savedQueryCache'

vi.mock('idb-keyval', () => ({ get: vi.fn(), set: vi.fn(), del: vi.fn() }))

const groupsKey = ['groups']
const groups = ['Greece trip']

function createClient(): QueryClient {
  return createQueryClient(createTestPersister())
}

function savedQueryKeys(queryClient: QueryClient): unknown[] {
  const options = savedQueryCacheOptions(queryClient, createTestPersister())
  return dehydrate(queryClient, options.dehydrateOptions).queries.map((query) => query.queryKey)
}

function savedMutationCount(queryClient: QueryClient): number {
  const options = savedQueryCacheOptions(queryClient, createTestPersister())
  return dehydrate(queryClient, options.dehydrateOptions).mutations.length
}

function wrapperFor(queryClient: QueryClient): (props: { children: ReactNode }) => ReactNode {
  return ({ children }) => createElement(QueryClientProvider, { client: queryClient }, children)
}

describe('savedQueryCacheOptions', () => {
  afterEach(() => {
    onlineManager.setOnline(true)
  })

  it('saves successful queries when a real person is signed in', () => {
    const queryClient = createClient()
    queryClient.setQueryData<Me | null>(meQueryKey, testMe)
    queryClient.setQueryData(groupsKey, groups)

    expect(savedQueryKeys(queryClient)).toEqual([meQueryKey, groupsKey])
  })

  it('saves nothing when me is null', () => {
    const queryClient = createClient()
    queryClient.setQueryData<Me | null>(meQueryKey, null)
    queryClient.setQueryData(groupsKey, groups)

    expect(savedQueryKeys(queryClient)).toEqual([])
  })

  it('saves nothing when me is not in the cache at all', () => {
    const queryClient = createClient()
    queryClient.setQueryData(groupsKey, groups)

    expect(savedQueryKeys(queryClient)).toEqual([])
  })

  it('saves nothing when me is in error', async () => {
    const queryClient = createClient()
    queryClient.setQueryDefaults(meQueryKey, { retry: false })
    await captureError(
      queryClient.fetchQuery({
        queryKey: meQueryKey,
        queryFn: () => Promise.reject(new Error('me failed')),
      }),
    )
    queryClient.setQueryData(groupsKey, groups)

    expect(queryClient.getQueryState(meQueryKey)?.status).toBe('error')
    expect(savedQueryKeys(queryClient)).toEqual([])
  })

  it('saves nothing when me is still being asked', () => {
    const queryClient = createClient()
    void queryClient.prefetchQuery({
      queryKey: meQueryKey,
      queryFn: () => new Promise<Me | null>(() => {}),
    })
    queryClient.setQueryData(groupsKey, groups)

    expect(queryClient.getQueryState(meQueryKey)?.status).toBe('pending')
    expect(savedQueryKeys(queryClient)).toEqual([])
  })

  it('leaves out queries that failed or are still pending but keeps the successful ones', async () => {
    const queryClient = createClient()
    queryClient.setQueryData<Me | null>(meQueryKey, testMe)
    queryClient.setQueryData(groupsKey, groups)
    await captureError(
      queryClient.fetchQuery({
        queryKey: ['broken'],
        queryFn: () => Promise.reject(new Error('broken failed')),
        retry: false,
      }),
    )
    void queryClient.prefetchQuery({
      queryKey: ['slow'],
      queryFn: () => new Promise<string>(() => {}),
    })

    expect(savedQueryKeys(queryClient)).toEqual([meQueryKey, groupsKey])
  })

  it('never saves mutations, even one that is waiting to be sent', async () => {
    const queryClient = createClient()
    queryClient.setQueryData<Me | null>(meQueryKey, testMe)
    onlineManager.setOnline(false)
    const waitingMutation = queryClient
      .getMutationCache()
      .build(queryClient, { mutationFn: () => Promise.resolve('saved') })
    void waitingMutation.execute(undefined)

    expect(dehydrate(queryClient).mutations).toHaveLength(1)
    expect(savedMutationCount(queryClient)).toBe(0)
  })

  it('sets the saved copy to expire after one day', () => {
    const options = savedQueryCacheOptions(createClient(), createTestPersister())

    expect(options.maxAge).toBe(savedQueryCacheMaxAge)
    expect(savedQueryCacheMaxAge).toBe(24 * 60 * 60 * 1000)
  })

  it('sets a version string so an older saved copy is thrown away', () => {
    const options = savedQueryCacheOptions(createClient(), createTestPersister())

    expect(options.buster).toBe(savedQueryCacheVersion)
    expect(savedQueryCacheVersion).not.toBe('')
  })

  it('uses the persister it is given', () => {
    const persister = createTestPersister()

    expect(savedQueryCacheOptions(createClient(), persister).persister).toBe(persister)
  })

  it('keeps unused queries in memory at least as long as the saved copy lives', () => {
    const gcTime = createClient().getDefaultOptions().queries?.gcTime

    expect(gcTime).toBeGreaterThanOrEqual(savedQueryCacheMaxAge)
  })
})

describe('checkRestoredMeWithServer', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('marks me as old without asking the server by itself', async () => {
    const fetchMock = stubFetch(Response.json(testMe))
    const queryClient = createClient()
    queryClient.setQueryData<Me | null>(meQueryKey, testMe)

    await checkRestoredMeWithServer(queryClient)

    expect(queryClient.getQueryState(meQueryKey)?.isInvalidated).toBe(true)
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('makes the first screen that uses me ask the server once and keep the restored person meanwhile', async () => {
    const fetchMock = stubFetch(Response.json(testMe))
    const queryClient = createClient()
    queryClient.setQueryData<Me | null>(meQueryKey, testMe)
    await checkRestoredMeWithServer(queryClient)

    const { result } = renderHook(() => useMe(), { wrapper: wrapperFor(queryClient) })

    expect(result.current.data).toEqual(testMe)
    await waitFor(() => {
      expect(queryClient.getQueryState(meQueryKey)?.isInvalidated).toBe(false)
    })
    expect(requestCountTo(fetchMock, endpoints.me)).toBe(1)
  })

  it('does not ask the server again for screens that use me later', async () => {
    const fetchMock = stubFetch(Response.json(testMe))
    const queryClient = createClient()
    queryClient.setQueryData<Me | null>(meQueryKey, testMe)
    await checkRestoredMeWithServer(queryClient)
    const first = renderHook(() => useMe(), { wrapper: wrapperFor(queryClient) })
    await waitFor(() => {
      expect(queryClient.getQueryState(meQueryKey)?.isInvalidated).toBe(false)
    })
    first.unmount()

    const second = renderHook(() => useMe(), { wrapper: wrapperFor(queryClient) })
    const third = renderHook(() => useMe(), { wrapper: wrapperFor(queryClient) })

    expect(second.result.current.data).toEqual(testMe)
    expect(third.result.current.data).toEqual(testMe)
    expect(queryClient.getQueryState(meQueryKey)?.fetchStatus).toBe('idle')
    expect(requestCountTo(fetchMock, endpoints.me)).toBe(1)
  })

  it('does not ask the server at all when me was never restored', async () => {
    const fetchMock = stubFetch(Response.json(testMe))
    const queryClient = createClient()

    await checkRestoredMeWithServer(queryClient)

    expect(fetchMock).not.toHaveBeenCalled()
  })
})

describe('reportRestoreFailure', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('logs one specific message about the saved data that could not be restored', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})

    reportRestoreFailure(createTestPersister())

    expect(consoleError).toHaveBeenCalledTimes(1)
    expect(consoleError).toHaveBeenCalledWith(
      'Could not restore the saved data from this browser (IndexedDB); Kvit starts with an empty cache',
    )
  })

  it('logs that it stopped waiting when reading the saved data took longer than the time limit', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})

    reportRestoreFailure({ ...createTestPersister(), hasReadTimedOut: () => true })

    expect(consoleError).toHaveBeenCalledTimes(1)
    expect(consoleError).toHaveBeenCalledWith(
      'Reading the saved data from this browser (IndexedDB) took longer than 3000 ms; Kvit stopped waiting and starts with an empty cache',
    )
  })
})

describe('createSavedQueryCachePersister', () => {
  const savedCopyText = JSON.stringify({ timestamp: 1, buster: savedQueryCacheVersion, clientState: {} })

  afterEach(() => {
    vi.useRealTimers()
    vi.resetAllMocks()
    vi.restoreAllMocks()
  })

  function fakeTheTimeouts(): void {
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] })
  }

  it('waits at most 3 seconds for the saved copy', () => {
    expect(savedCopyReadTimeLimitMs).toBe(3000)
  })

  it('gives back the saved copy that was read in time and stops its timer', async () => {
    fakeTheTimeouts()
    vi.mocked(get).mockResolvedValue(savedCopyText)
    const persister = createSavedQueryCachePersister()

    const restored = await persister.restoreClient()

    expect(restored).toEqual(JSON.parse(savedCopyText))
    expect(vi.getTimerCount()).toBe(0)
    expect(persister.hasReadTimedOut()).toBe(false)
  })

  it('fails with a specific error once reading the saved copy takes longer than the time limit', async () => {
    fakeTheTimeouts()
    vi.mocked(get).mockReturnValue(new Promise(() => {}))
    const persister = createSavedQueryCachePersister()

    const restoring = Promise.resolve(persister.restoreClient())
    const failure = expect(restoring).rejects.toThrow(
      'Reading the saved data from this browser (IndexedDB) took longer than 3000 ms',
    )
    await vi.advanceTimersByTimeAsync(savedCopyReadTimeLimitMs)

    await failure
    expect(persister.hasReadTimedOut()).toBe(true)
  })

  it('does not fail before the time limit is reached', async () => {
    fakeTheTimeouts()
    vi.mocked(get).mockReturnValue(new Promise(() => {}))
    const persister = createSavedQueryCachePersister()
    const onFailure = vi.fn()

    void Promise.resolve(persister.restoreClient()).catch(onFailure)
    await vi.advanceTimersByTimeAsync(savedCopyReadTimeLimitMs - 1)

    expect(onFailure).not.toHaveBeenCalled()
    expect(persister.hasReadTimedOut()).toBe(false)
  })

  it('does not wait for deleting the saved copy after reading it took too long', async () => {
    fakeTheTimeouts()
    vi.mocked(get).mockReturnValue(new Promise(() => {}))
    vi.mocked(del).mockReturnValue(new Promise(() => {}))
    const persister = createSavedQueryCachePersister()
    const restoring = Promise.resolve(persister.restoreClient()).catch(() => undefined)
    await vi.advanceTimersByTimeAsync(savedCopyReadTimeLimitMs)
    await restoring

    await persister.removeClient()

    expect(del).toHaveBeenCalledTimes(1)
  })

  it('logs a failed delete it did not wait for after reading took too long', async () => {
    fakeTheTimeouts()
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    const deleteFailure = new Error('IndexedDB delete failed')
    vi.mocked(get).mockReturnValue(new Promise(() => {}))
    vi.mocked(del).mockRejectedValue(deleteFailure)
    const persister = createSavedQueryCachePersister()
    const restoring = Promise.resolve(persister.restoreClient()).catch(() => undefined)
    await vi.advanceTimersByTimeAsync(savedCopyReadTimeLimitMs)
    await restoring

    await persister.removeClient()
    await vi.waitFor(() => {
      expect(consoleError).toHaveBeenCalledWith(
        'Could not delete the saved data from this browser (IndexedDB) after reading it took too long',
        deleteFailure,
      )
    })
  })

  it('waits for deleting the saved copy and passes its failure on while reading works', async () => {
    const deleteFailure = new Error('IndexedDB delete failed')
    vi.mocked(del).mockRejectedValue(deleteFailure)
    const persister = createSavedQueryCachePersister()

    await expect(Promise.resolve(persister.removeClient())).rejects.toBe(deleteFailure)
  })
})
