import { useMutation, useQueryClient } from '@tanstack/react-query'
import { deletePersonalEntry } from '@/core/services/personalEntries/personalEntriesService'
import { useSaveOnce } from '../../categories/hooks/useSaveOnce'
import { refreshPersonalEntries } from '../personalEntryCache'
import type { PersonalEntryRun } from './useAddPersonalEntry'

export function useDeletePersonalEntry(): PersonalEntryRun<string> {
  const queryClient = useQueryClient()
  const deleting = useSaveOnce()
  const mutation = useMutation<void, Error, string>({
    mutationFn: deletePersonalEntry,
    onSuccess: () => refreshPersonalEntries(queryClient),
    onSettled: deleting.finish,
  })

  return {
    run: (entryId, onDone, onRefused) => {
      if (deleting.tryStart()) {
        mutation.mutate(entryId, { onSuccess: onDone, onError: onRefused })
      }
    },
    isPending: mutation.isPending,
    error: mutation.error,
  }
}
