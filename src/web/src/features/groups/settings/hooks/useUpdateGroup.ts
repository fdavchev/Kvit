import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { updateGroup, type GroupInput } from '@/core/services/groups/groupsService'
import { groupsQueryKey } from '../../shared/groupCache'

export function useUpdateGroup(groupId: string): UseMutationResult<void, Error, GroupInput> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: GroupInput) => updateGroup(groupId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: groupsQueryKey })
    },
  })
}
