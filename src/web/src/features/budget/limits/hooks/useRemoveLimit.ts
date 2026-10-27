import { useMutation, useQueryClient } from '@tanstack/react-query'
import { removeCategoryLimit, removeMonthLimit } from '@/core/services/budgetLimits/budgetLimitsService'
import type { SaveOnce } from '../../categories/hooks/useSaveOnce'
import type { PersonalEntryRun } from '../../entries/hooks/useAddPersonalEntry'
import { refreshPersonalEntries } from '../../entries/personalEntryCache'

export interface LimitRemoval {
  categoryId: string | null
}

export function useRemoveLimit(oneAtATime: SaveOnce): PersonalEntryRun<LimitRemoval> {
  const queryClient = useQueryClient()
  const mutation = useMutation<void, Error, LimitRemoval>({
    mutationFn: ({ categoryId }) => (categoryId === null ? removeMonthLimit() : removeCategoryLimit(categoryId)),
    onSuccess: () => refreshPersonalEntries(queryClient),
    onSettled: oneAtATime.finish,
  })

  return {
    run: (removal, onDone, onRefused) => {
      if (oneAtATime.tryStart()) {
        mutation.mutate(removal, { onSuccess: onDone, onError: onRefused })
      }
    },
    isPending: mutation.isPending,
    error: mutation.error,
  }
}
