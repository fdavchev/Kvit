import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { objectToClosing } from '@/core/services/groups/groupsService'
import { refreshWholeGroup } from '../../shared/groupCache'

export function useObjectToClosing(groupId: string): UseMutationResult<void, Error, string | undefined> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (reason: string | undefined) => objectToClosing(groupId, reason),
    onSuccess: () => refreshWholeGroup(queryClient, groupId),
  })
}
