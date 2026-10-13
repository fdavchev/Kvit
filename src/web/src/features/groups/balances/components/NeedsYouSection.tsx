import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import type { Language } from '@/core/i18n/language'
import { formatHowLongAgo } from '@/shared/utils/formatDate'
import { formatMoney } from '@/shared/utils/formatMoney'
import { balancesSectionHeadingLook } from '../balancesLooks'
import type { NeedsYouView } from '../balancesView'
import { NeedsYouCard } from './NeedsYouCard'

interface NeedsYouSectionProps {
  groupId: string
  items: readonly NeedsYouView[]
  language: Language
  timeZone: string
  now: Date
}

export function NeedsYouSection({ groupId, items, language, timeZone, now }: NeedsYouSectionProps) {
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
          <NeedsYouCard
            key={item.settlementId}
            groupId={groupId}
            item={item}
            sentence={sentenceOf(item)}
            howLongAgo={formatHowLongAgo(item.createdAt, now, timeZone, language, t)}
          />
        ))}
      </ul>
    </section>
  )
}
