import type { PersonalCategory } from '@/core/services/personalCategories/personalCategoriesService'
import type { MonthLimits } from '@/core/services/personalEntries/personalEntriesService'

export interface LimitTarget {
  category: PersonalCategory | null
  limitMinor: number | null
}

export const wholeMonthEmoji = '\u{1F5D3}\u{FE0F}'

export function limitMinorOf(limits: MonthLimits, categoryId: string | null): number | null {
  const row = categoryId === null ? limits.month : limits.categories.find((candidate) => candidate.categoryId === categoryId)
  return row?.limitMinor ?? null
}

export function hasAnyLimit(limits: MonthLimits): boolean {
  return limits.month !== null || limits.categories.length > 0
}
