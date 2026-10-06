import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { useSignedInMe } from '@/core/auth/useSignedInMe'
import type { Language } from '@/core/i18n/language'
import { useLanguage } from '@/core/i18n/useLanguage'
import { routes } from '@/core/router/routes'
import type { ExpenseDetail } from '@/core/services/expenses/expensesService'
import type { GroupMember } from '@/core/services/groups/membersService'
import { useMembers } from '@/features/groups/members/hooks/useMembers'
import { GroupLoadError } from '@/features/groups/shared/components/GroupLoadError'
import { useGroupIdParam } from '@/features/groups/shared/useGroupIdParam'
import { KvitAvatar } from '@/shared/components/KvitAvatar'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitLinkButton } from '@/shared/components/KvitLinkButton'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { showUndoToast } from '@/shared/toasts/showUndoToast'
import { formatDayWithYear, formatExpenseDay, todayInTimeZone } from '@/shared/utils/formatDate'
import { formatMoney } from '@/shared/utils/formatMoney'
import { CategoryTile } from '../../shared/components/CategoryTile'
import { ExpenseLoadError } from '../../shared/components/ExpenseLoadError'
import { categoryOf } from '../../shared/expenseCategories'
import { expensePeople, personOf } from '../../shared/expensePeople'
import { expenseTitle } from '../../shared/expenseTitle'
import { useCategories } from '../../shared/hooks/useCategories'
import { useExpense } from '../../shared/hooks/useExpense'
import { useRestoreExpense } from '../../shared/hooks/useRestoreExpense'
import { useExpenseIdParam } from '../../shared/useExpenseIdParam'
import { useDeleteExpense } from '../hooks/useDeleteExpense'
import { ExpenseHistory } from './ExpenseHistory'

const rateDecimals = 4
const sectionTitleLook = 'pt-5 pb-2 text-[1.0625rem] font-bold'

export function ExpenseDetailScreen() {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const navigate = useNavigate()
  const groupId = useGroupIdParam()
  const expenseId = useExpenseIdParam()
  const me = useSignedInMe()
  const expenseQuery = useExpense(groupId, expenseId)
  const membersQuery = useMembers(groupId)
  const categoriesQuery = useCategories()
  const deleteExpense = useDeleteExpense(groupId, expenseId)
  const restoreExpense = useRestoreExpense(groupId)

  function showFailure(error: Error): void {
    console.error('An expense action failed', error)
    toast.error(t(errorMessageKey(error)))
  }

  function undoDelete(): void {
    void restoreExpense
      .mutateAsync(expenseId)
      .then(() => {
        toast.success(t('expenses.restored'))
      })
      .catch(showFailure)
  }

  function deleteNow(): void {
    deleteExpense.mutate(undefined, {
      onSuccess: () => {
        showUndoToast(t('expenses.deleted'), {
          danger: true,
          undoLabel: t('common.undo'),
          onUndo: undoDelete,
        })
        navigate(routes.group(groupId), { replace: true })
      },
      onError: showFailure,
    })
  }

  function renderContent() {
    if (expenseQuery.isError) {
      return (
        <ExpenseLoadError error={expenseQuery.error} onRetry={() => void expenseQuery.refetch()} />
      )
    }
    if (membersQuery.isError) {
      return <GroupLoadError error={membersQuery.error} onRetry={() => void membersQuery.refetch()} />
    }
    if (categoriesQuery.isError) {
      return (
        <GroupLoadError
          error={categoriesQuery.error}
          onRetry={() => void categoriesQuery.refetch()}
        />
      )
    }
    if (expenseQuery.isPending || membersQuery.isPending || categoriesQuery.isPending) {
      return <KvitLoading />
    }
    const expense = expenseQuery.data
    const categories = categoriesQuery.data
    const category = categoryOf(categories, expense.categoryId)
    const today = todayInTimeZone(me.timeZone, new Date())
    return (
      <>
        <div className="flex flex-col items-center gap-1.5 pb-2 text-center">
          <CategoryTile category={category} size="large" />
          <h1 className="text-[1.75rem] leading-[1.15] font-extrabold tracking-[-0.02em] text-balance">
            {expenseTitle(expense.title, category, t)}
          </h1>
          <p className="text-[2.2rem] leading-tight font-extrabold tracking-[-0.02em]">
            {formatMoney(expense.amountMinor, expense.currency, language)}
          </p>
          <p className="text-[0.9375rem] text-muted-foreground">
            {t('expense.paidByOn', {
              name: expense.paidByName,
              date: formatExpenseDay(expense.expenseDate, today, language),
            })}
          </p>
          {expense.note !== null && <p className="text-pretty">{expense.note}</p>}
        </div>
        <h2 className={sectionTitleLook}>{t('expense.split')}</h2>
        <SplitRows expense={expense} members={membersQuery.data.members} language={language} />
        {expense.currency === 'EUR' && (
          <p className="pt-2.5 text-[0.8125rem] text-muted-foreground">
            {t('expense.rate', {
              rate: formatRate(expense.mkdPerEur, language),
              date: formatDayWithYear(expense.rateDate, language),
            })}
          </p>
        )}
        <h2 className={sectionTitleLook}>{t('expense.history')}</h2>
        <ExpenseHistory
          expense={expense}
          categories={categories}
          timeZone={me.timeZone}
          language={language}
        />
        {expense.canEdit && (
          <div className="flex flex-col gap-2.5 pt-6">
            <KvitLinkButton to={routes.groupExpenseEdit(groupId, expense.id)} variant="secondary">
              {t('expense.edit')}
            </KvitLinkButton>
            <KvitButton variant="danger" disabled={deleteExpense.isPending} onClick={deleteNow}>
              {t('expense.delete')}
            </KvitButton>
          </div>
        )}
      </>
    )
  }

  return (
    <KvitScreen>
      <div className="flex min-h-12">
        <KvitBackButton to={routes.group(groupId)} />
      </div>
      {renderContent()}
    </KvitScreen>
  )
}

interface SplitRowsProps {
  expense: ExpenseDetail
  members: readonly GroupMember[]
  language: Language
}

function SplitRows({ expense, members, language }: SplitRowsProps) {
  const people = expensePeople(members, expense.shares)
  return (
    <ul className="flex flex-col gap-2">
      {expense.shares.map((share) => {
        const person = personOf(people, share.memberId)
        return (
          <li
            key={share.memberId}
            className="flex min-h-13 items-center gap-3 rounded-2xl bg-card px-3.5 py-2 text-card-foreground"
          >
            <KvitAvatar name={share.name} pictureUrl={person.pictureUrl} colorIndex={person.colorIndex} />
            <span className="min-w-0 flex-1 font-semibold break-words">{share.name}</span>
            <span className="font-bold whitespace-nowrap">
              {formatMoney(share.shareMinor, expense.currency, language)}
            </span>
          </li>
        )
      })}
    </ul>
  )
}

function formatRate(mkdPerEur: number, language: Language): string {
  return new Intl.NumberFormat(language, {
    minimumFractionDigits: rateDecimals,
    maximumFractionDigits: rateDecimals,
  }).format(mkdPerEur)
}
