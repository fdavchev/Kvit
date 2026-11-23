import {
  addDuplicateAnswer,
  type DuplicateAnswer,
  type DuplicatePairIds,
} from '@/core/services/personalEntries/personalEntriesService'
import type { SaveRun } from '@/shared/hooks/saveRun'
import type { SaveOnce } from '../../categories/hooks/useSaveOnce'
import { useSaveThenRefreshEntries } from '../../entries/hooks/useSaveThenRefreshEntries'

export interface DuplicateAnswerChoice {
  pair: DuplicatePairIds
  answer: DuplicateAnswer
}

export function useAnswerDuplicate(oneAtATime: SaveOnce): SaveRun<DuplicateAnswerChoice> {
  return useSaveThenRefreshEntries(
    ({ pair, answer }: DuplicateAnswerChoice) => addDuplicateAnswer(pair, answer),
    oneAtATime,
  )
}
