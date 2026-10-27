import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  removeDuplicateAnswer,
  type DuplicatePairIds,
} from '@/core/services/personalEntries/personalEntriesService'
import type { SaveOnce } from '../../categories/hooks/useSaveOnce'
import type { PersonalEntryRun } from '../../entries/hooks/useAddPersonalEntry'
import { refreshPersonalEntries } from '../../entries/personalEntryCache'

export function useRemoveDuplicateAnswer(oneAtATime: SaveOnce): PersonalEntryRun<DuplicatePairIds> {
  const queryClient = useQueryClient()
  const mutation = useMutation<void, Error, DuplicatePairIds>({
    mutationFn: removeDuplicateAnswer,
    onSuccess: () => refreshPersonalEntries(queryClient),
    onSettled: oneAtATime.finish,
  })

  return {
    run: (pair, onDone, onRefused) => {
      if (oneAtATime.tryStart()) {
        mutation.mutate(pair, { onSuccess: onDone, onError: onRefused })
      }
    },
    isPending: mutation.isPending,
    error: mutation.error,
  }
}
