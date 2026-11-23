import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import type { Language } from '@/core/i18n/language'
import { KvitAvatar } from '@/shared/components/KvitAvatar'
import { cn } from '@/shared/utils/cn'
import { formatSignedMoney } from '@/shared/utils/formatSignedMoney'
import { amountLookOf, balancesCardLook, balancesSectionHeadingLook } from '../balancesLooks'
import type { EveryoneCardView } from '../balancesView'

interface EveryoneSectionProps {
  cards: readonly EveryoneCardView[]
  language: Language
}

export function EveryoneSection({ cards, language }: EveryoneSectionProps) {
  const { t } = useTranslation()
  const headingId = useId()

  return (
    <section aria-labelledby={headingId}>
      <h2 id={headingId} className={balancesSectionHeadingLook}>
        {t('balances.everyone')}
      </h2>
      {cards.map((card) => (
        <ul key={card.currency} className={cn(balancesCardLook, 'mb-3.5 flex flex-col gap-3')}>
          {card.rows.map((row) => (
            <li key={row.person.memberId} className="flex items-center gap-3">
              <KvitAvatar name={row.person.name} pictureUrl={row.person.pictureUrl} colorIndex={row.person.colorIndex} />
              <span className="flex min-w-0 flex-1 flex-wrap items-center gap-x-3">
                <span className="min-w-0 grow break-words">{row.person.name}</span>
                <span className={cn('ml-auto font-extrabold whitespace-nowrap', amountLookOf(row.balanceMinor))}>
                  {formatSignedMoney(row.balanceMinor, card.currency, language)}
                </span>
              </span>
            </li>
          ))}
        </ul>
      ))}
    </section>
  )
}
