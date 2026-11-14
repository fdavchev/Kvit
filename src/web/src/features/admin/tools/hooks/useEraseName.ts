import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { eraseName } from '@/core/services/ownerTools/ownerToolsService'
import { refreshPeopleAndLog } from '../ownerToolsCache'
import type { OwnerToolsAction } from '../types'

export function useEraseName(): UseMutationResult<void, Error, OwnerToolsAction> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, reason }: OwnerToolsAction) => eraseName(id, reason),
    onSuccess: () => refreshPeopleAndLog(queryClient),
  })
}
