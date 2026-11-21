import { removeCategoryLimit, removeMonthLimit } from '@/core/services/budgetLimits/budgetLimitsService'
import type { SaveRun } from '@/shared/hooks/saveRun'
import type { SaveOnce } from '../../categories/hooks/useSaveOnce'
import { useSaveThenRefreshEntries } from '../../entries/hooks/useSaveThenRefreshEntries'

export interface LimitRemoval {
  categoryId: string | null
}

export function useRemoveLimit(oneAtATime: SaveOnce): SaveRun<LimitRemoval> {
  return useSaveThenRefreshEntries(
    ({ categoryId }: LimitRemoval) => (categoryId === null ? removeMonthLimit() : removeCategoryLimit(categoryId)),
    oneAtATime,
  )
}
