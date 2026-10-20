import { useMutation, useQueryClient } from '@tanstack/react-query'
import { deletePersonalEntry } from '@/core/services/personalEntries/personalEntriesService'
import type { SaveOnce } from '../../categories/hooks/useSaveOnce'
import { refreshPersonalEntries } from '../personalEntryCache'
import type { PersonalEntryRun } from './useAddPersonalEntry'

export function useDeletePersonalEntry(oneAtATime: SaveOnce): PersonalEntryRun<string> {
  const queryClient = useQueryClient()
  const mutation = useMutation<void, Error, string>({
    mutationFn: deletePersonalEntry,
    onSuccess: () => refreshPersonalEntries(queryClient),
    onSettled: oneAtATime.finish,
  })

  return {
    run: (entryId, onDone, onRefused) => {
      if (oneAtATime.tryStart()) {
        mutation.mutate(entryId, { onSuccess: onDone, onError: onRefused })
      }
    },
    isPending: mutation.isPending,
    error: mutation.error,
  }
}
