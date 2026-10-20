import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { restorePersonalEntry } from '@/core/services/personalEntries/personalEntriesService'
import { refreshPersonalEntries } from '../personalEntryCache'

export function useRestorePersonalEntry(): UseMutationResult<void, Error, string> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: restorePersonalEntry,
    onSuccess: () => refreshPersonalEntries(queryClient),
  })
}
