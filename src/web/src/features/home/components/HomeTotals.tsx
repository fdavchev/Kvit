import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { DashboardAmount, DashboardTotals } from '@/core/services/dashboard/dashboardService'
import { cn } from '@/shared/utils/cn'
import { formatMoney } from '@/shared/utils/formatMoney'

interface HomeTotalsProps {
  totals: DashboardTotals
}

export function HomeTotals({ totals }: HomeTotalsProps) {
  const { t } = useTranslation()

  return (
    <div className="grid grid-cols-2 gap-2.5">
      <TotalBox label={t('home.owed')} amounts={totals.owed} boxLook="bg-positive-surface" amountLook="text-positive" />
      <TotalBox label={t('home.owe')} amounts={totals.owe} boxLook="bg-negative-surface" amountLook="text-negative" />
    </div>
  )
}

interface TotalBoxProps {
  label: string
  amounts: readonly DashboardAmount[]
  boxLook: string
  amountLook: string
}

function TotalBox({ label, amounts, boxLook, amountLook }: TotalBoxProps) {
  const { language } = useLanguage()
  const labelId = useId()

  return (
    <section
      aria-labelledby={labelId}
      className={cn('flex min-w-0 flex-col gap-0.5 rounded-[20px] px-3.5 py-3 text-foreground', boxLook)}
    >
      <p id={labelId} className="text-[0.8125rem] font-bold">
        {label}
      </p>
      {amounts.length === 0 ? (
        <p className="text-lg font-extrabold text-muted-foreground">–</p>
      ) : (
        amounts.map((amount) => (
          <p key={amount.currency} className={cn('text-lg leading-tight font-extrabold break-words', amountLook)}>
            {formatMoney(amount.amountMinor, amount.currency, language)}
          </p>
        ))
      )}
    </section>
  )
}
