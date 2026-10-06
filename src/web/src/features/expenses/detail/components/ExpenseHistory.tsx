import { useTranslation } from 'react-i18next'
import type { Language } from '@/core/i18n/language'
import type { Category } from '@/core/services/categories/categoriesService'
import type {
  ExpenseChange,
  ExpenseDetail,
  ExpenseHistoryEntry,
} from '@/core/services/expenses/expensesService'
import { formatHowLongAgo, todayInTimeZone } from '@/shared/utils/formatDate'
import type { Currency } from '@/shared/utils/formatMoney'
import {
  currencyBeforeOf,
  expenseChangeSentence,
  type ChangeSentenceContext,
} from '../../shared/expenseChangeSentence'

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

export function ExpenseHistory({ expense, categories, timeZone, language }: ExpenseHistoryProps) {
  const { t } = useTranslation()
  const now = new Date()
  const context: ChangeSentenceContext = {
    t,
    language,
    today: todayInTimeZone(timeZone, now),
    categories,
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
            sentences.push(
              expenseChangeSentence(
                change,
                entry.actorName,
                { before: currencyBefore, after: currencyAfter },
                context,
              ),
            )
          }
          break
        }
      }
      currencyAfter = currencyBefore
      return {
        key: `${entry.createdAt}-${index}`,
        sentences,
        when: formatHowLongAgo(entry.createdAt, now, timeZone, language),
      }
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
