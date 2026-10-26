import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { useSaveOnce } from '@/features/budget/categories/hooks/useSaveOnce'
import { balancesCardLook } from '@/features/groups/balances/balancesLooks'
import type { Currency } from '@/shared/utils/formatMoney'
import { cn } from '@/shared/utils/cn'
import { useChangeBudgetCurrency } from '../hooks/useChangeBudgetCurrency'

type BudgetTotalCurrency = Currency | null

interface BudgetTotalOption {
  value: BudgetTotalCurrency
  textKey: string
}

const options: readonly BudgetTotalOption[] = [
  { value: null, textKey: 'settings.budget.apart' },
  { value: 'MKD', textKey: 'settings.budget.inMkd' },
  { value: 'EUR', textKey: 'settings.budget.inEur' },
]

interface BudgetTotalSettingsProps {
  budgetTotalCurrency: BudgetTotalCurrency
}

export function BudgetTotalSettings({ budgetTotalCurrency }: BudgetTotalSettingsProps) {
  const { t } = useTranslation()
  const labelId = useId()
  const optionName = useId()
  const changeCurrency = useChangeBudgetCurrency()
  const oneAtATime = useSaveOnce()
  const shownCurrency = changeCurrency.isPending ? changeCurrency.variables : budgetTotalCurrency

  function choose(value: BudgetTotalCurrency): void {
    if (value === shownCurrency || !oneAtATime.tryStart()) {
      return
    }
    changeCurrency.mutate(value, {
      onError: (error) => {
        console.error('Changing the budget total currency failed', error)
        toast.error(t(errorMessageKey(error)))
      },
      onSettled: oneAtATime.finish,
    })
  }

  return (
    <section className={cn(balancesCardLook, 'mb-8 flex flex-col gap-2.5')}>
      <h2 className="text-[1.0625rem] font-bold">{t('settings.budget.title')}</h2>
      <div role="radiogroup" aria-labelledby={labelId} className="flex flex-col gap-1">
        <span id={labelId} className="text-[0.9375rem] font-semibold">
          {t('settings.budget.showOneTotal')}
        </span>
        {options.map((option) => (
          <label
            key={option.textKey}
            className="pressable -mx-2 flex min-h-11 cursor-pointer items-center gap-3 rounded-2xl px-2 py-1.5 text-[0.9375rem] has-checked:bg-background has-checked:font-semibold"
          >
            <input
              type="radio"
              name={optionName}
              checked={option.value === shownCurrency}
              onChange={() => choose(option.value)}
              className="size-5 shrink-0 cursor-pointer accent-primary"
            />
            <span className="text-pretty">{t(option.textKey)}</span>
          </label>
        ))}
      </div>
      <p className="text-[0.875rem] text-pretty text-muted-foreground">{t('settings.budget.hint')}</p>
    </section>
  )
}
