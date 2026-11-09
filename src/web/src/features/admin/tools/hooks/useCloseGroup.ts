import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { closeGroup } from '@/core/services/ownerTools/ownerToolsService'
import { refreshGroupAndLog } from '../ownerToolsCache'
import type { OwnerToolsAction } from '../types'

export function useCloseGroup(): UseMutationResult<void, Error, OwnerToolsAction> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, reason }: OwnerToolsAction) => closeGroup(id, reason),
    onSuccess: () => refreshGroupAndLog(queryClient),
  })
}
