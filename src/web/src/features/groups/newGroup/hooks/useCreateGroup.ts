import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { createGroup, type Group, type GroupInput } from '@/core/services/groups/groupsService'
import { groupsQueryKey } from '../../shared/groupCache'

export function useCreateGroup(): UseMutationResult<Group, Error, GroupInput> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: createGroup,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: groupsQueryKey, exact: true })
    },
  })
}
