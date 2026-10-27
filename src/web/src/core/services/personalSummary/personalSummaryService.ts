import { apiRequest } from '@/core/api/apiClient'
import { endpoints } from '@/core/api/endpoints'
import { groupCurrencies } from '@/core/services/groups/groupsService'
import { readCount, readList, readObject, readOneOf, readText, type Fields } from '@/core/services/readFields'
import type { Currency } from '@/shared/utils/formatMoney'

export interface SummaryCategory {
  categoryId: string
  thisMinor: number
  previousMinor: number
}

export interface SummaryCurrency {
  currency: Currency
  spendingThisMinor: number
  spendingPreviousMinor: number
  incomeThisMinor: number
  incomePreviousMinor: number
  categories: SummaryCategory[]
}

export interface SummaryConvertedTotals {
  currency: Currency
  spendingThisMinor: number
  spendingPreviousMinor: number
  incomeThisMinor: number
  incomePreviousMinor: number
}

export interface PersonalSummary {
  month: string
  previousMonth: string
  currencies: SummaryCurrency[]
  convertedTotals: SummaryConvertedTotals | null
}

export async function getPersonalSummary(month: string): Promise<PersonalSummary> {
  const what = 'the monthly summary'
  const fields = readObject(await apiRequest(endpoints.personalSummary(month)), what)
  return {
    month: readText(fields, 'month', what),
    previousMonth: readText(fields, 'previousMonth', what),
    currencies: readList(fields, 'currencies', what).map(parseCurrency),
    convertedTotals:
      fields.convertedTotals === null
        ? null
        : parseConvertedTotals(readObject(fields.convertedTotals, `"convertedTotals" of ${what}`)),
  }
}

function parseCurrency(row: unknown): SummaryCurrency {
  const what = 'a currency row of the monthly summary'
  const fields = readObject(row, what)
  return {
    currency: readOneOf(fields, 'currency', groupCurrencies, what),
    spendingThisMinor: readCount(fields, 'spendingThisMinor', what),
    spendingPreviousMinor: readCount(fields, 'spendingPreviousMinor', what),
    incomeThisMinor: readCount(fields, 'incomeThisMinor', what),
    incomePreviousMinor: readCount(fields, 'incomePreviousMinor', what),
    categories: readList(fields, 'categories', what).map(parseCategory),
  }
}

function parseCategory(row: unknown): SummaryCategory {
  const what = 'a category row of the monthly summary'
  const fields = readObject(row, what)
  return {
    categoryId: readText(fields, 'categoryId', what),
    thisMinor: readCount(fields, 'thisMinor', what),
    previousMinor: readCount(fields, 'previousMinor', what),
  }
}

function parseConvertedTotals(fields: Fields): SummaryConvertedTotals {
  const what = 'the converted totals of the monthly summary'
  return {
    currency: readOneOf(fields, 'currency', groupCurrencies, what),
    spendingThisMinor: readCount(fields, 'spendingThisMinor', what),
    spendingPreviousMinor: readCount(fields, 'spendingPreviousMinor', what),
    incomeThisMinor: readCount(fields, 'incomeThisMinor', what),
    incomePreviousMinor: readCount(fields, 'incomePreviousMinor', what),
  }
}
