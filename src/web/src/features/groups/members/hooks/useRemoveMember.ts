import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { removeMember } from '@/core/services/groups/membersService'
import { refreshEveryGroup } from '../../shared/groupCache'

export function useRemoveMember(groupId: string): UseMutationResult<void, Error, string> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (memberId: string) => removeMember(groupId, memberId),
    onSuccess: () => refreshEveryGroup(queryClient),
  })
}
