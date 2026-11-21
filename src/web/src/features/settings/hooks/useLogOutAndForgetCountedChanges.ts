import { useMe } from '@/core/auth/useMe'
import { clearOutboxIfEmpty, removeOutboxItem } from '@/core/outbox/outboxStorage'
import { useOutboxItems } from '@/core/outbox/useOutbox'
import { useLogOut } from './useLogOut'

export interface LogOutAndForgetCountedChanges {
  logOutNow: () => void
  isLoggingOut: boolean
  unsentCount: number
}

export function useLogOutAndForgetCountedChanges(onFailure: (error: Error) => void): LogOutAndForgetCountedChanges {
  const { data: me } = useMe()
  const logOut = useLogOut()
  const unsentItems = useOutboxItems()

  function logOutNow(): void {
    const personId = me?.id
    const countedIds = unsentItems.map((item) => item.id)
    logOut.mutateAsync().then(() => {
      if (personId !== undefined) {
        removeCountedChangesAndEmptyOutbox(personId, countedIds).catch((error: unknown) => {
          console.error(`Logged out, but the unsent changes of person ${personId} could not be removed from this browser`, error)
        })
      }
    }, onFailure)
  }

  return { logOutNow, isLoggingOut: logOut.isPending, unsentCount: unsentItems.length }
}

export async function removeCountedChanges(personId: string, ids: readonly string[]): Promise<void> {
  for (const id of ids) {
    await removeOutboxItem(personId, id)
  }
}

async function removeCountedChangesAndEmptyOutbox(personId: string, ids: readonly string[]): Promise<void> {
  await removeCountedChanges(personId, ids)
  await clearOutboxIfEmpty(personId)
}
