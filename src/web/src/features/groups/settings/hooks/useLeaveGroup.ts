import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { leaveGroup } from '@/core/services/groups/groupsService'
import { forgetGroup } from '../../shared/groupCache'

export function useLeaveGroup(groupId: string): UseMutationResult<void, Error, void> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => leaveGroup(groupId),
    onSuccess: () => forgetGroup(queryClient, groupId),
  })
}
