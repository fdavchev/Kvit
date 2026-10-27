import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { changeCountsInBudget, type Group } from '@/core/services/groups/groupsService'
import { groupQueryKey } from '../../shared/groupCache'

export function useChangeCountsInBudget(groupId: string): UseMutationResult<void, Error, boolean> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (countsInBudget: boolean) => changeCountsInBudget(groupId, countsInBudget),
    onSuccess: (_answer, countsInBudget) => {
      const group = queryClient.getQueryData<Group>(groupQueryKey(groupId))
      if (group === undefined) {
        return
      }
      queryClient.setQueryData<Group>(groupQueryKey(groupId), { ...group, countsInBudget })
    },
  })
}
