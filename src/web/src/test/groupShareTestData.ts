import type {
  ConvertedTotals,
  GroupShare,
  LimitRow,
  PersonalEntryMonth,
} from '@/core/services/personalEntries/personalEntriesService'
import { personalFoodCategory, personalGroceriesCategory } from './personalCategoryTestData'
import {
  coffeeEntry,
  lunchInEuroEntry,
  mkdOnlyMonth,
  noLimits,
  octoberMonth,
  octoberWithSalary,
  untitledEuroIncomeEntry,
} from './personalEntryTestData'

export const greeceTripGroupId = 'a1000000-0000-4000-8000-000000000001'
export const flatGroupId = 'a1000000-0000-4000-8000-000000000002'

export const hotelShare: GroupShare = {
  expenseId: 'e1000000-0000-4000-8000-000000000001',
  groupId: greeceTripGroupId,
  groupName: 'Greece trip',
  title: 'Hotel',
  categoryId: null,
  entryDate: '2026-10-14',
  currency: 'MKD',
  shareMinor: 480000,
}

export const dinnerShare: GroupShare = {
  expenseId: 'e1000000-0000-4000-8000-000000000002',
  groupId: flatGroupId,
  groupName: 'Flat',
  title: 'Dinner',
  categoryId: personalFoodCategory.id,
  entryDate: '2026-10-18',
  currency: 'MKD',
  shareMinor: 95000,
}

export const untitledGroceriesShare: GroupShare = {
  expenseId: 'e1000000-0000-4000-8000-000000000003',
  groupId: flatGroupId,
  groupName: 'Flat',
  title: null,
  categoryId: personalGroceriesCategory.id,
  entryDate: '2026-10-19',
  currency: 'MKD',
  shareMinor: 30000,
}

export const untitledPlainShare: GroupShare = {
  expenseId: 'e1000000-0000-4000-8000-000000000004',
  groupId: flatGroupId,
  groupName: 'Flat',
  title: null,
  categoryId: null,
  entryDate: '2026-10-10',
  currency: 'MKD',
  shareMinor: 12000,
}

export const euroTaxiShare: GroupShare = {
  expenseId: 'e1000000-0000-4000-8000-000000000005',
  groupId: greeceTripGroupId,
  groupName: 'Greece trip',
  title: 'Taxi',
  categoryId: personalFoodCategory.id,
  entryDate: '2026-10-18',
  currency: 'EUR',
  shareMinor: 800,
}

export const longGroupNameShare: GroupShare = {
  ...hotelShare,
  expenseId: 'e1000000-0000-4000-8000-000000000006',
  groupName: 'Thessaloniki-and-Halkidiki-summer-holiday-with-the-whole-family-2026',
}

export const longTitleWord = 'Supercalifragilisticexpialidociousandonwardstotheveryendxyzq'

export const longTitleShare: GroupShare = {
  ...dinnerShare,
  expenseId: 'e1000000-0000-4000-8000-000000000007',
  title: longTitleWord,
}

export const allGroupShares: GroupShare[] = [
  untitledPlainShare,
  hotelShare,
  dinnerShare,
  euroTaxiShare,
  untitledGroceriesShare,
]

export const octoberWithGroupShares: PersonalEntryMonth = {
  ...octoberMonth,
  groupShares: allGroupShares,
}

export const groupSharesOnlyOctober: PersonalEntryMonth = {
  month: '2026-10',
  entries: [],
  totals: [],
  incomeTotals: [],
  groupShares: [hotelShare, dinnerShare, euroTaxiShare],
  convertedTotals: null,
  limits: noLimits,
  possibleDuplicates: [],
}

export const convertedToMkd: ConvertedTotals = {
  currency: 'MKD',
  spendingMinor: 88900,
  incomeMinor: 0,
  groupSharesMinor: 666200,
  leftMinor: -755100,
}

export const convertedToEur: ConvertedTotals = {
  currency: 'EUR',
  spendingMinor: 1445,
  incomeMinor: 0,
  groupSharesMinor: 10833,
  leftMinor: -12278,
}

export const octoberWithGroupSharesConvertedToMkd: PersonalEntryMonth = {
  ...octoberWithGroupShares,
  convertedTotals: convertedToMkd,
}

export const octoberWithGroupSharesConvertedToEur: PersonalEntryMonth = {
  ...octoberWithGroupShares,
  convertedTotals: convertedToEur,
}

export const octoberWithSalaryAndGroupSharesConvertedToMkd: PersonalEntryMonth = {
  ...octoberWithSalary,
  groupShares: allGroupShares,
  convertedTotals: {
    currency: 'MKD',
    spendingMinor: 88900,
    incomeMinor: 6500000,
    groupSharesMinor: 666200,
    leftMinor: 5744900,
  },
}

export const octoberWithSalaryAndGroupSharesOverspentInMkd: PersonalEntryMonth = {
  ...octoberWithSalary,
  groupShares: allGroupShares,
  convertedTotals: {
    currency: 'MKD',
    spendingMinor: 88900,
    incomeMinor: 500000,
    groupSharesMinor: 666200,
    leftMinor: -255100,
  },
}

export const mkdOnlyWithSharesConvertedToMkd: PersonalEntryMonth = {
  ...mkdOnlyMonth,
  groupShares: [hotelShare, dinnerShare],
  convertedTotals: {
    currency: 'MKD',
    spendingMinor: 150400,
    incomeMinor: 0,
    groupSharesMinor: 575000,
    leftMinor: -725400,
  },
}

export const mkdOnlyWithSharesConvertedToEur: PersonalEntryMonth = {
  ...mkdOnlyMonth,
  groupShares: [hotelShare, dinnerShare],
  convertedTotals: {
    currency: 'EUR',
    spendingMinor: 2446,
    incomeMinor: 0,
    groupSharesMinor: 9350,
    leftMinor: -11796,
  },
}

export const euroOnlyOctoberWithShareConvertedToEur: PersonalEntryMonth = {
  month: '2026-10',
  entries: [lunchInEuroEntry],
  totals: [octoberMonth.totals[1]],
  incomeTotals: [],
  groupShares: [euroTaxiShare],
  convertedTotals: { currency: 'EUR', spendingMinor: 1250, incomeMinor: 0, groupSharesMinor: 800, leftMinor: -2050 },
  limits: noLimits,
  possibleDuplicates: [],
}

export const euroOnlyOctoberWithShareConvertedToMkd: PersonalEntryMonth = {
  ...euroOnlyOctoberWithShareConvertedToEur,
  convertedTotals: { currency: 'MKD', spendingMinor: 76900, incomeMinor: 0, groupSharesMinor: 49200, leftMinor: -126100 },
}

export const octoberWithEuroIncomeOnlyOtherCurrency: PersonalEntryMonth = {
  month: '2026-10',
  entries: [coffeeEntry, untitledEuroIncomeEntry],
  totals: [octoberMonth.totals[0]],
  incomeTotals: [{ currency: 'EUR', totalMinor: 20000, count: 1 }],
  groupShares: [],
  convertedTotals: { currency: 'MKD', spendingMinor: 12000, incomeMinor: 1230000, groupSharesMinor: 0, leftMinor: 1218000 },
  limits: noLimits,
  possibleDuplicates: [],
}

export const foodLimitRow: LimitRow = {
  categoryId: personalFoodCategory.id,
  limitMinor: 1000000,
  spentMinor: 1200000,
  overMinor: 200000,
  includedEurMinor: 1250,
}

export const wholeMonthLimitRow: LimitRow = {
  categoryId: null,
  limitMinor: 5000000,
  spentMinor: 755100,
  overMinor: 0,
  includedEurMinor: 2050,
}
