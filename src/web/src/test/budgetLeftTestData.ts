import type { PersonalEntry, PersonalEntryMonth } from '@/core/services/personalEntries/personalEntriesService'
import { coffeeCategory, personalFoodCategory } from './personalCategoryTestData'
import { coffeeEntry, octoberMonth, salaryIncomeEntry, untitledEuroIncomeEntry } from './personalEntryTestData'

export const largestSafeMinor = Number.MAX_SAFE_INTEGER

function entryOf(base: PersonalEntry, id: string, amountMinor: number): PersonalEntry {
  return { ...base, id, amountMinor }
}

const bigSalaryEntry = entryOf(salaryIncomeEntry, 'f1000000-0000-4000-8000-000000000001', 4500000)
const rentEntry = entryOf(coffeeEntry, 'f1000000-0000-4000-8000-000000000002', 1240000)
const holidayEuroEntry: PersonalEntry = {
  ...entryOf(coffeeEntry, 'f1000000-0000-4000-8000-000000000003', 5000),
  category: personalFoodCategory,
  currency: 'EUR',
}

export const octoberIncomeAndTwoSpendings: PersonalEntryMonth = {
  month: '2026-10',
  entries: [rentEntry, holidayEuroEntry, bigSalaryEntry],
  totals: [
    { currency: 'MKD', totalMinor: 1240000, categories: [{ category: coffeeCategory, totalMinor: 1240000, count: 1 }] },
    { currency: 'EUR', totalMinor: 5000, categories: [{ category: personalFoodCategory, totalMinor: 5000, count: 1 }] },
  ],
  incomeTotals: [{ currency: 'MKD', totalMinor: 4500000, count: 1 }],
}

const smallSalaryEntry = entryOf(salaryIncomeEntry, 'f1000000-0000-4000-8000-000000000004', 1000000)

export const octoberOverspent: PersonalEntryMonth = {
  month: '2026-10',
  entries: [rentEntry, smallSalaryEntry],
  totals: [
    { currency: 'MKD', totalMinor: 1240000, categories: [{ category: coffeeCategory, totalMinor: 1240000, count: 1 }] },
  ],
  incomeTotals: [{ currency: 'MKD', totalMinor: 1000000, count: 1 }],
}

export const octoberEuroIncomeAndDenarSpending: PersonalEntryMonth = {
  month: '2026-10',
  entries: [coffeeEntry, untitledEuroIncomeEntry],
  totals: [octoberMonth.totals[0]],
  incomeTotals: [{ currency: 'EUR', totalMinor: 20000, count: 1 }],
}

export const octoberIncomeInBothCurrenciesSpendingInBoth: PersonalEntryMonth = {
  month: '2026-10',
  entries: [coffeeEntry, untitledEuroIncomeEntry, salaryIncomeEntry, holidayEuroEntry],
  totals: [
    octoberMonth.totals[0],
    { currency: 'EUR', totalMinor: 5000, categories: [{ category: personalFoodCategory, totalMinor: 5000, count: 1 }] },
  ],
  incomeTotals: [
    { currency: 'MKD', totalMinor: 6500000, count: 1 },
    { currency: 'EUR', totalMinor: 20000, count: 1 },
  ],
}

const septemberSalaryEntry: PersonalEntry = {
  ...entryOf(salaryIncomeEntry, 'f1000000-0000-4000-8000-000000000005', 3000000),
  entryDate: '2026-09-15',
}

const septemberCoffeeEntry: PersonalEntry = {
  ...entryOf(coffeeEntry, 'f1000000-0000-4000-8000-000000000006', 150000),
  entryDate: '2026-09-02',
}

export const septemberWithIncome: PersonalEntryMonth = {
  month: '2026-09',
  entries: [septemberCoffeeEntry, septemberSalaryEntry],
  totals: [
    { currency: 'MKD', totalMinor: 150000, categories: [{ category: coffeeCategory, totalMinor: 150000, count: 1 }] },
  ],
  incomeTotals: [{ currency: 'MKD', totalMinor: 3000000, count: 1 }],
}

const hugeEuroIncomeEntry: PersonalEntry = {
  ...entryOf(untitledEuroIncomeEntry, 'f1000000-0000-4000-8000-000000000007', largestSafeMinor),
}

const oneCentEuroSpendingEntry: PersonalEntry = {
  ...entryOf(holidayEuroEntry, 'f1000000-0000-4000-8000-000000000008', 1),
}

export const octoberWithLargestSafeIncome: PersonalEntryMonth = {
  month: '2026-10',
  entries: [oneCentEuroSpendingEntry, hugeEuroIncomeEntry],
  totals: [
    { currency: 'EUR', totalMinor: 1, categories: [{ category: personalFoodCategory, totalMinor: 1, count: 1 }] },
  ],
  incomeTotals: [{ currency: 'EUR', totalMinor: largestSafeMinor, count: 1 }],
}

export const octoberWithLargestSafeIncomeAndSpending: PersonalEntryMonth = {
  month: '2026-10',
  entries: [hugeEuroIncomeEntry],
  totals: [
    {
      currency: 'EUR',
      totalMinor: largestSafeMinor,
      categories: [{ category: personalFoodCategory, totalMinor: largestSafeMinor, count: 1 }],
    },
  ],
  incomeTotals: [{ currency: 'EUR', totalMinor: largestSafeMinor, count: 1 }],
}

const longDenarIncomeEntry = entryOf(salaryIncomeEntry, 'f1000000-0000-4000-8000-000000000009', 999999999900)
const longDenarSpendingEntry = entryOf(rentEntry, 'f1000000-0000-4000-8000-000000000010', 124056700)
const longEuroIncomeEntry = entryOf(untitledEuroIncomeEntry, 'f1000000-0000-4000-8000-000000000011', 99999999999)
const longEuroSpendingEntry = entryOf(holidayEuroEntry, 'f1000000-0000-4000-8000-000000000012', 99999999999)

export const octoberWithLongAmounts: PersonalEntryMonth = {
  month: '2026-10',
  entries: [longDenarSpendingEntry, longEuroSpendingEntry, longDenarIncomeEntry, longEuroIncomeEntry],
  totals: [
    { currency: 'MKD', totalMinor: 124056700, categories: [{ category: coffeeCategory, totalMinor: 124056700, count: 1 }] },
    { currency: 'EUR', totalMinor: 99999999999, categories: [{ category: personalFoodCategory, totalMinor: 99999999999, count: 1 }] },
  ],
  incomeTotals: [
    { currency: 'MKD', totalMinor: 999999999900, count: 1 },
    { currency: 'EUR', totalMinor: 99999999999, count: 1 },
  ],
}
