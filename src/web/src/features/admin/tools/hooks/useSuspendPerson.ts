import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { suspendPerson } from '@/core/services/ownerTools/ownerToolsService'
import { refreshPeopleAndLog } from '../ownerToolsCache'
import type { OwnerToolsAction } from '../types'

export function useSuspendPerson(): UseMutationResult<void, Error, OwnerToolsAction> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, reason }: OwnerToolsAction) => suspendPerson(id, reason),
    onSuccess: () => refreshPeopleAndLog(queryClient),
  })
}
