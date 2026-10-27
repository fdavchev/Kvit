import type {
  GroupShare,
  PersonalCurrencyTotal,
  PersonalEntry,
  PersonalEntryMonth,
} from '@/core/services/personalEntries/personalEntriesService'
import type { Currency } from '@/shared/utils/formatMoney'
import { dinnerShare, euroTaxiShare, untitledGroceriesShare } from './groupShareTestData'
import { coffeeCategory, personalFoodCategory, personalGroceriesCategory } from './personalCategoryTestData'
import { coffeeEntry, groceriesEntry, noLimits } from './personalEntryTestData'

export type DuplicateMatch = 'Share' | 'Whole'

export interface DuplicatePairFixture {
  entryId: string
  entryNote: string | null
  entryAmountMinor: number
  entryDate: string
  entryCategoryId: string
  expenseId: string
  groupId: string
  groupName: string
  expenseTitle: string | null
  currency: Currency
  shareMinor: number
  expenseAmountMinor: number
  expenseDate: string
  matchedOn: DuplicateMatch
}

export const duplicateAnswersUrl = '/api/personal/duplicate-answers'

export const dinnerOutEntry: PersonalEntry = {
  id: 'f2000000-0000-4000-8000-000000000001',
  kind: 'Spending',
  category: personalFoodCategory,
  amountMinor: 100000,
  currency: 'MKD',
  entryDate: '2026-10-17',
  note: 'Dinner out',
  mkdPerEur: 61.5,
  rateDate: '2026-10-17',
  createdAt: '2026-10-17T20:00:00Z',
  updatedAt: null,
  recurringIncomeId: null,
  mergedWithExpenseId: null,
}

export const taxiRideEntry: PersonalEntry = {
  id: 'f2000000-0000-4000-8000-000000000002',
  kind: 'Spending',
  category: personalFoodCategory,
  amountMinor: 850,
  currency: 'EUR',
  entryDate: '2026-10-18',
  note: 'Taxi ride',
  mkdPerEur: 61.5,
  rateDate: '2026-10-17',
  createdAt: '2026-10-18T22:00:00Z',
  updatedAt: null,
  recurringIncomeId: null,
  mergedWithExpenseId: null,
}

export const untitledGroceriesEntry: PersonalEntry = {
  ...groceriesEntry,
  id: 'f2000000-0000-4000-8000-000000000003',
  entryDate: '2026-10-17',
  note: null,
}

export const mergedDinnerOutEntry: PersonalEntry = {
  ...dinnerOutEntry,
  mergedWithExpenseId: dinnerShare.expenseId,
}

export const mergedTaxiRideEntry: PersonalEntry = {
  ...taxiRideEntry,
  mergedWithExpenseId: euroTaxiShare.expenseId,
}

export const mergedUntitledGroceriesEntry: PersonalEntry = {
  ...untitledGroceriesEntry,
  mergedWithExpenseId: untitledGroceriesShare.expenseId,
}

export function duplicatePair(
  entry: PersonalEntry,
  share: GroupShare,
  matchedOn: DuplicateMatch = 'Share',
  expenseAmountMinor: number = share.shareMinor * 2,
): DuplicatePairFixture {
  if (entry.category === null) {
    throw new Error(`The entry ${entry.id} has no category, so it cannot be one of the possible duplicates`)
  }
  return {
    entryId: entry.id,
    entryNote: entry.note,
    entryAmountMinor: entry.amountMinor,
    entryDate: entry.entryDate,
    entryCategoryId: entry.category.id,
    expenseId: share.expenseId,
    groupId: share.groupId,
    groupName: share.groupName,
    expenseTitle: share.title,
    currency: share.currency,
    shareMinor: share.shareMinor,
    expenseAmountMinor,
    expenseDate: share.entryDate,
    matchedOn,
  }
}

export const dinnerPair = duplicatePair(dinnerOutEntry, dinnerShare)
export const taxiPair = duplicatePair(taxiRideEntry, euroTaxiShare)
export const groceriesPair = duplicatePair(untitledGroceriesEntry, untitledGroceriesShare, 'Whole', 60000)

const foodInMkd: PersonalCurrencyTotal = {
  currency: 'MKD',
  totalMinor: 100000,
  categories: [{ category: personalFoodCategory, totalMinor: 100000, count: 1 }],
}

const foodInEur: PersonalCurrencyTotal = {
  currency: 'EUR',
  totalMinor: 850,
  categories: [{ category: personalFoodCategory, totalMinor: 850, count: 1 }],
}

const groceriesInMkd: PersonalCurrencyTotal = {
  currency: 'MKD',
  totalMinor: 123400,
  categories: [{ category: personalGroceriesCategory, totalMinor: 123400, count: 1 }],
}

const withoutConversions: Pick<PersonalEntryMonth, 'incomeTotals' | 'convertedTotals' | 'limits'> = {
  incomeTotals: [],
  convertedTotals: null,
  limits: noLimits,
}

export const octoberWithTwoPairs: PersonalEntryMonth = {
  month: '2026-10',
  entries: [taxiRideEntry, dinnerOutEntry],
  totals: [foodInMkd, foodInEur],
  groupShares: [dinnerShare, euroTaxiShare],
  possibleDuplicates: [dinnerPair, taxiPair],
  ...withoutConversions,
}

export const octoberWithTwoPairsAfterDinnerMerged: PersonalEntryMonth = {
  ...octoberWithTwoPairs,
  entries: [taxiRideEntry, mergedDinnerOutEntry],
  totals: [foodInEur],
  possibleDuplicates: [taxiPair],
}

export const octoberWithTwoPairsAfterDinnerIsNew: PersonalEntryMonth = {
  ...octoberWithTwoPairs,
  possibleDuplicates: [taxiPair],
}

export const octoberWithOnlyTheDinnerPair: PersonalEntryMonth = {
  month: '2026-10',
  entries: [dinnerOutEntry],
  totals: [foodInMkd],
  groupShares: [dinnerShare],
  possibleDuplicates: [dinnerPair],
  ...withoutConversions,
}

export const octoberWithOnlyTheDinnerMerged: PersonalEntryMonth = {
  ...octoberWithOnlyTheDinnerPair,
  entries: [mergedDinnerOutEntry],
  totals: [],
  possibleDuplicates: [],
}

export const octoberWithOnlyTheDinnerAnsweredNew: PersonalEntryMonth = {
  ...octoberWithOnlyTheDinnerPair,
  possibleDuplicates: [],
}

export const octoberWithOnlyTheTaxiPair: PersonalEntryMonth = {
  month: '2026-10',
  entries: [taxiRideEntry],
  totals: [foodInEur],
  groupShares: [euroTaxiShare],
  possibleDuplicates: [taxiPair],
  ...withoutConversions,
}

export const octoberWithOnlyTheGroceriesPair: PersonalEntryMonth = {
  month: '2026-10',
  entries: [untitledGroceriesEntry],
  totals: [groceriesInMkd],
  groupShares: [untitledGroceriesShare],
  possibleDuplicates: [groceriesPair],
  ...withoutConversions,
}

export const octoberWithMergedDinnerAndCoffee: PersonalEntryMonth = {
  month: '2026-10',
  entries: [coffeeEntry, mergedDinnerOutEntry],
  totals: [
    {
      currency: 'MKD',
      totalMinor: 12000,
      categories: [{ category: coffeeCategory, totalMinor: 12000, count: 1 }],
    },
  ],
  groupShares: [dinnerShare],
  possibleDuplicates: [],
  ...withoutConversions,
}

export const octoberWithMergedUntitledGroceries: PersonalEntryMonth = {
  month: '2026-10',
  entries: [mergedUntitledGroceriesEntry, dinnerOutEntry],
  totals: [foodInMkd],
  groupShares: [untitledGroceriesShare],
  possibleDuplicates: [],
  ...withoutConversions,
}

export const missingShareExpenseId = 'e1000000-0000-4000-8000-0000000000aa'

export const octoberWithMergedEntryWhoseShareIsMissing: PersonalEntryMonth = {
  month: '2026-10',
  entries: [{ ...dinnerOutEntry, mergedWithExpenseId: missingShareExpenseId }],
  totals: [],
  groupShares: [],
  possibleDuplicates: [],
  ...withoutConversions,
}

export const octoberWithEntryOnTheFirstMergedWithShareOfSeptember: PersonalEntryMonth = {
  month: '2026-10',
  entries: [{ ...dinnerOutEntry, entryDate: '2026-10-01', mergedWithExpenseId: missingShareExpenseId }],
  totals: [],
  groupShares: [],
  possibleDuplicates: [],
  ...withoutConversions,
}

export const septemberWithEntryOnThe30thMergedWithShareOfOctober: PersonalEntryMonth = {
  month: '2026-09',
  entries: [{ ...dinnerOutEntry, entryDate: '2026-09-30', mergedWithExpenseId: missingShareExpenseId }],
  totals: [],
  groupShares: [],
  possibleDuplicates: [],
  ...withoutConversions,
}
