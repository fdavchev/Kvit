import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { resetInviteLink } from '@/core/services/groups/membersService'
import { groupsQueryKey } from '../../shared/groupCache'

export function useResetInviteLink(groupId: string): UseMutationResult<string, Error, void> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => resetInviteLink(groupId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: groupsQueryKey })
    },
  })
}
