import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { claimName } from '@/core/services/groups/membersService'
import { groupsQueryKey } from '../../shared/groupCache'

export function useClaimName(groupId: string): UseMutationResult<void, Error, string> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (memberId: string) => claimName(groupId, memberId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: groupsQueryKey })
    },
  })
}
