import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { letInAgain } from '@/core/services/groups/membersService'
import { refreshEveryGroup } from '../../shared/groupCache'

export function useLetInAgain(groupId: string): UseMutationResult<void, Error, string> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (memberId: string) => letInAgain(groupId, memberId),
    onSuccess: () => refreshEveryGroup(queryClient),
  })
}
