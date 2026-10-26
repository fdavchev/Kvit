import { apiRequest, jsonRequest } from '@/core/api/apiClient'
import { endpoints } from '@/core/api/endpoints'
import { groupCurrencies } from '@/core/services/groups/groupsService'
import {
  parsePersonalCategory,
  type PersonalCategory,
} from '@/core/services/personalCategories/personalCategoriesService'
import {
  readCount,
  readDate,
  readDateOrNull,
  readList,
  readObject,
  readOneOf,
  readOrNullWhenMissing,
  readPositiveNumber,
  readText,
  readTextOrNull,
  readWholeNumber,
  type Fields,
} from '@/core/services/readFields'
import type { Currency } from '@/shared/utils/formatMoney'

export type PersonalEntryKind = 'Spending' | 'Income'

const personalEntryKinds: readonly PersonalEntryKind[] = ['Spending', 'Income']

export interface PersonalEntry {
  id: string
  kind: PersonalEntryKind
  category: PersonalCategory | null
  amountMinor: number
  currency: Currency
  entryDate: string
  note: string | null
  mkdPerEur: number
  rateDate: string
  createdAt: string
  updatedAt: string | null
  recurringIncomeId: string | null
}

export interface PersonalCategoryTotal {
  category: PersonalCategory
  totalMinor: number
  count: number
}

export interface PersonalCurrencyTotal {
  currency: Currency
  totalMinor: number
  categories: PersonalCategoryTotal[]
}

export interface PersonalIncomeTotal {
  currency: Currency
  totalMinor: number
  count: number
}

export interface GroupShare {
  expenseId: string
  groupId: string
  groupName: string
  title: string | null
  categoryId: string | null
  entryDate: string
  currency: Currency
  shareMinor: number
}

export interface ConvertedTotals {
  currency: Currency
  spendingMinor: number
  incomeMinor: number
  groupSharesMinor: number
  leftMinor: number
}

export interface LimitRow {
  categoryId: string | null
  limitMinor: number
  spentMinor: number
  overMinor: number
  includedEurMinor: number
}

export interface MonthLimits {
  month: LimitRow | null
  categories: LimitRow[]
}

export interface PersonalEntryMonth {
  month: string
  entries: PersonalEntry[]
  totals: PersonalCurrencyTotal[]
  incomeTotals: PersonalIncomeTotal[]
  groupShares: GroupShare[]
  convertedTotals: ConvertedTotals | null
  limits: MonthLimits
}

interface PersonalEntryMoneyFields {
  amountMinor: number
  currency: Currency
  entryDate: string
  note: string | null
}

export interface PersonalSpendingFields extends PersonalEntryMoneyFields {
  kind: 'Spending'
  categoryId: string
}

export interface PersonalIncomeFields extends PersonalEntryMoneyFields {
  kind: 'Income'
}

export type PersonalEntryFields = PersonalSpendingFields | PersonalIncomeFields

export type NewPersonalEntry = PersonalEntryFields & { clientRequestId: string }

export async function getPersonalEntries(month: string): Promise<PersonalEntryMonth> {
  const what = 'the personal entries of the month'
  const fields = readObject(await apiRequest(endpoints.personalEntriesOfMonth(month)), what)
  return {
    month: readText(fields, 'month', what),
    entries: readList(fields, 'entries', what).map(parseEntry),
    totals: readList(fields, 'totals', what).map(parseCurrencyTotal),
    incomeTotals: readList(fields, 'incomeTotals', what).map(parseIncomeTotal),
    groupShares: readList(fields, 'groupShares', what).map(parseGroupShare),
    convertedTotals:
      fields.convertedTotals === null
        ? null
        : parseConvertedTotals(readObject(fields.convertedTotals, `"convertedTotals" of ${what}`)),
    limits: parseMonthLimits(readObject(fields.limits, `"limits" of ${what}`)),
  }
}

export async function addPersonalEntry(entry: NewPersonalEntry): Promise<PersonalEntry> {
  return parseEntry(await apiRequest(endpoints.personalEntries, jsonRequest('POST', entry)))
}

export async function editPersonalEntry(entryId: string, fields: PersonalEntryFields): Promise<PersonalEntry> {
  return parseEntry(await apiRequest(endpoints.personalEntry(entryId), jsonRequest('PUT', fields)))
}

export async function deletePersonalEntry(entryId: string): Promise<void> {
  await apiRequest(endpoints.personalEntry(entryId), { method: 'DELETE' })
}

export async function restorePersonalEntry(entryId: string): Promise<void> {
  await apiRequest(endpoints.personalEntryRestore(entryId), { method: 'POST' })
}

function parseEntry(row: unknown): PersonalEntry {
  const what = 'a personal entry'
  const fields = readObject(row, what)
  const kind = readOneOf(fields, 'kind', personalEntryKinds, what)
  return {
    id: readText(fields, 'id', what),
    kind,
    category: readEntryCategory(fields, kind, what),
    amountMinor: readCount(fields, 'amountMinor', what),
    currency: readOneOf(fields, 'currency', groupCurrencies, what),
    entryDate: readDate(fields, 'entryDate', what),
    note: readTextOrNull(fields, 'note', what),
    mkdPerEur: readPositiveNumber(fields, 'mkdPerEur', what),
    rateDate: readDate(fields, 'rateDate', what),
    createdAt: readDate(fields, 'createdAt', what),
    updatedAt: readDateOrNull(fields, 'updatedAt', what),
    recurringIncomeId: readOrNullWhenMissing(fields, 'recurringIncomeId', what, readTextOrNull),
  }
}

function parseCurrencyTotal(row: unknown): PersonalCurrencyTotal {
  const what = 'a currency total of personal entries'
  const fields = readObject(row, what)
  return {
    currency: readOneOf(fields, 'currency', groupCurrencies, what),
    totalMinor: readCount(fields, 'totalMinor', what),
    categories: readList(fields, 'categories', what).map(parseCategoryTotal),
  }
}

function parseIncomeTotal(row: unknown): PersonalIncomeTotal {
  const what = 'a currency total of personal income'
  const fields = readObject(row, what)
  return {
    currency: readOneOf(fields, 'currency', groupCurrencies, what),
    totalMinor: readCount(fields, 'totalMinor', what),
    count: readCount(fields, 'count', what),
  }
}

function parseCategoryTotal(row: unknown): PersonalCategoryTotal {
  const what = 'a category total of personal entries'
  const fields = readObject(row, what)
  return {
    category: readCategoryWithItsId(fields, what),
    totalMinor: readCount(fields, 'totalMinor', what),
    count: readCount(fields, 'count', what),
  }
}

function parseGroupShare(row: unknown): GroupShare {
  const what = 'a group share of the month'
  const fields = readObject(row, what)
  return {
    expenseId: readText(fields, 'expenseId', what),
    groupId: readText(fields, 'groupId', what),
    groupName: readText(fields, 'groupName', what),
    title: readTextOrNull(fields, 'title', what),
    categoryId: readTextOrNull(fields, 'categoryId', what),
    entryDate: readDate(fields, 'entryDate', what),
    currency: readOneOf(fields, 'currency', groupCurrencies, what),
    shareMinor: readCount(fields, 'shareMinor', what),
  }
}

function parseConvertedTotals(fields: Fields): ConvertedTotals {
  const what = 'the converted totals of the month'
  return {
    currency: readOneOf(fields, 'currency', groupCurrencies, what),
    spendingMinor: readCount(fields, 'spendingMinor', what),
    incomeMinor: readCount(fields, 'incomeMinor', what),
    groupSharesMinor: readCount(fields, 'groupSharesMinor', what),
    leftMinor: readWholeNumber(fields, 'leftMinor', what),
  }
}

function parseMonthLimits(fields: Fields): MonthLimits {
  return {
    month: fields.month === null ? null : parseLimitRow(fields.month, 'the "month" limit of the whole month'),
    categories: readList(fields, 'categories', 'the limits of the month').map((row) =>
      parseLimitRow(row, 'a category limit of the month'),
    ),
  }
}

function parseLimitRow(row: unknown, what: string): LimitRow {
  const fields = readObject(row, what)
  return {
    categoryId: readTextOrNull(fields, 'categoryId', what),
    limitMinor: readCount(fields, 'limitMinor', what),
    spentMinor: readCount(fields, 'spentMinor', what),
    overMinor: readCount(fields, 'overMinor', what),
    includedEurMinor: readCount(fields, 'includedEurMinor', what),
  }
}

function readEntryCategory(fields: Fields, kind: PersonalEntryKind, what: string): PersonalCategory | null {
  const hasNoCategory = fields.category === null && fields.categoryId === null
  if (kind === 'Income') {
    if (!hasNoCategory) {
      throw new Error(`Expected "category" and "categoryId" of ${what} of kind Income to be null`)
    }
    return null
  }
  if (fields.category === null || fields.categoryId === null) {
    throw new Error(`Expected "category" and "categoryId" of ${what} of kind Spending to be set, got null`)
  }
  return readCategoryWithItsId(fields, what)
}

function readCategoryWithItsId(fields: Fields, what: string): PersonalCategory {
  const categoryId = readText(fields, 'categoryId', what)
  const category = parsePersonalCategory(fields.category)
  if (category.id !== categoryId) {
    throw new Error(`Expected "categoryId" of ${what} to be the id of its category ${category.id}, got ${categoryId}`)
  }
  return category
}
