import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { restoreGroup } from '@/core/services/groups/groupsService'
import { groupsQueryKey } from '../../shared/groupCache'

export function useRestoreGroup(): UseMutationResult<void, Error, string> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: restoreGroup,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: groupsQueryKey })
    },
  })
}
