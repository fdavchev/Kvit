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
  readPositiveNumber,
  readText,
  readTextOrNull,
  type Fields,
} from '@/core/services/readFields'
import type { Currency } from '@/shared/utils/formatMoney'

export type PersonalEntryKind = 'Spending' | 'Income'

const personalEntryKinds: readonly PersonalEntryKind[] = ['Spending', 'Income']

export interface PersonalEntry {
  id: string
  kind: PersonalEntryKind
  category: PersonalCategory
  amountMinor: number
  currency: Currency
  entryDate: string
  note: string | null
  mkdPerEur: number
  rateDate: string
  createdAt: string
  updatedAt: string | null
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

export interface PersonalEntryMonth {
  month: string
  entries: PersonalEntry[]
  totals: PersonalCurrencyTotal[]
  incomeTotals: PersonalIncomeTotal[]
}

export interface PersonalEntryFields {
  categoryId: string
  amountMinor: number
  currency: Currency
  entryDate: string
  note: string | null
}

export interface NewPersonalEntry extends PersonalEntryFields {
  clientRequestId: string
}

export async function getPersonalEntries(month: string): Promise<PersonalEntryMonth> {
  const what = 'the personal entries of the month'
  const fields = readObject(await apiRequest(endpoints.personalEntriesOfMonth(month)), what)
  return {
    month: readText(fields, 'month', what),
    entries: readList(fields, 'entries', what).map(parseEntry),
    totals: readList(fields, 'totals', what).map(parseCurrencyTotal),
    incomeTotals: readList(fields, 'incomeTotals', what).map(parseIncomeTotal),
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
  return {
    id: readText(fields, 'id', what),
    kind: readOneOf(fields, 'kind', personalEntryKinds, what),
    category: readCategoryWithItsId(fields, what),
    amountMinor: readCount(fields, 'amountMinor', what),
    currency: readOneOf(fields, 'currency', groupCurrencies, what),
    entryDate: readDate(fields, 'entryDate', what),
    note: readTextOrNull(fields, 'note', what),
    mkdPerEur: readPositiveNumber(fields, 'mkdPerEur', what),
    rateDate: readDate(fields, 'rateDate', what),
    createdAt: readDate(fields, 'createdAt', what),
    updatedAt: readDateOrNull(fields, 'updatedAt', what),
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

function readCategoryWithItsId(fields: Fields, what: string): PersonalCategory {
  const categoryId = readText(fields, 'categoryId', what)
  const category = parsePersonalCategory(fields.category)
  if (category.id !== categoryId) {
    throw new Error(`Expected "categoryId" of ${what} to be the id of its category ${category.id}, got ${categoryId}`)
  }
  return category
}
