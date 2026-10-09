import type { PersistedClient } from '@tanstack/react-query-persist-client'
import type { SavedQueryCachePersister } from '@/core/api/savedQueryCache'

export function createTestPersister(): SavedQueryCachePersister {
  let savedClient: PersistedClient | undefined
  return {
    persistClient: (client) => {
      savedClient = client
    },
    restoreClient: () => savedClient,
    removeClient: () => {
      savedClient = undefined
    },
    hasReadTimedOut: () => false,
  }
}
