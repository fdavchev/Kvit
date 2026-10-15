import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { undoResetInviteLink } from '@/core/services/groups/membersService'
import { refreshEveryGroup } from '../../shared/groupCache'

export function useUndoResetInviteLink(groupId: string): UseMutationResult<string, Error, void> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => undoResetInviteLink(groupId),
    onSuccess: () => refreshEveryGroup(queryClient),
  })
}
