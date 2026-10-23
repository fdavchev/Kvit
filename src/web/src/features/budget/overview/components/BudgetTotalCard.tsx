import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import { useLanguage } from '@/core/i18n/useLanguage'
import type {
  PersonalCurrencyTotal,
  PersonalIncomeTotal,
} from '@/core/services/personalEntries/personalEntriesService'
import { balancesCardLook } from '@/features/groups/balances/balancesLooks'
import { KvitFitAmount, KvitFitRollingAmount } from '@/shared/components/KvitFitAmount'
import { cn } from '@/shared/utils/cn'
import { moneyParts, type Currency } from '@/shared/utils/formatMoney'
import { signedMoneyParts } from '@/shared/utils/formatSignedMoney'

interface BudgetTotalCardProps {
  totals: readonly PersonalCurrencyTotal[]
  incomeTotals: readonly PersonalIncomeTotal[]
}

const mainAmountLook = 'text-[2.25rem] leading-[1.1] font-extrabold tracking-[-0.02em]'
const secondAmountLook = 'text-lg leading-tight font-bold text-muted-foreground'
const labelLook = 'text-[0.8125rem] font-bold text-muted-foreground'

export function BudgetTotalCard({ totals, incomeTotals }: BudgetTotalCardProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const labelId = useId()

  if (incomeTotals.length === 0) {
    return (
      <section
        aria-labelledby={labelId}
        className={cn(balancesCardLook, 'mt-5 flex flex-col items-center gap-1 py-4 text-center')}
      >
        <p id={labelId} className={labelLook}>
          {t('budget.screen.spentThisMonth')}
        </p>
        {totals.map((total, index) => (
          <KvitFitRollingAmount
            key={total.currency}
            parts={moneyParts(total.totalMinor, total.currency, language)}
            data-total-amount={index === 0 ? 'main' : 'second'}
            className={index === 0 ? mainAmountLook : secondAmountLook}
          />
        ))}
      </section>
    )
  }

  const spentIn = (currency: Currency): number =>
    totals.find((total) => total.currency === currency)?.totalMinor ?? 0
  const spendingWithoutIncome = totals.filter(
    (total) => !incomeTotals.some((income) => income.currency === total.currency),
  )

  return (
    <section
      aria-labelledby={labelId}
      className={cn(balancesCardLook, 'mt-5 flex flex-col items-center gap-1 py-4 text-center')}
    >
      <p id={labelId} className={labelLook}>
        {t('budget.screen.leftThisMonth')}
      </p>
      {incomeTotals.map((income, index) => {
        const spentMinor = spentIn(income.currency)
        const leftMinor = income.totalMinor - spentMinor
        return (
          <div
            key={income.currency}
            className={cn('flex w-full flex-col items-center gap-1', index > 0 && 'mt-2 border-t border-field-border pt-3')}
          >
            <KvitFitRollingAmount
              parts={moneyParts(leftMinor, income.currency, language)}
              data-total-amount={index === 0 ? 'main' : 'second'}
              className={cn(index === 0 ? mainAmountLook : secondAmountLook, leftMinor < 0 && 'text-negative')}
            />
            <div className="mt-1 grid w-full grid-cols-2 gap-2">
              <p className="flex min-w-0 flex-col">
                <span className={labelLook}>{t('budget.screen.income')}</span>
                <KvitFitAmount
                  parts={signedMoneyParts(income.totalMinor, income.currency, language)}
                  data-income-amount=""
                  className="font-bold text-positive"
                />
              </p>
              <p className="flex min-w-0 flex-col">
                <span className={labelLook}>{t('budget.screen.spent')}</span>
                <KvitFitAmount
                  parts={moneyParts(spentMinor, income.currency, language)}
                  data-spent-amount=""
                  className="font-bold"
                />
              </p>
            </div>
          </div>
        )
      })}
      {spendingWithoutIncome.map((total) => (
        <p key={total.currency} className="mt-2 flex w-full flex-col items-center border-t border-field-border pt-3">
          <span className={labelLook}>{t('budget.screen.spent')}</span>
          <KvitFitAmount
            parts={moneyParts(total.totalMinor, total.currency, language)}
            data-spent-amount=""
            className={secondAmountLook}
          />
        </p>
      ))}
    </section>
  )
}
