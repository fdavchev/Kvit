import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  editPersonalEntry,
  type PersonalEntry,
  type PersonalEntryFields,
} from '@/core/services/personalEntries/personalEntriesService'
import { useSaveOnce } from '../../categories/hooks/useSaveOnce'
import { refreshPersonalEntries } from '../personalEntryCache'
import type { PersonalEntryRun } from './useAddPersonalEntry'

export interface PersonalEntryEdit {
  entryId: string
  fields: PersonalEntryFields
}

export function useEditPersonalEntry(): PersonalEntryRun<PersonalEntryEdit> {
  const queryClient = useQueryClient()
  const saving = useSaveOnce()
  const mutation = useMutation<PersonalEntry, Error, PersonalEntryEdit>({
    mutationFn: ({ entryId, fields }) => editPersonalEntry(entryId, fields),
    onSuccess: () => refreshPersonalEntries(queryClient),
    onSettled: saving.finish,
  })

  return {
    run: (edit, onDone, onRefused) => {
      if (saving.tryStart()) {
        mutation.mutate(edit, { onSuccess: onDone, onError: onRefused })
      }
    },
    isPending: mutation.isPending,
    error: mutation.error,
  }
}
