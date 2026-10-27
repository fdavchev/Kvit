import type { LimitRow, PersonalEntryMonth } from '@/core/services/personalEntries/personalEntriesService'
import {
  archivedGymCategory,
  coffeeCategory,
  personalFoodCategory,
  personalGroceriesCategory,
  petCategory,
} from './personalCategoryTestData'
import { emptyMonth, octoberMonth, septemberWithArchivedCategory } from './personalEntryTestData'

export function limitRow(categoryId: string | null, limitMinor: number, spentMinor: number, includedEurMinor = 0): LimitRow {
  return { categoryId, limitMinor, spentMinor, overMinor: Math.max(0, spentMinor - limitMinor), includedEurMinor }
}

export const wholeMonthLimit = limitRow(null, 2500000, 1750000)
export const foodLimit = limitRow(personalFoodCategory.id, 1000000, 720000)
export const groceriesLimitWithEuros = limitRow(personalGroceriesCategory.id, 1000000, 900000, 2000)
export const coffeeLimitOver = limitRow(coffeeCategory.id, 450000, 480000)
export const petLimitExactlyReached = limitRow(petCategory.id, 500000, 500000)
export const gymLimitOfArchivedCategory = limitRow(archivedGymCategory.id, 300000, 0)

export const octoberWithLimits: PersonalEntryMonth = {
  ...octoberMonth,
  limits: {
    month: wholeMonthLimit,
    categories: [foodLimit, groceriesLimitWithEuros, coffeeLimitOver, petLimitExactlyReached],
  },
}

export const octoberWithLimitsInAnotherOrder: PersonalEntryMonth = {
  ...octoberMonth,
  limits: {
    month: wholeMonthLimit,
    categories: [petLimitExactlyReached, coffeeLimitOver, groceriesLimitWithEuros, foodLimit],
  },
}

export const octoberWithOnlyTheWholeMonthLimit: PersonalEntryMonth = {
  ...octoberMonth,
  limits: { month: wholeMonthLimit, categories: [] },
}

export const octoberWithOnlyACoffeeLimit: PersonalEntryMonth = {
  ...octoberMonth,
  limits: { month: null, categories: [coffeeLimitOver] },
}

export const octoberWithoutWholeMonthLimit: PersonalEntryMonth = {
  ...octoberMonth,
  limits: { month: null, categories: [foodLimit, coffeeLimitOver] },
}

export const emptyOctoberWithALimit: PersonalEntryMonth = {
  ...emptyMonth('2026-10'),
  limits: { month: limitRow(null, 2500000, 0), categories: [] },
}

export const septemberWithLimits: PersonalEntryMonth = {
  ...septemberWithArchivedCategory,
  limits: {
    month: limitRow(null, 2000000, 150000),
    categories: [gymLimitOfArchivedCategory],
  },
}

export function octoberWithMonthLimitSpent(spentMinor: number): PersonalEntryMonth {
  return { ...octoberMonth, limits: { month: limitRow(null, 1000000, spentMinor), categories: [] } }
}
