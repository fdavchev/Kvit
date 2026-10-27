import { groupCurrencies } from '@/core/services/groups/groupsService'
import type { PersonalCategory } from '@/core/services/personalCategories/personalCategoriesService'
import type {
  GroupShare,
  PersonalCategoryTotal,
  PersonalCurrencyTotal,
} from '@/core/services/personalEntries/personalEntriesService'

export function groupShareCategory(share: GroupShare, categories: readonly PersonalCategory[]): PersonalCategory | null {
  if (share.categoryId === null) {
    return null
  }
  const category = categories.find((candidate) => candidate.id === share.categoryId)
  if (category === undefined) {
    throw new Error(
      `The category ${share.categoryId} of the group share of expense ${share.expenseId} is not one of the personal categories`,
    )
  }
  return category
}

export function spendingWithGroupShares(
  totals: readonly PersonalCurrencyTotal[],
  groupShares: readonly GroupShare[],
  categories: readonly PersonalCategory[],
): PersonalCurrencyTotal[] {
  const shareOnlyCurrencies = groupCurrencies.filter(
    (currency) =>
      groupShares.some((share) => share.currency === currency) && !totals.some((total) => total.currency === currency),
  )
  return [
    ...totals,
    ...shareOnlyCurrencies.map((currency): PersonalCurrencyTotal => ({ currency, totalMinor: 0, categories: [] })),
  ].map((total) =>
    withShares(
      total,
      groupShares.filter((share) => share.currency === total.currency),
      categories,
    ),
  )
}

export function newestGroupSharesFirst(groupShares: readonly GroupShare[]): GroupShare[] {
  return [...groupShares].sort((first, second) => {
    if (first.entryDate !== second.entryDate) {
      return first.entryDate < second.entryDate ? 1 : -1
    }
    if (first.expenseId === second.expenseId) {
      return 0
    }
    return first.expenseId < second.expenseId ? -1 : 1
  })
}

function withShares(
  total: PersonalCurrencyTotal,
  shares: readonly GroupShare[],
  categories: readonly PersonalCategory[],
): PersonalCurrencyTotal {
  let rows = total.categories
  for (const share of shares) {
    const category = groupShareCategory(share, categories)
    if (category !== null) {
      rows = withShareInCategory(rows, category, share.shareMinor)
    }
  }
  return {
    currency: total.currency,
    totalMinor: shares.reduce((sum, share) => sum + share.shareMinor, total.totalMinor),
    categories: [...rows].sort((first, second) => compareCategoryOrder(first.category, second.category)),
  }
}

function withShareInCategory(
  rows: readonly PersonalCategoryTotal[],
  category: PersonalCategory,
  shareMinor: number,
): PersonalCategoryTotal[] {
  if (!rows.some((row) => row.category.id === category.id)) {
    return [...rows, { category, totalMinor: shareMinor, count: 1 }]
  }
  return rows.map((row) =>
    row.category.id === category.id
      ? { ...row, totalMinor: row.totalMinor + shareMinor, count: row.count + 1 }
      : row,
  )
}

function compareCategoryOrder(first: PersonalCategory, second: PersonalCategory): number {
  if (first.isCustom !== second.isCustom) {
    return first.isCustom ? 1 : -1
  }
  if (first.sortOrder !== second.sortOrder) {
    return first.sortOrder - second.sortOrder
  }
  if (first.id === second.id) {
    return 0
  }
  return first.id < second.id ? -1 : 1
}
