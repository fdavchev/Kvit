import type { PersonalCategory } from '@/core/services/personalCategories/personalCategoriesService'
import type { LimitRow, MonthLimits } from '@/core/services/personalEntries/personalEntriesService'

export interface LimitTarget {
  category: PersonalCategory | null
  limitMinor: number | null
}

export type LimitState = 'ok' | 'near' | 'over'

export const wholeMonthEmoji = '\u{1F5D3}\u{FE0F}'

const nearShareNumerator = 4
const nearShareDenominator = 5
export function limitStateOf(row: LimitRow): LimitState {
  if (row.spentMinor > row.limitMinor) {
    return 'over'
  }
  return row.spentMinor * nearShareDenominator >= row.limitMinor * nearShareNumerator ? 'near' : 'ok'
}

export function limitFillRatio(row: LimitRow): number {
  if (row.limitMinor <= 0) {
    throw new Error(`Expected a limit above 0 for the category ${row.categoryId ?? 'of the whole month'}, got ${row.limitMinor}`)
  }
  return Math.min(row.spentMinor, row.limitMinor) / row.limitMinor
}

export function limitMinorOf(limits: MonthLimits, categoryId: string | null): number | null {
  const row = categoryId === null ? limits.month : limits.categories.find((candidate) => candidate.categoryId === categoryId)
  return row?.limitMinor ?? null
}

export function hasAnyLimit(limits: MonthLimits): boolean {
  return limits.month !== null || limits.categories.length > 0
}
