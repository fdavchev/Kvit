import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { PersonalCurrencyTotal } from '@/core/services/personalEntries/personalEntriesService'
import { balancesCardLook } from '@/features/groups/balances/balancesLooks'
import { KvitAmount } from '@/shared/components/KvitAmount'
import { cn } from '@/shared/utils/cn'
import { moneyParts } from '@/shared/utils/formatMoney'

interface BudgetTotalCardProps {
  totals: readonly PersonalCurrencyTotal[]
}

const mainAmountLook = 'text-[2.25rem] leading-[1.1] font-extrabold tracking-[-0.02em]'
const secondAmountLook = 'text-lg leading-tight font-bold text-muted-foreground'

export function BudgetTotalCard({ totals }: BudgetTotalCardProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const labelId = useId()

  return (
    <section
      aria-labelledby={labelId}
      className={cn(balancesCardLook, 'mt-5 flex flex-col items-center gap-1 py-4 text-center')}
    >
      <p id={labelId} className="text-[0.8125rem] font-bold text-muted-foreground">
        {t('budget.screen.spentThisMonth')}
      </p>
      {totals.map((total, index) => (
        <p
          key={total.currency}
          data-total-amount={index === 0 ? 'main' : 'second'}
          className={cn('max-w-full break-words', index === 0 ? mainAmountLook : secondAmountLook)}
        >
          <KvitAmount parts={moneyParts(total.totalMinor, total.currency, language)} />
        </p>
      ))}
    </section>
  )
}
