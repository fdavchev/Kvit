import { useMutation, useQueryClient } from '@tanstack/react-query'
import { removeGroupPlan } from '@/core/services/groups/groupsService'
import type { SaveOnce } from '@/features/budget/categories/hooks/useSaveOnce'
import type { PersonalEntryRun } from '@/features/budget/entries/hooks/useAddPersonalEntry'
import { showChangedPlan } from '../groupPlanCache'

export function useRemoveGroupPlan(groupId: string, oneAtATime: SaveOnce): PersonalEntryRun<void> {
  const queryClient = useQueryClient()
  const mutation = useMutation<void, Error, void>({
    mutationFn: () => removeGroupPlan(groupId),
    onSuccess: () => showChangedPlan(queryClient, groupId, { planAmountMinor: null, planCurrency: null }),
    onSettled: oneAtATime.finish,
  })

  return {
    run: (_input, onDone, onRefused) => {
      if (oneAtATime.tryStart()) {
        mutation.mutate(undefined, { onSuccess: onDone, onError: onRefused })
      }
    },
    isPending: mutation.isPending,
    error: mutation.error,
  }
}
