import { setCategoryLimit, setMonthLimit } from '@/core/services/budgetLimits/budgetLimitsService'
import type { SaveRun } from '@/shared/hooks/saveRun'
import type { SaveOnce } from '../../categories/hooks/useSaveOnce'
import { useSaveThenRefreshEntries } from '../../entries/hooks/useSaveThenRefreshEntries'

export interface LimitChange {
  categoryId: string | null
  limitMinor: number
}

export function useSetLimit(oneAtATime: SaveOnce): SaveRun<LimitChange> {
  return useSaveThenRefreshEntries(
    ({ categoryId, limitMinor }: LimitChange) =>
      categoryId === null ? setMonthLimit(limitMinor) : setCategoryLimit(categoryId, limitMinor),
    oneAtATime,
  )
}
