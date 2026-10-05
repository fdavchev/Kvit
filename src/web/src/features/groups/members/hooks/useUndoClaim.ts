import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { undoClaim } from '@/core/services/groups/membersService'
import { groupsQueryKey } from '../../shared/groupCache'

export function useUndoClaim(groupId: string): UseMutationResult<void, Error, string> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (memberId: string) => undoClaim(groupId, memberId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: groupsQueryKey })
    },
  })
}
