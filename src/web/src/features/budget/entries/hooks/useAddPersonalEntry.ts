import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { SaveOrQueueOutcome } from '@/core/outbox/saveOrQueue'
import {
  addPersonalEntry,
  type NewPersonalEntry,
  type PersonalEntry,
} from '@/core/services/personalEntries/personalEntriesService'
import { asSaveError, useQueuedSave } from '@/features/outbox/hooks/useQueuedSave'
import { buildPersonalEntryOutboxItem } from '@/features/outbox/queuedItems'
import type { SaveOnce } from '../../categories/hooks/useSaveOnce'
import { refreshPersonalEntries } from '../personalEntryCache'

export interface PersonalEntryRun<Input> {
  run: (input: Input, onDone: () => void, onRefused: (error: Error) => void) => void
  isPending: boolean
  error: Error | null
}

export interface PersonalEntryAdd {
  run: (
    input: NewPersonalEntry,
    onDone: (outcome: SaveOrQueueOutcome<PersonalEntry>) => void,
    onRefused: (error: Error) => void,
  ) => void
  isPending: boolean
  error: Error | null
}

export function useAddPersonalEntry(oneAtATime: SaveOnce): PersonalEntryAdd {
  const queryClient = useQueryClient()
  const queuedSave = useQueuedSave()
  const mutation = useMutation<PersonalEntry, Error, NewPersonalEntry>({
    mutationFn: addPersonalEntry,
    onSuccess: () => refreshPersonalEntries(queryClient),
  })

  return {
    run: (input, onDone, onRefused) => {
      if (!oneAtATime.tryStart()) {
        return
      }
      void queuedSave
        .saveOrQueueItem(buildPersonalEntryOutboxItem({ input }), () => mutation.mutateAsync(input))
        .finally(oneAtATime.finish)
        .then(onDone, (error: unknown) => onRefused(asSaveError(error)))
    },
    isPending: queuedSave.isPending,
    error: queuedSave.error,
  }
}
