import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { resetInviteLink } from '@/core/services/groups/membersService'
import { refreshEveryGroup } from '../../shared/groupCache'

export function useResetInviteLink(groupId: string): UseMutationResult<string, Error, void> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => resetInviteLink(groupId),
    onSuccess: () => refreshEveryGroup(queryClient),
  })
}
