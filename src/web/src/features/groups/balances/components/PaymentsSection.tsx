import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import type { Language } from '@/core/i18n/language'
import { useListAnimation } from '@/shared/hooks/useListAnimation'
import { formatExpenseDay, todayInTimeZone } from '@/shared/utils/formatDate'
import { formatMoney } from '@/shared/utils/formatMoney'
import { balancesSectionHeadingLook } from '../balancesLooks'
import type { ConfirmedPaymentView } from '../balancesView'
import { PaymentRow } from './PaymentRow'

interface PaymentsSectionProps {
  groupId: string
  payments: readonly ConfirmedPaymentView[]
  language: Language
  timeZone: string
  now: Date
  canDelete: boolean
}

export function PaymentsSection({ groupId, payments, language, timeZone, now, canDelete }: PaymentsSectionProps) {
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
