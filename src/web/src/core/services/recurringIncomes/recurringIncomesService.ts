import { apiRequest, jsonRequest } from '@/core/api/apiClient'
import { endpoints } from '@/core/api/endpoints'
import { groupCurrencies } from '@/core/services/groups/groupsService'
import {
  readCount,
  readCountOrNull,
  readDate,
  readDateOrNull,
  readList,
  readObject,
  readOneOf,
  readText,
  readTextOrNull,
  type Fields,
} from '@/core/services/readFields'
import type { Currency } from '@/shared/utils/formatMoney'

export type RecurringIncomeFrequency = 'Monthly' | 'Weekly'

const recurringIncomeFrequencies: readonly RecurringIncomeFrequency[] = ['Monthly', 'Weekly']

export interface RecurringIncomeFields {
  amountMinor: number
  currency: Currency
  title: string | null
  frequency: RecurringIncomeFrequency
  dayOfMonth: number | null
  dayOfWeek: number | null
  startsOn: string
  endsOn: string | null
}

export type NewRecurringIncome = RecurringIncomeFields & { clientRequestId: string }

export interface RecurringIncome extends RecurringIncomeFields {
  id: string
  createdAt: string
  nextDate: string | null
}

interface RecurringIncomeDays {
  dayOfMonth: number | null
  dayOfWeek: number | null
}

const lastDayOfMonth = 31
const lastDayOfWeek = 6

export async function getRecurringIncomes(): Promise<RecurringIncome[]> {
  const what = 'the repeating incomes'
  const fields = readObject(await apiRequest(endpoints.recurringIncomes), what)
  return readList(fields, 'recurringIncomes', what).map(parseRecurringIncome)
}

export async function createRecurringIncome(income: NewRecurringIncome): Promise<RecurringIncome> {
  return parseRecurringIncome(await apiRequest(endpoints.recurringIncomes, jsonRequest('POST', income)))
}

export async function editRecurringIncome(
  recurringIncomeId: string,
  fields: RecurringIncomeFields,
): Promise<RecurringIncome> {
  return parseRecurringIncome(
    await apiRequest(endpoints.recurringIncome(recurringIncomeId), jsonRequest('PUT', fields)),
  )
}

export async function stopRecurringIncome(recurringIncomeId: string): Promise<void> {
  await apiRequest(endpoints.recurringIncomeStop(recurringIncomeId), { method: 'POST' })
}

export async function restartRecurringIncome(recurringIncomeId: string): Promise<void> {
  await apiRequest(endpoints.recurringIncomeRestart(recurringIncomeId), { method: 'POST' })
}

export async function catchUpRecurringIncomes(): Promise<number> {
  const what = 'the catch-up of the repeating incomes'
  const fields = readObject(await apiRequest(endpoints.recurringIncomesCatchUp, { method: 'POST' }), what)
  return readCount(fields, 'created', what)
}

function parseRecurringIncome(row: unknown): RecurringIncome {
  const what = 'a repeating income'
  const fields = readObject(row, what)
  const frequency = readOneOf(fields, 'frequency', recurringIncomeFrequencies, what)
  return {
    id: readText(fields, 'id', what),
    amountMinor: readCount(fields, 'amountMinor', what),
    currency: readOneOf(fields, 'currency', groupCurrencies, what),
    title: readTextOrNull(fields, 'title', what),
    frequency,
    ...readDays(fields, frequency, what),
    startsOn: readDate(fields, 'startsOn', what),
    endsOn: readDateOrNull(fields, 'endsOn', what),
    createdAt: readDate(fields, 'createdAt', what),
    nextDate: readDateOrNull(fields, 'nextDate', what),
  }
}

function readDays(fields: Fields, frequency: RecurringIncomeFrequency, what: string): RecurringIncomeDays {
  const dayOfMonth = readCountOrNull(fields, 'dayOfMonth', what)
  const dayOfWeek = readCountOrNull(fields, 'dayOfWeek', what)
  if (frequency === 'Monthly') {
    if (dayOfMonth === null || dayOfMonth < 1 || dayOfMonth > lastDayOfMonth) {
      throw new Error(`Expected "dayOfMonth" of a Monthly ${what} to be a day from 1 to ${lastDayOfMonth}, got ${dayOfMonth}`)
    }
    if (dayOfWeek !== null) {
      throw new Error(`Expected "dayOfWeek" of a Monthly ${what} to be null, got ${dayOfWeek}`)
    }
    return { dayOfMonth, dayOfWeek }
  }
  if (dayOfWeek === null || dayOfWeek > lastDayOfWeek) {
    throw new Error(`Expected "dayOfWeek" of a Weekly ${what} to be a weekday from 0 to ${lastDayOfWeek}, got ${dayOfWeek}`)
  }
  if (dayOfMonth !== null) {
    throw new Error(`Expected "dayOfMonth" of a Weekly ${what} to be null, got ${dayOfMonth}`)
  }
  return { dayOfMonth, dayOfWeek }
}
