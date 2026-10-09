import { matchQuery, type Query, type QueryClient } from '@tanstack/react-query'
import type { Persister } from '@tanstack/react-query-persist-client'
import type { Me } from '@/core/services/me/meService'
import { meQueryKey } from './useMe'

type PersonId = string | null

export function keepCacheToOnePerson(queryClient: QueryClient, persister: Persister): () => void {
  let cachedPersonId: PersonId | undefined

  return queryClient.getQueryCache().subscribe((event) => {
    if ((event.type !== 'added' && event.type !== 'updated') || !isMeQuery(event.query)) {
      return
    }
    const me = queryClient.getQueryData<Me | null>(meQueryKey)
    if (me === undefined) {
      return
    }
    const previousPersonId = cachedPersonId
    cachedPersonId = me === null ? null : me.id
    if (typeof previousPersonId !== 'string' || previousPersonId === cachedPersonId) {
      return
    }
    forgetDataOfPerson(queryClient, cachedPersonId)
    removeSavedCopy(persister).catch((error: unknown) => {
      console.error(
        `Could not delete the saved data of the person who was signed in before (${previousPersonId}) from this browser (IndexedDB)`,
        error,
      )
    })
  })
}

function forgetDataOfPerson(queryClient: QueryClient, newPersonId: PersonId): void {
  const isOtherQuery = (query: Query): boolean => !isMeQuery(query)
  if (newPersonId === null) {
    queryClient.removeQueries({ predicate: isOtherQuery })
    return
  }
  void queryClient.resetQueries({ predicate: isOtherQuery })
}

async function removeSavedCopy(persister: Persister): Promise<void> {
  await persister.removeClient()
}

function isMeQuery(query: Query): boolean {
  return matchQuery({ queryKey: meQueryKey, exact: true }, query)
}
