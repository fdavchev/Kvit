import { useTranslation } from 'react-i18next'
import { countedText } from '@/core/i18n/pluralForm'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { RecurringIncome } from '@/core/services/recurringIncomes/recurringIncomesService'
import { KvitButton } from '@/shared/components/KvitButton'
import { cn } from '@/shared/utils/cn'

interface RecurringIncomeRowProps {
  incomes: readonly RecurringIncome[]
  onOpen: () => void
}

interface RecurringIncomeRowErrorProps {
  onRetry: () => void
}

const namedIncomeCount = 2
const repeatEmoji = '\u{1F501}'
const rowLook = 'mt-3 flex min-h-16 w-full items-center gap-3 rounded-[20px] bg-card px-4 py-3 text-left text-card-foreground'

export function RecurringIncomeRow({ incomes, onOpen }: RecurringIncomeRowProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const names = incomes.slice(0, namedIncomeCount).map((income) => income.title ?? t('budget.income.listTitle'))
  const hiddenCount = incomes.length - names.length
  const hint =
    hiddenCount === 0
      ? names.join(', ')
      : `${names.join(', ')} ${countedText(t, 'budget.recurring.more', hiddenCount, language)}`

  return (
    <button type="button" className={cn('pressable hover:bg-secondary-hover', rowLook)} onClick={onOpen}>
      <span aria-hidden="true" className="text-[1.375rem] leading-none">
        {repeatEmoji}
      </span>
      <span className="flex min-w-0 flex-1 flex-col">
        <span className="font-semibold break-words">
          {incomes.length === 0
            ? t('budget.recurring.rowNone')
            : t('budget.recurring.rowCount', { replace: { count: incomes.length } })}
        </span>
        {incomes.length > 0 && (
          <span className="line-clamp-2 text-[0.8125rem] break-words text-muted-foreground">{hint}</span>
        )}
      </span>
      <span aria-hidden="true" className="text-xl text-muted-foreground">
        ›
      </span>
    </button>
  )
}

export function RecurringIncomeRowError({ onRetry }: RecurringIncomeRowErrorProps) {
  const { t } = useTranslation()

  return (
    <div className={rowLook}>
      <span aria-hidden="true" className="text-[1.375rem] leading-none">
        {repeatEmoji}
      </span>
      <span className="min-w-0 flex-1 font-semibold break-words">{t('budget.recurring.title')}</span>
      <KvitButton variant="smallLink" className="w-auto px-2" onClick={onRetry}>
        {t('common.retry')}
      </KvitButton>
    </div>
  )
}
