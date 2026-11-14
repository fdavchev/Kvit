import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { reopenGroup } from '@/core/services/ownerTools/ownerToolsService'
import { refreshGroupAndLog } from '../ownerToolsCache'
import type { OwnerToolsAction } from '../types'

export function useReopenGroup(): UseMutationResult<void, Error, OwnerToolsAction> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, reason }: OwnerToolsAction) => reopenGroup(id, reason),
    onSuccess: () => refreshGroupAndLog(queryClient),
  })
}
