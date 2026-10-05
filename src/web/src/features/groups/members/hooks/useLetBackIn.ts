import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { letBackIn } from '@/core/services/groups/membersService'
import { groupsQueryKey } from '../../shared/groupCache'

export function useLetBackIn(groupId: string): UseMutationResult<void, Error, string> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (memberId: string) => letBackIn(groupId, memberId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: groupsQueryKey })
    },
  })
}
