import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { updateGroup, type GroupInput } from '@/core/services/groups/groupsService'
import { refreshEveryGroup } from '../../shared/groupCache'

export function useUpdateGroup(groupId: string): UseMutationResult<void, Error, GroupInput> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: GroupInput) => updateGroup(groupId, input),
    onSuccess: () => refreshEveryGroup(queryClient),
  })
}
