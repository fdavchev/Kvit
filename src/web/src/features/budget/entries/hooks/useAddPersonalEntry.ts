import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { AnsweredSignal, SaveOrQueueOutcome } from '@/core/outbox/saveOrQueue'
import {
  addPersonalEntry,
  type NewPersonalEntry,
  type PersonalEntry,
} from '@/core/services/personalEntries/personalEntriesService'
import { asSaveError, useQueuedSave } from '@/features/outbox/hooks/useQueuedSave'
import { buildPersonalEntryOutboxItem } from '@/features/outbox/queuedItems'
import { withAnswered } from '@/features/outbox/withAnswered'
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

interface PersonalEntrySave extends AnsweredSignal {
  entry: NewPersonalEntry
}

export function useAddPersonalEntry(oneAtATime: SaveOnce): PersonalEntryAdd {
  const queryClient = useQueryClient()
  const queuedSave = useQueuedSave()
  const mutation = useMutation<PersonalEntry, Error, PersonalEntrySave>({
    mutationFn: withAnswered<PersonalEntrySave, PersonalEntry>(({ entry }) => addPersonalEntry(entry)),
    onSuccess: () => refreshPersonalEntries(queryClient),
  })

  return {
    run: (input, onDone, onRefused) => {
      if (!oneAtATime.tryStart()) {
        return
      }
      void queuedSave
        .saveOrQueueItem(buildPersonalEntryOutboxItem({ input }), (onAnswered) => mutation.mutateAsync({ entry: input, onAnswered }))
        .finally(oneAtATime.finish)
        .then(onDone, (error: unknown) => onRefused(asSaveError(error)))
        .catch((failure: unknown) => {
          console.error('Finishing the screen after a personal entry was saved, queued or refused failed', failure)
        })
    },
    isPending: queuedSave.isPending,
    error: queuedSave.error,
  }
}
