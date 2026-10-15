import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { makeOwner } from '@/core/services/groups/membersService'
import { refreshEveryGroup } from '../../shared/groupCache'

export function useMakeOwner(groupId: string): UseMutationResult<void, Error, string> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (memberId: string) => makeOwner(groupId, memberId),
    onSuccess: () => refreshEveryGroup(queryClient),
  })
}
