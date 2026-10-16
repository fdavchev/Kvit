import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useSignedInMe } from '@/core/auth/useSignedInMe'
import { useLanguage } from '@/core/i18n/useLanguage'
import { routes } from '@/core/router/routes'
import type { ExpenseListRow } from '@/core/services/expenses/expensesService'
import { GroupLoadError } from '@/features/groups/shared/components/GroupLoadError'
import { KvitEmpty } from '@/shared/components/KvitEmpty'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitSkeleton } from '@/shared/components/KvitSkeleton'
import { useListAnimation } from '@/shared/hooks/useListAnimation'
import { addDays, formatExpenseDay, todayInTimeZone } from '@/shared/utils/formatDate'
import { useCategories } from '../../shared/hooks/useCategories'
import { useExpenses } from '../hooks/useExpenses'
import { ExpenseDaySection } from './ExpenseDaySection'

interface GroupExpensesProps {
  groupId: string
  actionsWhenEmpty: ReactNode
}

interface ExpenseDay {
  date: string
  rows: ExpenseListRow[]
}

const receiptEmoji = '\u{1F9FE}'

export function GroupExpenses({ groupId, actionsWhenEmpty }: GroupExpensesProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const me = useSignedInMe()
  const expensesQuery = useExpenses(groupId)
  const categoriesQuery = useCategories()
  const daysRef = useListAnimation<HTMLDivElement>()

  function dayLabel(date: string, today: string): string {
    const day = formatExpenseDay(date, today, language)
    if (date === today) {
      return `${t('expenses.today')} · ${day}`
    }
    if (date === addDays(today, -1)) {
      return `${t('expenses.yesterday')} · ${day}`
    }
    return day
  }

  function renderList() {
    if (expensesQuery.isError) {
      return <GroupLoadError error={expensesQuery.error} onRetry={() => void expensesQuery.refetch()} />
    }
    if (categoriesQuery.isError) {
      return (
        <GroupLoadError error={categoriesQuery.error} onRetry={() => void categoriesQuery.refetch()} />
      )
    }
    if (expensesQuery.isPending || categoriesQuery.isPending) {
      return (
        <KvitLoading>
          <div className="flex flex-col gap-2">
            <KvitSkeleton className="mx-1 mt-4 mb-0.5 h-3.5 w-28 rounded-full" />
            <KvitSkeleton className="h-16" />
            <KvitSkeleton className="h-16" />
            <KvitSkeleton className="h-16" />
          </div>
        </KvitLoading>
      )
    }
    if (expensesQuery.data.length === 0) {
      return (
        <>
          <KvitEmpty emoji={receiptEmoji} title={t('expenses.emptyTitle')} message={t('expenses.emptyHint')} />
          {actionsWhenEmpty}
        </>
      )
    }
    const today = todayInTimeZone(me.timeZone, new Date())
    return (
      <div ref={daysRef} className="flex flex-col">
        {groupByDay(expensesQuery.data).map((day) => (
          <ExpenseDaySection
            key={day.date}
            groupId={groupId}
            label={dayLabel(day.date, today)}
            rows={day.rows}
            categories={categoriesQuery.data}
            language={language}
          />
        ))}
      </div>
    )
  }

  return (
    <div className="flex flex-col">
      {renderList()}
      <div className="flex justify-center pt-3">
        <Link
          to={routes.groupExpensesDeleted(groupId)}
          className="pressable inline-flex min-h-11 items-center gap-1 rounded-xl px-3 text-[0.9375rem] font-semibold text-link hover:underline"
        >
          {t('groups.recentlyDeletedLink')}
          <span aria-hidden="true">›</span>
        </Link>
      </div>
    </div>
  )
}

function groupByDay(rows: readonly ExpenseListRow[]): ExpenseDay[] {
  const days: ExpenseDay[] = []
  for (const row of rows) {
    const lastDay = days.at(-1)
    if (lastDay !== undefined && lastDay.date === row.expenseDate) {
      lastDay.rows.push(row)
    } else {
      days.push({ date: row.expenseDate, rows: [row] })
    }
  }
  return days
}
