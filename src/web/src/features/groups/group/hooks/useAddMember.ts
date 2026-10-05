import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { addMember } from '@/core/services/groups/groupsService'
import { groupsQueryKey } from '../../shared/groupCache'

export function useAddMember(groupId: string): UseMutationResult<void, Error, string> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (name: string) => addMember(groupId, name),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: groupsQueryKey })
    },
  })
}
