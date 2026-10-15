import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { confirmClosing } from '@/core/services/groups/groupsService'
import { refreshWholeGroup } from '../../shared/groupCache'

export function useConfirmClosing(groupId: string): UseMutationResult<void, Error, void> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => confirmClosing(groupId),
    onSuccess: () => refreshWholeGroup(queryClient, groupId),
  })
}
