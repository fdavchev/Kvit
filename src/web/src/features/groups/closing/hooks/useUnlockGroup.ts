import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { unlockGroup } from '@/core/services/groups/groupsService'
import { refreshWholeGroup } from '../../shared/groupCache'

export function useUnlockGroup(groupId: string): UseMutationResult<void, Error, void> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => unlockGroup(groupId),
    onSuccess: () => refreshWholeGroup(queryClient, groupId),
  })
}
