import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { useLanguage } from '@/core/i18n/useLanguage'
import { routes } from '@/core/router/routes'
import { GroupLoadError } from '@/features/groups/shared/components/GroupLoadError'
import { useGroupIdParam } from '@/features/groups/shared/useGroupIdParam'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitEmpty } from '@/shared/components/KvitEmpty'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'
import { formatDayMonthYear } from '@/shared/utils/formatDate'
import { formatMoney } from '@/shared/utils/formatMoney'
import { CategoryTile } from '../../shared/components/CategoryTile'
import { categoryOf } from '../../shared/expenseCategories'
import { expenseTitle } from '../../shared/expenseTitle'
import { useCategories } from '../../shared/hooks/useCategories'
import { useRestoreExpense } from '../../shared/hooks/useRestoreExpense'
import { useDeletedExpenses } from '../hooks/useDeletedExpenses'

export function RecentlyDeletedExpensesScreen() {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const groupId = useGroupIdParam()
  const deletedQuery = useDeletedExpenses(groupId)
  const categoriesQuery = useCategories()
  const restoreExpense = useRestoreExpense(groupId)

  function showFailure(error: Error): void {
    console.error('Restoring an expense failed', error)
    toast.error(t(errorMessageKey(error)))
  }

  function restore(expenseId: string): void {
    restoreExpense.mutate(expenseId, {
      onSuccess: () => {
        toast.success(t('expenses.restored'))
      },
      onError: showFailure,
    })
  }

  function renderContent() {
    if (deletedQuery.isError) {
      return <GroupLoadError error={deletedQuery.error} onRetry={() => void deletedQuery.refetch()} />
    }
    if (categoriesQuery.isError) {
      return (
        <GroupLoadError
          error={categoriesQuery.error}
          onRetry={() => void categoriesQuery.refetch()}
        />
      )
    }
    if (deletedQuery.isPending || categoriesQuery.isPending) {
      return <KvitLoading />
    }
    if (deletedQuery.data.length === 0) {
      return <KvitEmpty message={t('expenses.deletedEmpty')} />
    }
    const categories = categoriesQuery.data
    return (
      <div className="flex flex-col gap-2.5">
        {deletedQuery.data.map((row) => {
          const category = categoryOf(categories, row.categoryId)
          return (
            <div
              key={row.id}
              className="flex min-h-19 items-center gap-3 rounded-2xl bg-card px-3.5 py-3 text-card-foreground"
            >
              <CategoryTile category={category} />
              <span className="flex min-w-0 flex-1 flex-col">
                <span className="font-semibold break-words">
                  {expenseTitle(row.title, category, t)} ·{' '}
                  {formatMoney(row.amountMinor, row.currency, language)}
                </span>
                <span className="text-[0.8125rem] text-muted-foreground">
                  {t('recentlyDeleted.restorableUntil', {
                    date: formatDayMonthYear(row.restorableUntil, language),
                  })}
                </span>
              </span>
              {row.canEdit && (
                <KvitButton
                  className="min-h-11 w-auto rounded-full px-4 text-[0.9375rem] font-semibold"
                  disabled={restoreExpense.isPending && restoreExpense.variables === row.id}
                  onClick={() => restore(row.id)}
                >
                  {t('recentlyDeleted.restore')}
                </KvitButton>
              )}
            </div>
          )
        })}
      </div>
    )
  }

  return (
    <KvitScreen>
      <div className="flex min-h-12">
        <KvitBackButton to={routes.group(groupId)} />
      </div>
      <KvitScreenTitle>{t('recentlyDeleted.title')}</KvitScreenTitle>
      {renderContent()}
    </KvitScreen>
  )
}
