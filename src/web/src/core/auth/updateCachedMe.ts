import type { QueryClient } from '@tanstack/react-query'
import type { Me } from '@/core/services/me/meService'
import { meQueryKey } from './useMe'

export function updateCachedMe(queryClient: QueryClient, changes: Partial<Me>): void {
  const me = queryClient.getQueryData<Me | null>(meQueryKey)
  if (me === undefined || me === null) {
    throw new Error(
      `Expected a signed-in person in the me cache to apply ${JSON.stringify(changes)}, found ${String(me)}`,
    )
  }
  queryClient.setQueryData<Me | null>(meQueryKey, { ...me, ...changes })
}
