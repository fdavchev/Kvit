import type { TFunction } from 'i18next'
import type { OutboxFailure, OutboxItem } from '@/core/outbox/outboxItem'
import { groupCurrencies } from '@/core/services/groups/groupsService'
import { noValueText } from '@/features/expenses/shared/expenseTitle'
import { monthOfDate } from '@/shared/utils/calendarMonth'
import type { Currency } from '@/shared/utils/formatMoney'

export interface WaitingAmount {
  amountMinor: number
  currency: Currency
}

type RequiredDisplayText = 'date' | 'paidByName' | 'payerName' | 'receiverName'

export function amountOfItem(item: OutboxItem): WaitingAmount {
  const { amountMinor, currency } = item.display
  if (amountMinor === undefined) {
    throw new Error(`The waiting ${item.kind} ${item.id} has no amount to show`)
  }
  const knownCurrency = groupCurrencies.find((candidate) => candidate === currency)
  if (knownCurrency === undefined) {
    throw new Error(`The waiting ${item.kind} ${item.id} has the currency ${String(currency)}, which is neither MKD nor EUR`)
  }
  return { amountMinor, currency: knownCurrency }
}

export function displayTextOf(item: OutboxItem, field: RequiredDisplayText): string {
  const value = item.display[field]
  if (value === undefined) {
    throw new Error(`The waiting ${item.kind} ${item.id} has no ${field} to show`)
  }
  return value
}

export function failureOf(item: OutboxItem): OutboxFailure {
  if (item.failure === null) {
    throw new Error(`The failed ${item.kind} ${item.id} has no failure reason`)
  }
  return item.failure
}

export function paymentTitleOf(item: OutboxItem, t: TFunction): string {
  return t('settlements.fromPaidTo', { from: displayTextOf(item, 'payerName'), to: displayTextOf(item, 'receiverName') })
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
  return monthOfDate(displayTextOf(item, 'date')) === month
}
