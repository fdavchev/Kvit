import type { Language } from '@/core/i18n/language'
import type { Category } from '@/core/services/categories/categoriesService'
import type { ExpenseListRow } from '@/core/services/expenses/expensesService'
import { useListAnimation } from '@/shared/hooks/useListAnimation'
import { categoryOf } from '../../shared/expenseCategories'
import { ExpenseListItem } from './ExpenseListItem'

interface ExpenseDaySectionProps {
  groupId: string
  label: string
  rows: readonly ExpenseListRow[]
  categories: readonly Category[]
  language: Language
}

export function ExpenseDaySection({ groupId, label, rows, categories, language }: ExpenseDaySectionProps) {
  const rowsRef = useListAnimation<HTMLElement>()

  return (
    <section ref={rowsRef} className="flex flex-col gap-2">
      <h2 className="mx-1 mt-3.5 text-[0.8125rem] font-bold tracking-[0.04em] text-muted-foreground uppercase">
        {label}
      </h2>
      {rows.map((row) => (
        <ExpenseListItem
          key={row.id}
          groupId={groupId}
          row={row}
          category={categoryOf(categories, row.categoryId)}
          language={language}
        />
      ))}
    </section>
  )
}
