import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import type { Language } from '@/core/i18n/language'
import { KvitAvatar } from '@/shared/components/KvitAvatar'
import { formatMoney } from '@/shared/utils/formatMoney'
import { balancesSectionHeadingLook } from '../balancesLooks'
import type { BetweenOthersView } from '../balancesView'

interface BetweenOthersSectionProps {
  items: readonly BetweenOthersView[]
  language: Language
}

export function BetweenOthersSection({ items, language }: BetweenOthersSectionProps) {
  const { t } = useTranslation()
  const headingId = useId()

  return (
    <section aria-labelledby={headingId}>
      <h2 id={headingId} className={balancesSectionHeadingLook}>
        {t('balances.betweenOthers')}
      </h2>
      <ul className="mb-1 flex flex-col gap-2">
        {items.map((item) => (
          <li
            key={item.key}
            className="flex items-center gap-2.5 rounded-2xl bg-card px-3 py-2.5 text-card-foreground"
          >
            <KvitAvatar name={item.payer.name} pictureUrl={item.payer.pictureUrl} colorIndex={item.payer.colorIndex} />
            <div className="flex min-w-0 flex-1 flex-col">
              <p className="text-[0.9375rem] font-semibold break-words">
                {t('balances.pays', { from: item.payer.name, to: item.receiver.name })}
              </p>
              <p className="flex flex-wrap items-center gap-x-1.5 text-[0.8125rem] text-muted-foreground">
                <span>{formatMoney(item.amountMinor, item.currency, language)}</span>
                {item.isWaiting && (
                  <span className="rounded-full bg-pill px-2 py-0.5 text-xs font-bold text-foreground">
                    {t('balances.waiting')}
                  </span>
                )}
              </p>
            </div>
          </li>
        ))}
      </ul>
    </section>
  )
}
