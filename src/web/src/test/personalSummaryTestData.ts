import type {
  PersonalSummary,
  SummaryCategory,
  SummaryConvertedTotals,
  SummaryCurrency,
} from '@/core/services/personalSummary/personalSummaryService'
import {
  archivedGymCategory,
  coffeeCategory,
  personalFoodCategory,
  personalGroceriesCategory,
  petCategory,
} from './personalCategoryTestData'

export const foodSummaryRow: SummaryCategory = {
  categoryId: personalFoodCategory.id,
  thisMinor: 720000,
  previousMinor: 758000,
}

export const groceriesSummaryRow: SummaryCategory = {
  categoryId: personalGroceriesCategory.id,
  thisMinor: 650000,
  previousMinor: 650000,
}

export const coffeeSummaryRow: SummaryCategory = {
  categoryId: coffeeCategory.id,
  thisMinor: 900000,
  previousMinor: 0,
}

export const dogSummaryRow: SummaryCategory = {
  categoryId: petCategory.id,
  thisMinor: 0,
  previousMinor: 239000,
}

export const gymSummaryRow: SummaryCategory = {
  categoryId: archivedGymCategory.id,
  thisMinor: 650000,
  previousMinor: 100000,
}

export const mkdSummary: SummaryCurrency = {
  currency: 'MKD',
  spendingThisMinor: 2920000,
  spendingPreviousMinor: 1747000,
  incomeThisMinor: 4000000,
  incomePreviousMinor: 3800000,
  categories: [foodSummaryRow, groceriesSummaryRow, coffeeSummaryRow, dogSummaryRow, gymSummaryRow],
}

export const eurSummary: SummaryCurrency = {
  currency: 'EUR',
  spendingThisMinor: 12500,
  spendingPreviousMinor: 10000,
  incomeThisMinor: 0,
  incomePreviousMinor: 20000,
  categories: [{ categoryId: personalFoodCategory.id, thisMinor: 12500, previousMinor: 10000 }],
}

export const convertedToMkdSummary: SummaryConvertedTotals = {
  currency: 'MKD',
  spendingThisMinor: 3688800,
  spendingPreviousMinor: 2362000,
  incomeThisMinor: 4000000,
  incomePreviousMinor: 5030000,
}

export const convertedToEurSummary: SummaryConvertedTotals = {
  currency: 'EUR',
  spendingThisMinor: 59800,
  spendingPreviousMinor: 38400,
  incomeThisMinor: 65000,
  incomePreviousMinor: 80000,
}

export function summaryOf(
  month: string,
  previousMonth: string,
  currencies: SummaryCurrency[],
  convertedTotals: SummaryConvertedTotals | null = null,
): PersonalSummary {
  return { month, previousMonth, currencies, convertedTotals }
}

export const octoberSummary: PersonalSummary = summaryOf('2026-10', '2026-09', [mkdSummary, eurSummary])

export const octoberSummaryConvertedToMkd: PersonalSummary = summaryOf(
  '2026-10',
  '2026-09',
  [mkdSummary, eurSummary],
  convertedToMkdSummary,
)

export const octoberSummaryConvertedToEur: PersonalSummary = summaryOf(
  '2026-10',
  '2026-09',
  [mkdSummary, eurSummary],
  convertedToEurSummary,
)

export const octoberSummaryOfDenarsOnly: PersonalSummary = summaryOf('2026-10', '2026-09', [mkdSummary])

export const emptyOctoberSummary: PersonalSummary = summaryOf('2026-10', '2026-09', [])

export const emptyOctoberSummaryWithZeroConvertedTotals: PersonalSummary = summaryOf('2026-10', '2026-09', [], {
  currency: 'MKD',
  spendingThisMinor: 0,
  spendingPreviousMinor: 0,
  incomeThisMinor: 0,
  incomePreviousMinor: 0,
})

export const januarySummary: PersonalSummary = summaryOf('2026-01', '2025-12', [
  {
    currency: 'MKD',
    spendingThisMinor: 300000,
    spendingPreviousMinor: 200000,
    incomeThisMinor: 0,
    incomePreviousMinor: 0,
    categories: [{ categoryId: personalFoodCategory.id, thisMinor: 300000, previousMinor: 200000 }],
  },
])

export const novemberSummary: PersonalSummary = summaryOf('2026-11', '2026-10', [
  {
    currency: 'MKD',
    spendingThisMinor: 100000,
    spendingPreviousMinor: 100000,
    incomeThisMinor: 0,
    incomePreviousMinor: 0,
    categories: [],
  },
])

export const spendingOnlyThisMonthSummary: PersonalSummary = summaryOf('2026-10', '2026-09', [
  {
    currency: 'MKD',
    spendingThisMinor: 300000,
    spendingPreviousMinor: 0,
    incomeThisMinor: 0,
    incomePreviousMinor: 0,
    categories: [{ categoryId: personalFoodCategory.id, thisMinor: 300000, previousMinor: 0 }],
  },
])

export const spendingOnlyLastMonthSummary: PersonalSummary = summaryOf('2026-10', '2026-09', [
  {
    currency: 'MKD',
    spendingThisMinor: 0,
    spendingPreviousMinor: 500000,
    incomeThisMinor: 0,
    incomePreviousMinor: 0,
    categories: [{ categoryId: personalFoodCategory.id, thisMinor: 0, previousMinor: 500000 }],
  },
])

export const unchangedSummary: PersonalSummary = summaryOf('2026-10', '2026-09', [
  {
    currency: 'MKD',
    spendingThisMinor: 650000,
    spendingPreviousMinor: 650000,
    incomeThisMinor: 4000000,
    incomePreviousMinor: 4000000,
    categories: [groceriesSummaryRow],
  },
])

export const incomeOnlySummary: PersonalSummary = summaryOf('2026-10', '2026-09', [
  {
    currency: 'MKD',
    spendingThisMinor: 0,
    spendingPreviousMinor: 0,
    incomeThisMinor: 4000000,
    incomePreviousMinor: 3800000,
    categories: [],
  },
])

export const barelyMoreSummary: PersonalSummary = summaryOf('2026-10', '2026-09', [
  {
    currency: 'MKD',
    spendingThisMinor: 2000100,
    spendingPreviousMinor: 2000000,
    incomeThisMinor: 0,
    incomePreviousMinor: 0,
    categories: [],
  },
])

export const halfPercentSummary: PersonalSummary = summaryOf('2026-10', '2026-09', [
  {
    currency: 'MKD',
    spendingThisMinor: 900000,
    spendingPreviousMinor: 800000,
    incomeThisMinor: 700000,
    incomePreviousMinor: 800000,
    categories: [],
  },
])

export const longNameSummary: PersonalSummary = summaryOf('2026-10', '2026-09', [
  {
    currency: 'MKD',
    spendingThisMinor: 99999999900,
    spendingPreviousMinor: 12345678900,
    incomeThisMinor: 0,
    incomePreviousMinor: 0,
    categories: [{ categoryId: coffeeCategory.id, thisMinor: 99999999900, previousMinor: 12345678900 }],
  },
])
