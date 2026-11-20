import { useEffect, useId, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { balancesCardLook } from '@/features/groups/balances/balancesLooks'
import { KvitRatesSourceNote } from '@/shared/components/KvitRatesSourceNote'
import type { Currency } from '@/shared/utils/formatMoney'
import { cn } from '@/shared/utils/cn'
import { useChangeBudgetCurrency } from '../hooks/useChangeBudgetCurrency'

type BudgetTotalCurrency = Currency | null

interface BudgetTotalOption {
  value: BudgetTotalCurrency
  textKey: string
}

const saveAfterLastChoiceMilliseconds = 400

interface WaitingSave {
  timer: ReturnType<typeof setTimeout>
  currency: BudgetTotalCurrency
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
  const { mutate: saveCurrency } = changeCurrency
  const [pickedCurrency, setPickedCurrency] = useState<BudgetTotalCurrency | undefined>(undefined)
  const waitingSaveRef = useRef<WaitingSave | null>(null)
  const shownCurrency = pickedCurrency === undefined ? budgetTotalCurrency : pickedCurrency

  useEffect(
    () => () => {
      const waitingSave = waitingSaveRef.current
      if (waitingSave !== null) {
        clearTimeout(waitingSave.timer)
        saveCurrency(waitingSave.currency)
      }
    },
    [saveCurrency],
  )

  function forgetPickOnceSettled(currency: BudgetTotalCurrency): void {
    setPickedCurrency((current) => (current === currency ? undefined : current))
  }

  function save(currency: BudgetTotalCurrency): void {
    waitingSaveRef.current = null
    saveCurrency(currency, {
      onSuccess: () => forgetPickOnceSettled(currency),
      onError: () => forgetPickOnceSettled(currency),
    })
  }

  function choose(value: BudgetTotalCurrency): void {
    if (value === shownCurrency) {
      return
    }
    const waitingSave = waitingSaveRef.current
    if (waitingSave !== null) {
      clearTimeout(waitingSave.timer)
      waitingSaveRef.current = null
    }
    if (value === budgetTotalCurrency && !changeCurrency.isPending) {
      setPickedCurrency(undefined)
      return
    }
    setPickedCurrency(value)
    waitingSaveRef.current = {
      timer: setTimeout(() => save(value), saveAfterLastChoiceMilliseconds),
      currency: value,
    }
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
            className="pressable -mx-2 flex min-h-11 cursor-pointer items-center gap-3 rounded-2xl px-2 py-1.5 text-[0.9375rem] has-checked:bg-card-tint has-checked:font-semibold"
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
      <KvitRatesSourceNote />
    </section>
  )
}
