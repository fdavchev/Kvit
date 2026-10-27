import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { routes } from '@/core/router/routes'

interface BudgetSummaryRowProps {
  month: string
}

const chartEmoji = '\u{1F4CA}'

export function BudgetSummaryRow({ month }: BudgetSummaryRowProps) {
  const { t } = useTranslation()

  return (
    <Link
      to={routes.budgetSummaryOf(month)}
      className="pressable mt-3 flex min-h-11 w-full items-center gap-3 rounded-[20px] bg-card px-4 py-2.5 text-left text-card-foreground hover:bg-secondary-hover"
    >
      <span aria-hidden="true" className="text-[1.375rem] leading-none">
        {chartEmoji}
      </span>
      <span className="min-w-0 flex-1 font-semibold break-words">{t('budget.summary.row')}</span>
      <span aria-hidden="true" className="text-xl text-muted-foreground">
        ›
      </span>
    </Link>
  )
}
