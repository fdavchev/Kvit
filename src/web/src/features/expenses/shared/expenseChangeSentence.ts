import type { TFunction } from 'i18next'
import type { Language } from '@/core/i18n/language'
import type { Category } from '@/core/services/categories/categoriesService'
import type { ExpenseChange } from '@/core/services/expenses/expensesService'
import { groupCurrencies } from '@/core/services/groups/groupsService'
import { formatExpenseDay } from '@/shared/utils/formatDate'
import { formatMoney, type Currency } from '@/shared/utils/formatMoney'
import { categoryNameKey } from './expenseCategories'
import { noValueText } from './expenseTitle'

export interface ChangeSentenceContext {
  t: TFunction
  language: Language
  today: string
  categories: readonly Category[]
}

export interface ChangeCurrencies {
  before: Currency
  after: Currency
}

const changeKeys = new Map<string, string>([
  ['amount', 'expense.historyChangedAmount'],
  ['currency', 'expense.historyChangedCurrency'],
  ['title', 'expense.historyChangedTitle'],
  ['note', 'expense.historyChangedNote'],
  ['date', 'expense.historyChangedDate'],
  ['category', 'expense.historyChangedCategory'],
  ['paidBy', 'expense.historyChangedPaidBy'],
])

export function expenseChangeSentence(
  change: ExpenseChange,
  actorName: string,
  currencies: ChangeCurrencies,
  context: ChangeSentenceContext,
): string {
  if (change.field === 'split') {
    return context.t('expense.historyChangedSplit', { name: actorName })
  }
  const key = changeKeys.get(change.field)
  if (key === undefined) {
    throw new Error(`The history has a change of a field Kvit does not know: "${change.field}"`)
  }
  return context.t(key, {
    name: actorName,
    old: valueText(change, change.old, currencies.before, context),
    new: valueText(change, change.new, currencies.after, context),
  })
}

export function currencyBeforeOf(changes: readonly ExpenseChange[], currencyAfter: Currency): Currency {
  const currencyChange = changes.find((change) => change.field === 'currency')
  return currencyChange === undefined ? currencyAfter : readCurrency(currencyChange.old)
}

export function currencyAfterOf(changes: readonly ExpenseChange[], currencyBefore: Currency): Currency {
  const currencyChange = changes.find((change) => change.field === 'currency')
  return currencyChange === undefined ? currencyBefore : readCurrency(currencyChange.new)
}

function valueText(
  change: ExpenseChange,
  value: string,
  currency: Currency,
  context: ChangeSentenceContext,
): string {
  if (value === '') {
    return noValueText
  }
  switch (change.field) {
    case 'amount':
      return formatMoney(readMinorUnits(value), currency, context.language)
    case 'date':
      return formatExpenseDay(value, context.today, context.language)
    case 'category':
      return context.t(categoryNameKey(categoryWithKey(context.categories, value)))
    default:
      return value
  }
}

function readCurrency(text: string): Currency {
  const currency = groupCurrencies.find((candidate) => candidate === text)
  if (currency === undefined) {
    throw new Error(`The history has a currency Kvit does not know: "${text}"`)
  }
  return currency
}

function readMinorUnits(value: string): number {
  if (!/^\d+$/.test(value)) {
    throw new Error(`The history has an amount that is not a whole number of minor units: "${value}"`)
  }
  return Number(value)
}

function categoryWithKey(categories: readonly Category[], key: string): Category {
  const category = categories.find((candidate) => candidate.key === key)
  if (category === undefined) {
    throw new Error(`The history names a category Kvit does not know: "${key}"`)
  }
  return category
}
