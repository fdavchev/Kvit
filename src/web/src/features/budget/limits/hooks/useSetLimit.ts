import { useMutation, useQueryClient } from '@tanstack/react-query'
import { setCategoryLimit, setMonthLimit } from '@/core/services/budgetLimits/budgetLimitsService'
import type { SaveOnce } from '../../categories/hooks/useSaveOnce'
import type { PersonalEntryRun } from '../../entries/hooks/useAddPersonalEntry'
import { refreshPersonalEntries } from '../../entries/personalEntryCache'

export interface LimitChange {
  categoryId: string | null
  limitMinor: number
}

export function useSetLimit(oneAtATime: SaveOnce): PersonalEntryRun<LimitChange> {
  const queryClient = useQueryClient()
  const mutation = useMutation<void, Error, LimitChange>({
    mutationFn: ({ categoryId, limitMinor }) =>
      categoryId === null ? setMonthLimit(limitMinor) : setCategoryLimit(categoryId, limitMinor),
    onSuccess: () => refreshPersonalEntries(queryClient),
    onSettled: oneAtATime.finish,
  })

  return {
    run: (change, onDone, onRefused) => {
      if (oneAtATime.tryStart()) {
        mutation.mutate(change, { onSuccess: onDone, onError: onRefused })
      }
    },
    isPending: mutation.isPending,
    error: mutation.error,
  }
}
