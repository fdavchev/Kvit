import type {
  PersonalEntry,
  PersonalEntryMonth,
} from '@/core/services/personalEntries/personalEntriesService'
import {
  archivedGymCategory,
  coffeeCategory,
  personalFoodCategory,
  personalGroceriesCategory,
} from './personalCategoryTestData'

export const coffeeEntry: PersonalEntry = {
  id: 'f0000000-0000-4000-8000-000000000001',
  category: coffeeCategory,
  amountMinor: 12000,
  currency: 'MKD',
  entryDate: '2026-10-19',
  note: null,
  mkdPerEur: 61.5,
  rateDate: '2026-10-17',
  createdAt: '2026-10-19T08:00:00Z',
  updatedAt: null,
}

export const lunchInEuroEntry: PersonalEntry = {
  id: 'f0000000-0000-4000-8000-000000000002',
  category: personalFoodCategory,
  amountMinor: 1250,
  currency: 'EUR',
  entryDate: '2026-10-18',
  note: 'Lunch in Ohrid',
  mkdPerEur: 61.5,
  rateDate: '2026-10-17',
  createdAt: '2026-10-18T12:00:00Z',
  updatedAt: '2026-10-18T12:30:00Z',
}

export const gymEntryWithArchivedCategory: PersonalEntry = {
  id: 'f0000000-0000-4000-8000-000000000003',
  category: archivedGymCategory,
  amountMinor: 150000,
  currency: 'MKD',
  entryDate: '2026-09-02',
  note: null,
  mkdPerEur: 61.5,
  rateDate: '2026-09-01',
  createdAt: '2026-09-02T18:00:00Z',
  updatedAt: null,
}

export function entryAnswerRow(entry: PersonalEntry): Record<string, unknown> {
  return { ...entry, categoryId: entry.category.id }
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
}

export function monthAnswerBody(month: PersonalEntryMonth): Record<string, unknown> {
  return {
    month: month.month,
    entries: month.entries.map(entryAnswerRow),
    totals: month.totals.map((total) => ({
      ...total,
      categories: total.categories.map((row) => ({ ...row, categoryId: row.category.id })),
    })),
  }
}

export const groceriesEntry: PersonalEntry = {
  id: 'f0000000-0000-4000-8000-000000000004',
  category: personalGroceriesCategory,
  amountMinor: 123400,
  currency: 'MKD',
  entryDate: '2026-10-12',
  note: 'Weekly shop',
  mkdPerEur: 61.5,
  rateDate: '2026-10-10',
  createdAt: '2026-10-12T17:00:00Z',
  updatedAt: null,
}

export const secondCoffeeEntry: PersonalEntry = {
  id: 'f0000000-0000-4000-8000-000000000005',
  category: coffeeCategory,
  amountMinor: 15000,
  currency: 'MKD',
  entryDate: '2026-10-18',
  note: null,
  mkdPerEur: 61.5,
  rateDate: '2026-10-17',
  createdAt: '2026-10-18T09:00:00Z',
  updatedAt: null,
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
}

export const euroDinnerEntry: PersonalEntry = {
  id: 'f0000000-0000-4000-8000-000000000006',
  category: personalFoodCategory,
  amountMinor: 4250,
  currency: 'EUR',
  entryDate: '2026-08-14',
  note: 'Dinner in Thessaloniki',
  mkdPerEur: 61.5,
  rateDate: '2026-08-14',
  createdAt: '2026-08-14T19:00:00Z',
  updatedAt: null,
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
}

export function emptyMonth(month: string): PersonalEntryMonth {
  return { month, entries: [], totals: [] }
}
