import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import type { Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { Category } from '@/core/services/categories/categoriesService'
import type { ExpenseListRow } from '@/core/services/expenses/expensesService'
import { formatMoney } from '@/shared/utils/formatMoney'
import { CategoryTile } from '../../shared/components/CategoryTile'
import { expenseTitle } from '../../shared/expenseTitle'

interface ExpenseListItemProps {
  groupId: string
  row: ExpenseListRow
  category: Category | null
  language: Language
}

export function ExpenseListItem({ groupId, row, category, language }: ExpenseListItemProps) {
  const { t } = useTranslation()
  const paidByLine =
    row.yourShareMinor === null
      ? t('expenses.paidByNotInSplit', { name: row.paidByName })
      : t('expenses.paidByShare', {
          name: row.paidByName,
          amount: formatMoney(row.yourShareMinor, row.currency, language),
        })

  return (
    <Link
      to={routes.groupExpense(groupId, row.id)}
      className="pressable flex min-h-16 items-center gap-3 rounded-2xl bg-card px-3.5 py-2.5 text-card-foreground hover:bg-secondary-hover"
    >
      <CategoryTile category={category} />
      <span className="flex min-w-0 flex-1 flex-col">
        <span className="line-clamp-2 font-semibold break-words">{expenseTitle(row.title, category, t)}</span>
        <span className="text-[0.8125rem] text-muted-foreground">{paidByLine}</span>
      </span>
      <span className="text-right font-bold whitespace-nowrap">
        {formatMoney(row.amountMinor, row.currency, language)}
      </span>
    </Link>
  )
}
