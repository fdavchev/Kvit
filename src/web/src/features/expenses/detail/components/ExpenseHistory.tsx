import { useTranslation } from 'react-i18next'
import type { Language } from '@/core/i18n/language'
import type { Category } from '@/core/services/categories/categoriesService'
import type {
  ExpenseChange,
  ExpenseDetail,
  ExpenseHistoryEntry,
} from '@/core/services/expenses/expensesService'
import { groupCurrencies } from '@/core/services/groups/groupsService'
import { formatExpenseDay, todayInTimeZone } from '@/shared/utils/formatDate'
import { formatMoney, type Currency } from '@/shared/utils/formatMoney'
import { categoryNameKey } from '../../shared/expenseCategories'
import { noValueText } from '../../shared/expenseTitle'

interface ExpenseHistoryProps {
  expense: ExpenseDetail
  categories: readonly Category[]
  timeZone: string
  language: Language
}

interface HistoryLine {
  key: string
  sentences: string[]
  when: string
}

const minuteMs = 60_000
const hourMs = 60 * minuteMs
const dayMs = 24 * hourMs
const daysShownAsRelative = 7

const changeKeys = new Map<string, string>([
  ['amount', 'expense.historyChangedAmount'],
  ['currency', 'expense.historyChangedCurrency'],
  ['title', 'expense.historyChangedTitle'],
  ['note', 'expense.historyChangedNote'],
  ['date', 'expense.historyChangedDate'],
  ['category', 'expense.historyChangedCategory'],
  ['paidBy', 'expense.historyChangedPaidBy'],
])

export function ExpenseHistory({ expense, categories, timeZone, language }: ExpenseHistoryProps) {
  const { t } = useTranslation()
  const now = new Date()
  const today = todayInTimeZone(timeZone, now)

  function valueText(change: ExpenseChange, value: string, currency: Currency): string {
    if (value === '') {
      return noValueText
    }
    switch (change.field) {
      case 'amount':
        return formatMoney(readMinorUnits(value), currency, language)
      case 'date':
        return formatExpenseDay(value, today, language)
      case 'category':
        return t(categoryNameKey(categoryWithKey(categories, value)))
      default:
        return value
    }
  }

  function changeSentence(
    entry: ExpenseHistoryEntry,
    change: ExpenseChange,
    currencyBefore: Currency,
    currencyAfter: Currency,
  ): string {
    if (change.field === 'split') {
      return t('expense.historyChangedSplit', { name: entry.actorName })
    }
    const key = changeKeys.get(change.field)
    if (key === undefined) {
      throw new Error(`The history has a change of a field Kvit does not know: "${change.field}"`)
    }
    return t(key, {
      name: entry.actorName,
      old: valueText(change, change.old, currencyBefore),
      new: valueText(change, change.new, currencyAfter),
    })
  }

  function whenText(createdAt: string): string {
    const moment = new Date(createdAt)
    const elapsedMs = Math.max(0, now.getTime() - moment.getTime())
    const relative = new Intl.RelativeTimeFormat(language)
    if (elapsedMs < hourMs) {
      return relative.format(-Math.floor(elapsedMs / minuteMs), 'minute')
    }
    if (elapsedMs < dayMs) {
      return relative.format(-Math.floor(elapsedMs / hourMs), 'hour')
    }
    if (elapsedMs < daysShownAsRelative * dayMs) {
      return relative.format(-Math.floor(elapsedMs / dayMs), 'day')
    }
    return formatExpenseDay(todayInTimeZone(timeZone, moment), today, language)
  }

  function historyLines(): HistoryLine[] {
    let currencyAfter: Currency = expense.currency
    return expense.history.map((entry, index) => {
      const sentences: string[] = []
      let currencyBefore: Currency = currencyAfter
      switch (entry.type) {
        case 'ExpenseAdded':
          sentences.push(t('expense.historyAdded', { name: entry.actorName }))
          break
        case 'ExpenseDeleted':
          sentences.push(t('expense.historyDeleted', { name: entry.actorName }))
          break
        case 'ExpenseRestored':
          sentences.push(t('expense.historyRestored', { name: entry.actorName }))
          break
        case 'ExpenseEdited': {
          const changes = editChangesOf(entry)
          currencyBefore = currencyBeforeOf(changes, currencyAfter)
          for (const change of changes) {
            sentences.push(changeSentence(entry, change, currencyBefore, currencyAfter))
          }
          break
        }
      }
      currencyAfter = currencyBefore
      return { key: `${entry.createdAt}-${index}`, sentences, when: whenText(entry.createdAt) }
    })
  }

  return (
    <ol className="flex flex-col">
      {historyLines().map((line) => (
        <li key={line.key} className="my-1.5 border-l-[3px] border-field-border py-0.5 pl-3">
          {line.sentences.map((sentence, sentenceIndex) => (
            <p key={`${line.key}-${sentenceIndex}`} className="font-semibold">
              {sentence}
            </p>
          ))}
          <p className="text-[0.8125rem] text-muted-foreground">{line.when}</p>
        </li>
      ))}
    </ol>
  )
}

function editChangesOf(entry: ExpenseHistoryEntry): ExpenseChange[] {
  if (entry.changes === null || entry.changes.length === 0) {
    throw new Error(`The edit by ${entry.actorName} at ${entry.createdAt} has no changes`)
  }
  return entry.changes
}

function currencyBeforeOf(changes: readonly ExpenseChange[], currencyAfter: Currency): Currency {
  const currencyChange = changes.find((change) => change.field === 'currency')
  if (currencyChange === undefined) {
    return currencyAfter
  }
  const currency = groupCurrencies.find((candidate) => candidate === currencyChange.old)
  if (currency === undefined) {
    throw new Error(`The history has a currency Kvit does not know: "${currencyChange.old}"`)
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
