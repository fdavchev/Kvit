import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { deleteGroup } from '@/core/services/groups/groupsService'
import { forgetGroup } from '../../shared/groupCache'

export function useDeleteGroup(groupId: string): UseMutationResult<void, Error, void> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => deleteGroup(groupId),
    onSuccess: () => forgetGroup(queryClient, groupId),
  })
}
