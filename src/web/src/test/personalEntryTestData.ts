import type {
  MonthLimits,
  PersonalEntry,
  PersonalEntryMonth,
} from '@/core/services/personalEntries/personalEntriesService'
import {
  archivedGymCategory,
  coffeeCategory,
  personalFoodCategory,
  personalGroceriesCategory,
} from './personalCategoryTestData'

export const noLimits: MonthLimits = { month: null, categories: [] }

export const noGroupSharesOrLimits: Pick<
  PersonalEntryMonth,
  'groupShares' | 'convertedTotals' | 'limits' | 'possibleDuplicates'
> = {
  groupShares: [],
  convertedTotals: null,
  limits: noLimits,
  possibleDuplicates: [],
}

export const coffeeEntry: PersonalEntry = {
  id: 'f0000000-0000-4000-8000-000000000001',
  kind: 'Spending',
  category: coffeeCategory,
  amountMinor: 12000,
  currency: 'MKD',
  entryDate: '2026-10-19',
  note: null,
  mkdPerEur: 61.5,
  rateDate: '2026-10-17',
  createdAt: '2026-10-19T08:00:00Z',
  updatedAt: null,
  recurringIncomeId: null,
  mergedWithExpenseId: null,
}

export const lunchInEuroEntry: PersonalEntry = {
  id: 'f0000000-0000-4000-8000-000000000002',
  kind: 'Spending',
  category: personalFoodCategory,
  amountMinor: 1250,
  currency: 'EUR',
  entryDate: '2026-10-18',
  note: 'Lunch in Ohrid',
  mkdPerEur: 61.5,
  rateDate: '2026-10-17',
  createdAt: '2026-10-18T12:00:00Z',
  updatedAt: '2026-10-18T12:30:00Z',
  recurringIncomeId: null,
  mergedWithExpenseId: null,
}

export const gymEntryWithArchivedCategory: PersonalEntry = {
  id: 'f0000000-0000-4000-8000-000000000003',
  kind: 'Spending',
  category: archivedGymCategory,
  amountMinor: 150000,
  currency: 'MKD',
  entryDate: '2026-09-02',
  note: null,
  mkdPerEur: 61.5,
  rateDate: '2026-09-01',
  createdAt: '2026-09-02T18:00:00Z',
  updatedAt: null,
  recurringIncomeId: null,
  mergedWithExpenseId: null,
}

export function entryAnswerRow(entry: PersonalEntry): Record<string, unknown> {
  return { ...entry, categoryId: entry.category === null ? null : entry.category.id }
}

export const salaryIncomeEntry: PersonalEntry = {
  id: 'f0000000-0000-4000-8000-000000000007',
  kind: 'Income',
  category: null,
  amountMinor: 6500000,
  currency: 'MKD',
  entryDate: '2026-10-15',
  note: 'Salary',
  mkdPerEur: 61.5,
  rateDate: '2026-10-14',
  createdAt: '2026-10-15T07:00:00Z',
  updatedAt: null,
  recurringIncomeId: null,
  mergedWithExpenseId: null,
}

export const untitledEuroIncomeEntry: PersonalEntry = {
  id: 'f0000000-0000-4000-8000-000000000008',
  kind: 'Income',
  category: null,
  amountMinor: 20000,
  currency: 'EUR',
  entryDate: '2026-10-19',
  note: null,
  mkdPerEur: 61.5,
  rateDate: '2026-10-17',
  createdAt: '2026-10-19T09:00:00Z',
  updatedAt: null,
  recurringIncomeId: null,
  mergedWithExpenseId: null,
}

export const generatedSalaryEntry: PersonalEntry = {
  ...salaryIncomeEntry,
  id: 'f0000000-0000-4000-8000-000000000009',
  entryDate: '2026-10-01',
  createdAt: '2026-10-01T07:00:00Z',
  recurringIncomeId: 'a0000000-0000-4000-8000-000000000001',
}

export const octoberMonth: PersonalEntryMonth = {
  month: '2026-10',
  entries: [coffeeEntry, lunchInEuroEntry],
  totals: [
    {
      currency: 'MKD',
      totalMinor: 12000,
      categories: [{ category: coffeeCategory, totalMinor: 12000, count: 1 }],
    },
    {
      currency: 'EUR',
      totalMinor: 1250,
      categories: [{ category: personalFoodCategory, totalMinor: 1250, count: 1 }],
    },
  ],
  incomeTotals: [],
  ...noGroupSharesOrLimits,
}

export function monthAnswerBody(month: PersonalEntryMonth): Record<string, unknown> {
  return {
    month: month.month,
    entries: month.entries.map(entryAnswerRow),
    totals: month.totals.map((total) => ({
      ...total,
      categories: total.categories.map((row) => ({ ...row, categoryId: row.category.id })),
    })),
    incomeTotals: month.incomeTotals,
    groupShares: month.groupShares,
    convertedTotals: month.convertedTotals,
    limits: month.limits,
    possibleDuplicates: month.possibleDuplicates,
  }
}

export const groceriesEntry: PersonalEntry = {
  id: 'f0000000-0000-4000-8000-000000000004',
  kind: 'Spending',
  category: personalGroceriesCategory,
  amountMinor: 123400,
  currency: 'MKD',
  entryDate: '2026-10-12',
  note: 'Weekly shop',
  mkdPerEur: 61.5,
  rateDate: '2026-10-10',
  createdAt: '2026-10-12T17:00:00Z',
  updatedAt: null,
  recurringIncomeId: null,
  mergedWithExpenseId: null,
}

export const secondCoffeeEntry: PersonalEntry = {
  id: 'f0000000-0000-4000-8000-000000000005',
  kind: 'Spending',
  category: coffeeCategory,
  amountMinor: 15000,
  currency: 'MKD',
  entryDate: '2026-10-18',
  note: null,
  mkdPerEur: 61.5,
  rateDate: '2026-10-17',
  createdAt: '2026-10-18T09:00:00Z',
  updatedAt: null,
  recurringIncomeId: null,
  mergedWithExpenseId: null,
}

export const mkdOnlyMonth: PersonalEntryMonth = {
  month: '2026-10',
  entries: [coffeeEntry, secondCoffeeEntry, groceriesEntry],
  totals: [
    {
      currency: 'MKD',
      totalMinor: 150400,
      categories: [
        { category: personalGroceriesCategory, totalMinor: 123400, count: 1 },
        { category: coffeeCategory, totalMinor: 27000, count: 2 },
      ],
    },
  ],
  incomeTotals: [],
  ...noGroupSharesOrLimits,
}

export const euroDinnerEntry: PersonalEntry = {
  id: 'f0000000-0000-4000-8000-000000000006',
  kind: 'Spending',
  category: personalFoodCategory,
  amountMinor: 4250,
  currency: 'EUR',
  entryDate: '2026-08-14',
  note: 'Dinner in Thessaloniki',
  mkdPerEur: 61.5,
  rateDate: '2026-08-14',
  createdAt: '2026-08-14T19:00:00Z',
  updatedAt: null,
  recurringIncomeId: null,
  mergedWithExpenseId: null,
}

export const euroOnlyAugust: PersonalEntryMonth = {
  month: '2026-08',
  entries: [euroDinnerEntry],
  totals: [
    {
      currency: 'EUR',
      totalMinor: 4250,
      categories: [{ category: personalFoodCategory, totalMinor: 4250, count: 1 }],
    },
  ],
  incomeTotals: [],
  ...noGroupSharesOrLimits,
}

export const septemberWithArchivedCategory: PersonalEntryMonth = {
  month: '2026-09',
  entries: [gymEntryWithArchivedCategory],
  totals: [
    {
      currency: 'MKD',
      totalMinor: 150000,
      categories: [{ category: archivedGymCategory, totalMinor: 150000, count: 1 }],
    },
  ],
  incomeTotals: [],
  ...noGroupSharesOrLimits,
}

export function emptyMonth(month: string): PersonalEntryMonth {
  return { month, entries: [], totals: [], incomeTotals: [], ...noGroupSharesOrLimits }
}

export const octoberWithSalary: PersonalEntryMonth = {
  month: '2026-10',
  entries: [coffeeEntry, lunchInEuroEntry, salaryIncomeEntry],
  totals: octoberMonth.totals,
  incomeTotals: [{ currency: 'MKD', totalMinor: 6500000, count: 1 }],
  ...noGroupSharesOrLimits,
}

export const octoberWithTwoIncomes: PersonalEntryMonth = {
  month: '2026-10',
  entries: [untitledEuroIncomeEntry, coffeeEntry, salaryIncomeEntry],
  totals: [octoberMonth.totals[0]],
  incomeTotals: [
    { currency: 'MKD', totalMinor: 6500000, count: 1 },
    { currency: 'EUR', totalMinor: 20000, count: 1 },
  ],
  ...noGroupSharesOrLimits,
}

export const incomeOnlyOctober: PersonalEntryMonth = {
  month: '2026-10',
  entries: [salaryIncomeEntry],
  totals: [],
  incomeTotals: [{ currency: 'MKD', totalMinor: 6500000, count: 1 }],
  ...noGroupSharesOrLimits,
}

export const euroIncomeOnlyOctober: PersonalEntryMonth = {
  month: '2026-10',
  entries: [untitledEuroIncomeEntry],
  totals: [],
  incomeTotals: [{ currency: 'EUR', totalMinor: 20000, count: 1 }],
  ...noGroupSharesOrLimits,
}
