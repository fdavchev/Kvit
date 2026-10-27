import type { TFunction } from 'i18next'
import type { PersonalCategory } from '@/core/services/personalCategories/personalCategoriesService'
import type {
  DuplicatePairIds,
  GroupShare,
  PersonalEntry,
} from '@/core/services/personalEntries/personalEntriesService'
import { monthOfDate } from '@/shared/utils/calendarMonth'
import { addDays } from '@/shared/utils/formatDate'
import { personalCategoryName } from '../categories/personalCategoryName'
import { groupShareCategory } from '../overview/groupShareTotals'

const maxDaysApart = 3

export function duplicatePairKey({ entryId, expenseId }: DuplicatePairIds): string {
  return `${entryId}:${expenseId}`
}

export function entryOfPair(pair: DuplicatePairIds, entries: readonly PersonalEntry[]): PersonalEntry {
  const entry = entries.find((candidate) => candidate.id === pair.entryId)
  if (entry === undefined) {
    throw new Error(
      `The possible duplicate of the expense ${pair.expenseId} names the entry ${pair.entryId}, which is not among the entries of the month`,
    )
  }
  return entry
}

export function spendingCategoryOf(entry: PersonalEntry): PersonalCategory {
  if (entry.category === null) {
    throw new Error(`The entry ${entry.id} has no category, so it cannot be a duplicate of a group expense`)
  }
  return entry.category
}

export function groupExpenseName(title: string | null, category: PersonalCategory, t: TFunction): string {
  return title ?? personalCategoryName(category, t)
}

export function mergedEntryLabel(
  entry: PersonalEntry,
  groupShares: readonly GroupShare[],
  categories: readonly PersonalCategory[],
  t: TFunction,
): string | null {
  const expenseId = entry.mergedWithExpenseId
  if (expenseId === null) {
    return null
  }
  const share = groupShares.find((candidate) => candidate.expenseId === expenseId)
  if (share !== undefined) {
    const category = groupShareCategory(share, categories) ?? spendingCategoryOf(entry)
    return t('budget.duplicates.mergedWith', {
      expense: groupExpenseName(share.title, category, t),
      group: share.groupName,
    })
  }
  if (canMatchOutsideItsMonth(entry.entryDate)) {
    return t('budget.duplicates.mergedWithGroupExpense')
  }
  throw new Error(
    `The entry ${entry.id} is merged with the expense ${expenseId}, which is not among the group shares of ${monthOfDate(entry.entryDate)}`,
  )
}

function canMatchOutsideItsMonth(entryDate: string): boolean {
  const month = monthOfDate(entryDate)
  return (
    monthOfDate(addDays(entryDate, -maxDaysApart)) !== month || monthOfDate(addDays(entryDate, maxDaysApart)) !== month
  )
}
