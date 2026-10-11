import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import type { Language } from '@/core/i18n/language'
import { KvitAvatar } from '@/shared/components/KvitAvatar'
import { cn } from '@/shared/utils/cn'
import { formatHowLongAgo } from '@/shared/utils/formatDate'
import { formatMoney } from '@/shared/utils/formatMoney'
import { balancesCardLook, balancesSectionHeadingLook } from '../balancesLooks'
import type { NeedsYouView } from '../balancesView'

interface NeedsYouSectionProps {
  items: readonly NeedsYouView[]
  language: Language
  timeZone: string
  now: Date
}

export function NeedsYouSection({ items, language, timeZone, now }: NeedsYouSectionProps) {
  const { t } = useTranslation()
  const headingId = useId()

  function sentenceOf(item: NeedsYouView): string {
    const amount = formatMoney(item.amountMinor, item.currency, language)
    return item.receiverName === null
      ? t('balances.saysPaidYou', { from: item.payer.name, amount })
      : t('balances.saysPaidName', { from: item.payer.name, to: item.receiverName, amount })
  }

  return (
    <section aria-labelledby={headingId}>
      <h2 id={headingId} className={balancesSectionHeadingLook}>
        {t('balances.needsYou')}
      </h2>
      <ul className="mb-3.5 flex flex-col gap-3.5">
        {items.map((item) => (
          <li key={item.settlementId} className={cn(balancesCardLook, 'flex items-center gap-3')}>
            <KvitAvatar name={item.payer.name} pictureUrl={item.payer.pictureUrl} colorIndex={item.payer.colorIndex} />
            <div className="flex min-w-0 flex-1 flex-col">
              <p className="font-bold break-words">{sentenceOf(item)}</p>
              <p className="text-[0.8125rem] text-muted-foreground">
                {formatHowLongAgo(item.createdAt, now, timeZone, language, t)}
              </p>
            </div>
          </li>
        ))}
      </ul>
    </section>
  )
}
