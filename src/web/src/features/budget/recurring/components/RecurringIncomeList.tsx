import { useTranslation } from 'react-i18next'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { RecurringIncome } from '@/core/services/recurringIncomes/recurringIncomesService'
import { kvitRoundActionLooks } from '@/shared/components/kvitRoundActionLooks'
import { useListAnimation } from '@/shared/hooks/useListAnimation'
import { cn } from '@/shared/utils/cn'
import { formatExpenseDay } from '@/shared/utils/formatDate'
import { formatSignedMoney } from '@/shared/utils/formatSignedMoney'
import { recurringRhythmText } from '../recurringIncomeRhythm'

interface RecurringIncomeListProps {
  incomes: readonly RecurringIncome[]
  today: string
  isStopping: (recurringIncomeId: string) => boolean
  onEdit: (income: RecurringIncome) => void
  onStop: (income: RecurringIncome) => void
}

const blockButtonLook = cn(kvitRoundActionLooks.other, 'min-h-11 px-4 text-[0.9375rem] font-bold')

export function RecurringIncomeList({ incomes, today, isStopping, onEdit, onStop }: RecurringIncomeListProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const listRef = useListAnimation<HTMLUListElement>()

  return (
    <ul ref={listRef} aria-label={t('budget.recurring.title')} className="flex flex-col gap-2.5">
      {incomes.map((income) => (
        <li key={income.id} className="flex flex-col gap-1 rounded-[20px] bg-card px-4 py-3.5 text-card-foreground">
          <div className="flex items-baseline justify-between gap-3">
            <span className="min-w-0 text-[1.0625rem] font-bold break-words">
              {income.title ?? t('budget.income.listTitle')}
            </span>
            <span className="text-[1.0625rem] font-bold whitespace-nowrap text-positive">
              {formatSignedMoney(income.amountMinor, income.currency, language)}
            </span>
          </div>
          <p className="text-[0.875rem] text-muted-foreground">
            <span>{recurringRhythmText(income, language, t)}</span>
            {income.nextDate !== null && (
              <>
                <span aria-hidden="true"> · </span>
                <span>{t('budget.recurring.next', { day: formatExpenseDay(income.nextDate, today, language) })}</span>
              </>
            )}
          </p>
          <div className="mt-2 flex flex-wrap gap-2">
            <button type="button" className={blockButtonLook} onClick={() => onEdit(income)}>
              {t('expense.edit')}
            </button>
            <button
              type="button"
              disabled={isStopping(income.id)}
              className={cn(blockButtonLook, 'border-danger text-danger disabled:opacity-50')}
              onClick={() => onStop(income)}
            >
              {t('budget.recurring.stop')}
            </button>
          </div>
        </li>
      ))}
    </ul>
  )
}
