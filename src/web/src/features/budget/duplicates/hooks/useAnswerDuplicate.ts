import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  addDuplicateAnswer,
  type DuplicateAnswer,
  type DuplicatePairIds,
} from '@/core/services/personalEntries/personalEntriesService'
import type { SaveOnce } from '../../categories/hooks/useSaveOnce'
import type { PersonalEntryRun } from '../../entries/hooks/useAddPersonalEntry'
import { refreshPersonalEntries } from '../../entries/personalEntryCache'

export interface DuplicateAnswerChoice {
  pair: DuplicatePairIds
  answer: DuplicateAnswer
}

export function useAnswerDuplicate(oneAtATime: SaveOnce): PersonalEntryRun<DuplicateAnswerChoice> {
  const queryClient = useQueryClient()
  const mutation = useMutation<void, Error, DuplicateAnswerChoice>({
    mutationFn: ({ pair, answer }) => addDuplicateAnswer(pair, answer),
    onSuccess: () => refreshPersonalEntries(queryClient),
    onSettled: oneAtATime.finish,
  })

  return {
    run: (choice, onDone, onRefused) => {
      if (oneAtATime.tryStart()) {
        mutation.mutate(choice, { onSuccess: onDone, onError: onRefused })
      }
    },
    isPending: mutation.isPending,
    error: mutation.error,
  }
}
