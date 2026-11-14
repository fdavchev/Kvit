import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { unsuspendPerson } from '@/core/services/ownerTools/ownerToolsService'
import { refreshPeopleAndLog } from '../ownerToolsCache'
import type { OwnerToolsAction } from '../types'

export function useUnsuspendPerson(): UseMutationResult<void, Error, OwnerToolsAction> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, reason }: OwnerToolsAction) => unsuspendPerson(id, reason),
    onSuccess: () => refreshPeopleAndLog(queryClient),
  })
}
