import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { joinGroup } from '@/core/services/invites/invitesService'
import { groupsQueryKey } from '../../shared/groupCache'

export interface JoinRequest {
  token: string
  claimMemberId?: string
}

export function useJoinGroup(): UseMutationResult<string, Error, JoinRequest> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ token, claimMemberId }: JoinRequest) => joinGroup(token, claimMemberId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: groupsQueryKey })
    },
  })
}
