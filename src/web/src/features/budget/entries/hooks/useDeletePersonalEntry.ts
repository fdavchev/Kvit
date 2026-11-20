import { deletePersonalEntry } from '@/core/services/personalEntries/personalEntriesService'
import type { SaveRun } from '@/shared/hooks/saveRun'
import type { SaveOnce } from '../../categories/hooks/useSaveOnce'
import { useSaveThenRefreshEntries } from './useSaveThenRefreshEntries'

export function useDeletePersonalEntry(oneAtATime: SaveOnce): SaveRun<string> {
  return useSaveThenRefreshEntries(deletePersonalEntry, oneAtATime)
}
