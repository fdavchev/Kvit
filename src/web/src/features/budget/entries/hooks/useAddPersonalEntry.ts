import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  addPersonalEntry,
  type NewPersonalEntry,
  type PersonalEntry,
} from '@/core/services/personalEntries/personalEntriesService'
import { useSaveOnce } from '../../categories/hooks/useSaveOnce'
import { refreshPersonalEntries } from '../personalEntryCache'

export interface PersonalEntryRun<Input> {
  run: (input: Input, onDone: () => void, onRefused: (error: Error) => void) => void
  isPending: boolean
  error: Error | null
}

export function useAddPersonalEntry(): PersonalEntryRun<NewPersonalEntry> {
  const queryClient = useQueryClient()
  const saving = useSaveOnce()
  const mutation = useMutation<PersonalEntry, Error, NewPersonalEntry>({
    mutationFn: addPersonalEntry,
    onSuccess: () => refreshPersonalEntries(queryClient),
    onSettled: saving.finish,
  })

  return {
    run: (input, onDone, onRefused) => {
      if (saving.tryStart()) {
        mutation.mutate(input, { onSuccess: onDone, onError: onRefused })
      }
    },
    isPending: mutation.isPending,
    error: mutation.error,
  }
}
