import type { TFunction } from 'i18next'
import type { OutboxItem, OutboxItemDisplay, OutboxItemKind } from '@/core/outbox/outboxItem'
import { groupCurrencies } from '@/core/services/groups/groupsService'
import { noValueText } from '@/features/expenses/shared/expenseTitle'
import { monthOfDate } from '@/shared/utils/calendarMonth'
import type { Currency } from '@/shared/utils/formatMoney'

export interface WaitingAmount {
  amountMinor: number
  currency: Currency
}

const requiredDisplayFields: Record<OutboxItemKind, readonly (keyof OutboxItemDisplay)[]> = {
  expense: ['amountMinor', 'currency', 'paidByName'],
  oneBill: ['amountMinor', 'currency'],
  personalEntry: ['amountMinor', 'currency', 'date'],
  settlement: ['amountMinor', 'currency', 'payerName', 'receiverName'],
}

export function amountOfItem(item: OutboxItem): WaitingAmount | null {
  const { amountMinor } = item.display
  const currency = currencyOf(item)
  if (amountMinor === undefined || currency === null) {
    return null
  }
  return { amountMinor, currency }
}

export function displayProblemOf(item: OutboxItem, unknownCategoryId: string | null = null): string | null {
  const problems: string[] = []
  const missingFields = requiredDisplayFields[item.kind].filter((field) => item.display[field] === undefined)
  if (missingFields.length > 0) {
    problems.push(`it has no ${missingFields.join(', ')}`)
  }
  if (item.display.currency !== undefined && currencyOf(item) === null) {
    problems.push(`its currency ${item.display.currency} is neither MKD nor EUR`)
  }
  if (unknownCategoryId !== null) {
    problems.push(`its category ${unknownCategoryId} is not one of the categories`)
  }
  if (problems.length === 0) {
    return null
  }
  return `The waiting ${item.kind} ${item.id} is shown with a dash in place of what it is missing: ${problems.join('; ')}`
}

export interface WaitingItemCategory<Category> {
  category: Category | null
  unknownCategoryId: string | null
}

export function categoryOfWaitingItem<Category extends { id: string }>(
  item: OutboxItem,
  categories: readonly Category[],
): WaitingItemCategory<Category> {
  const categoryId = item.display.categoryId ?? null
  if (categoryId === null) {
    return { category: null, unknownCategoryId: null }
  }
  const category = categories.find((candidate) => candidate.id === categoryId) ?? null
  return { category, unknownCategoryId: category === null ? categoryId : null }
}

export function paymentTitleOf(item: OutboxItem, t: TFunction): string {
  const { payerName, receiverName } = item.display
  if (payerName === undefined || receiverName === undefined) {
    return noValueText
  }
  return t('settlements.fromPaidTo', { from: payerName, to: receiverName })
}

export function oneBillNameOf(item: OutboxItem): string {
  return item.display.groupName ?? item.display.title ?? noValueText
}

export function titleWithoutCategoryOf(item: OutboxItem, t: TFunction): string {
  switch (item.kind) {
    case 'settlement':
      return paymentTitleOf(item, t)
    case 'oneBill':
      return oneBillNameOf(item)
    case 'personalEntry':
      return item.display.title ?? (item.display.entryKind === 'Income' ? t('budget.income.listTitle') : noValueText)
    case 'expense':
      return item.display.title ?? noValueText
    default:
      throw new Error(`The waiting item ${item.id} has the unknown kind ${String(item.kind satisfies never)}`)
  }
}

export function isInMonth(item: OutboxItem, month: string): boolean {
  const { date } = item.display
  return date === undefined || monthOfDate(date) === month
}

function currencyOf(item: OutboxItem): Currency | null {
  return groupCurrencies.find((candidate) => candidate === item.display.currency) ?? null
}
