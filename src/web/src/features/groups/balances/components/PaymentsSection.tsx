import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import type { Language } from '@/core/i18n/language'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { WaitingRow } from '@/features/outbox/components/WaitingRow'
import { amountOfItem, displayTextOf, paymentTitleOf } from '@/features/outbox/waitingItemDisplay'
import { KvitAvatar } from '@/shared/components/KvitAvatar'
import { useListAnimation } from '@/shared/hooks/useListAnimation'
import { formatExpenseDay, todayInTimeZone } from '@/shared/utils/formatDate'
import { formatMoney } from '@/shared/utils/formatMoney'
import { balancesSectionHeadingLook } from '../balancesLooks'
import type { ConfirmedPaymentView } from '../balancesView'
import { PaymentRow } from './PaymentRow'

interface PaymentsSectionProps {
  groupId: string
  payments: readonly ConfirmedPaymentView[]
  waitingPayments: readonly OutboxItem[]
  language: Language
  timeZone: string
  now: Date
  canDelete: boolean
  onOpenWaiting: (item: OutboxItem) => void
}

export function PaymentsSection({
  groupId,
  payments,
  waitingPayments,
  language,
  timeZone,
  now,
  canDelete,
  onOpenWaiting,
}: PaymentsSectionProps) {
  const { t } = useTranslation()
  const headingId = useId()
  const listRef = useListAnimation<HTMLUListElement>()
  const today = todayInTimeZone(timeZone, now)

  function detailOf(payment: ConfirmedPaymentView): string {
    const amount = formatMoney(payment.amountMinor, payment.currency, language)
    const day = formatExpenseDay(todayInTimeZone(timeZone, new Date(payment.createdAt)), today, language)
    return `${amount} · ${day}`
  }

  return (
    <section aria-labelledby={headingId}>
      <h2 id={headingId} className={balancesSectionHeadingLook}>
        {t('settlements.payments')}
      </h2>
      <ul ref={listRef} className="mb-3.5 flex flex-col gap-2">
        {waitingPayments.map((item) => {
          const { amountMinor, currency } = amountOfItem(item)
          return (
            <li key={item.id}>
              <WaitingRow
                title={paymentTitleOf(item, t)}
                amountMinor={amountMinor}
                currency={currency}
                showsRateNote={false}
                leading={<KvitAvatar name={displayTextOf(item, 'payerName')} pictureUrl={null} colorIndex="brand" />}
                onOpen={() => onOpenWaiting(item)}
              />
            </li>
          )
        })}
        {payments.map((payment) => (
          <PaymentRow
            key={payment.settlementId}
            groupId={groupId}
            payment={payment}
            detail={detailOf(payment)}
            canDelete={canDelete && payment.canDelete}
          />
        ))}
      </ul>
    </section>
  )
}
