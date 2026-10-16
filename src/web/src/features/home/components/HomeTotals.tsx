import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { DashboardAmount, DashboardTotals } from '@/core/services/dashboard/dashboardService'
import { KvitAmount } from '@/shared/components/KvitAmount'
import { cn } from '@/shared/utils/cn'
import { moneyParts } from '@/shared/utils/formatMoney'

const desktopAmountLook = 'lg:text-[clamp(2.25rem,5vw,4.25rem)] lg:leading-[1.05] lg:font-black lg:tracking-[-0.02em]'

interface HomeTotalsProps {
  totals: DashboardTotals
}

export function HomeTotals({ totals }: HomeTotalsProps) {
  const { t } = useTranslation()

  return (
    <div className="grid grid-cols-2 gap-2.5 lg:flex lg:flex-wrap lg:items-start lg:gap-x-[clamp(2rem,5vw,4rem)] lg:gap-y-4">
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
      className={cn(
        'flex min-w-0 flex-col gap-0.5 rounded-[20px] px-3.5 py-3 text-foreground lg:gap-1 lg:bg-transparent lg:p-0',
        boxLook,
      )}
    >
      <p id={labelId} className="text-[0.8125rem] font-bold lg:text-[clamp(0.875rem,1.3vw,1.0625rem)]">
        {label}
      </p>
      {amounts.length === 0 ? (
        <p className={cn('text-lg font-extrabold text-muted-foreground', desktopAmountLook)}>–</p>
      ) : (
        amounts.map((amount) => (
          <p
            key={amount.currency}
            className={cn('text-lg leading-tight font-extrabold break-words', desktopAmountLook, amountLook)}
          >
            <KvitAmount parts={moneyParts(amount.amountMinor, amount.currency, language)} />
          </p>
        ))
      )}
    </section>
  )
}
