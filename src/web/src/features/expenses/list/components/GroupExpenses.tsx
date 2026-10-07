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
import { addDays, formatExpenseDay, todayInTimeZone } from '@/shared/utils/formatDate'
import { categoryOf } from '../../shared/expenseCategories'
import { useCategories } from '../../shared/hooks/useCategories'
import { useExpenses } from '../hooks/useExpenses'
import { ExpenseListItem } from './ExpenseListItem'

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
      return <KvitLoading />
    }
    if (expensesQuery.data.length === 0) {
      return (
        <>
          <KvitEmpty emoji={receiptEmoji} title={t('expenses.emptyTitle')} message={t('expenses.emptyHint')} />
          {actionsWhenEmpty}
        </>
      )
    }
    const categories = categoriesQuery.data
    const today = todayInTimeZone(me.timeZone, new Date())
    return groupByDay(expensesQuery.data).map((day) => (
      <section key={day.date} className="flex flex-col gap-2">
        <h2 className="mx-1 mt-3.5 text-[0.8125rem] font-bold tracking-[0.04em] text-muted-foreground uppercase">
          {dayLabel(day.date, today)}
        </h2>
        {day.rows.map((row) => (
          <ExpenseListItem
            key={row.id}
            groupId={groupId}
            row={row}
            category={categoryOf(categories, row.categoryId)}
            language={language}
          />
        ))}
      </section>
    ))
  }

  return (
    <div className="flex flex-col pb-24">
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
      <div className="pointer-events-none fixed inset-x-0 bottom-0 z-10">
        <div className="mx-auto flex w-full max-w-md justify-end px-6 pb-[max(24px,env(safe-area-inset-bottom))]">
          <Link
            to={routes.groupExpenseNew(groupId)}
            aria-label={t('expense.addTitle')}
            className="pressable pointer-events-auto grid size-15 place-items-center rounded-[20px] bg-primary text-[2rem] leading-none text-primary-foreground shadow-[0_4px_14px_rgb(0_0_0/30%)] hover:bg-primary-hover"
          >
            <span aria-hidden="true">+</span>
          </Link>
        </div>
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
