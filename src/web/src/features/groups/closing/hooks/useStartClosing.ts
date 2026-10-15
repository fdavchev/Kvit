import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { startClosing } from '@/core/services/groups/groupsService'
import { refreshWholeGroup } from '../../shared/groupCache'

export function useStartClosing(groupId: string): UseMutationResult<void, Error, void> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => startClosing(groupId),
    onSuccess: () => refreshWholeGroup(queryClient, groupId),
  })
}
