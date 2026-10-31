import { apiRequest } from '@/core/api/apiClient'
import { endpoints } from '@/core/api/endpoints'
import { groupCurrencies } from '@/core/services/groups/groupsService'
import {
  readCount,
  readCountAboveZero,
  readList,
  readObject,
  readOneOf,
  readTextOrNull,
} from '@/core/services/readFields'
import type { Currency } from '@/shared/utils/formatMoney'

export interface CategorySpending {
  categoryId: string | null
  totalMinor: number
}

export interface CurrencySpending {
  currency: Currency
  totalMinor: number
  categories: CategorySpending[]
}

export interface GroupSpendingPlan {
  amountMinor: number
  currency: Currency
  spentMinor: number
  perPersonMinor: number
  overMinor: number
  includedOtherCurrencyMinor: number
}

export interface GroupSpending {
  currencies: CurrencySpending[]
  plan: GroupSpendingPlan | null
}

export async function getGroupSpending(groupId: string): Promise<GroupSpending> {
  const what = 'the spending of the group'
  const fields = readObject(await apiRequest(endpoints.groupSpending(groupId)), what)
  return {
    currencies: readList(fields, 'currencies', what).map(parseCurrencySpending),
    plan: fields.plan === null ? null : parsePlan(fields.plan, `"plan" of ${what}`),
  }
}

function parseCurrencySpending(row: unknown): CurrencySpending {
  const what = 'the spending in one currency'
  const fields = readObject(row, what)
  return {
    currency: readOneOf(fields, 'currency', groupCurrencies, what),
    totalMinor: readCount(fields, 'totalMinor', what),
    categories: readList(fields, 'categories', what).map(parseCategorySpending),
  }
}

function parseCategorySpending(row: unknown): CategorySpending {
  const what = 'the spending in one category'
  const fields = readObject(row, what)
  return {
    categoryId: readTextOrNull(fields, 'categoryId', what),
    totalMinor: readCount(fields, 'totalMinor', what),
  }
}

function parsePlan(value: unknown, what: string): GroupSpendingPlan {
  const fields = readObject(value, what)
  return {
    amountMinor: readCountAboveZero(fields, 'amountMinor', what),
    currency: readOneOf(fields, 'currency', groupCurrencies, what),
    spentMinor: readCount(fields, 'spentMinor', what),
    perPersonMinor: readCount(fields, 'perPersonMinor', what),
    overMinor: readCount(fields, 'overMinor', what),
    includedOtherCurrencyMinor: readCount(fields, 'includedOtherCurrencyMinor', what),
  }
}
