import { createAsyncStoragePersister } from '@tanstack/query-async-storage-persister'
import { defaultShouldDehydrateQuery, type QueryClient } from '@tanstack/react-query'
import type { PersistQueryClientOptions, Persister } from '@tanstack/react-query-persist-client'
import { del, get, set } from 'idb-keyval'
import { meQueryKey } from '@/core/auth/useMe'
import type { Me } from '@/core/services/me/meService'

export const savedQueryCacheVersion = '1'
export const savedQueryCacheMaxAge = 24 * 60 * 60 * 1000
export const savedCopyReadTimeLimitMs = 3000

const savedQueryCacheKey = 'kvit-query-cache'

export type SavedQueryCacheOptions = Omit<PersistQueryClientOptions, 'queryClient'>

export interface SavedQueryCachePersister extends Persister {
  hasReadTimedOut: () => boolean
}

export function createSavedQueryCachePersister(): SavedQueryCachePersister {
  let hasReadTimedOut = false
  const persister = createAsyncStoragePersister({
    storage: {
      getItem: (key) =>
        readWithinTimeLimit(get<string>(key), () => {
          hasReadTimedOut = true
        }),
      setItem: (key, value) => set(key, value),
      removeItem: (key) => (hasReadTimedOut ? startDeletingWithoutWaiting(key) : del(key)),
    },
    key: savedQueryCacheKey,
    retry: ({ error }) => {
      console.error('Could not save the data to this browser (IndexedDB)', error)
      return undefined
    },
  })
  return { ...persister, hasReadTimedOut: () => hasReadTimedOut }
}

export function savedQueryCacheOptions(
  queryClient: QueryClient,
  persister: Persister,
): SavedQueryCacheOptions {
  return {
    persister,
    maxAge: savedQueryCacheMaxAge,
    buster: savedQueryCacheVersion,
    dehydrateOptions: {
      shouldDehydrateQuery: (query) =>
        defaultShouldDehydrateQuery(query) && hasConfirmedSignedInPerson(queryClient),
      shouldDehydrateMutation: () => false,
    },
  }
}

export function checkRestoredMeWithServer(queryClient: QueryClient): Promise<void> {
  return queryClient.invalidateQueries({ queryKey: meQueryKey, refetchType: 'none' })
}

export function reportRestoreFailure(persister: SavedQueryCachePersister): void {
  if (persister.hasReadTimedOut()) {
    console.error(
      `Reading the saved data from this browser (IndexedDB) took longer than ${savedCopyReadTimeLimitMs} ms; Kvit stopped waiting and starts with an empty cache`,
    )
    return
  }
  console.error(
    'Could not restore the saved data from this browser (IndexedDB); Kvit starts with an empty cache',
  )
}

function readWithinTimeLimit(
  read: Promise<string | undefined>,
  onTimeout: () => void,
): Promise<string | undefined> {
  let timer: ReturnType<typeof setTimeout> | undefined
  const timeLimit = new Promise<never>((_resolve, reject) => {
    timer = setTimeout(() => {
      onTimeout()
      reject(
        new Error(
          `Reading the saved data from this browser (IndexedDB) took longer than ${savedCopyReadTimeLimitMs} ms`,
        ),
      )
    }, savedCopyReadTimeLimitMs)
  })
  return Promise.race([read, timeLimit]).finally(() => {
    clearTimeout(timer)
  })
}

function startDeletingWithoutWaiting(key: string): void {
  del(key).catch((error: unknown) => {
    console.error(
      'Could not delete the saved data from this browser (IndexedDB) after reading it took too long',
      error,
    )
  })
}

function hasConfirmedSignedInPerson(queryClient: QueryClient): boolean {
  const meState = queryClient.getQueryState<Me | null>(meQueryKey)
  return meState?.status === 'success' && meState.data !== null && meState.data !== undefined
}
