import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { claimName } from '@/core/services/groups/membersService'
import { refreshEveryGroup } from '../../shared/groupCache'

export function useClaimName(groupId: string): UseMutationResult<void, Error, string> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (memberId: string) => claimName(groupId, memberId),
    onSuccess: () => refreshEveryGroup(queryClient),
  })
}
