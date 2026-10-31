import { useMutation, useQueryClient } from '@tanstack/react-query'
import { setGroupPlan, type GroupPlanInput } from '@/core/services/groups/groupsService'
import type { SaveOnce } from '@/features/budget/categories/hooks/useSaveOnce'
import type { PersonalEntryRun } from '@/features/budget/entries/hooks/useAddPersonalEntry'
import { showChangedPlan } from '../groupPlanCache'

export function useSetGroupPlan(groupId: string, oneAtATime: SaveOnce): PersonalEntryRun<GroupPlanInput> {
  const queryClient = useQueryClient()
  const mutation = useMutation<void, Error, GroupPlanInput>({
    mutationFn: (plan) => setGroupPlan(groupId, plan),
    onSuccess: (_answer, plan) =>
      showChangedPlan(queryClient, groupId, { planAmountMinor: plan.amountMinor, planCurrency: plan.currency }),
    onSettled: oneAtATime.finish,
  })

  return {
    run: (plan, onDone, onRefused) => {
      if (oneAtATime.tryStart()) {
        mutation.mutate(plan, { onSuccess: onDone, onError: onRefused })
      }
    },
    isPending: mutation.isPending,
    error: mutation.error,
  }
}
