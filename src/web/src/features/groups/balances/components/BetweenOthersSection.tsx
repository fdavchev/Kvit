import { useId, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessageKey } from '@/core/api/errors'
import type { Language } from '@/core/i18n/language'
import type { PaymentToRecord } from '@/features/settlements/record/paymentToRecord'
import { KvitAvatar } from '@/shared/components/KvitAvatar'
import { KvitInlineError } from '@/shared/components/KvitInlineError'
import { useListAnimation } from '@/shared/hooks/useListAnimation'
import { formatMoney } from '@/shared/utils/formatMoney'
import { balancesSectionHeadingLook } from '../balancesLooks'
import type { BetweenOthersView } from '../balancesView'
import { useCancelWaitingPayment } from '../hooks/useCancelWaitingPayment'
import { CancelPaymentButton } from './CancelPaymentButton'
import { MarkPaidButton } from './MarkPaidButton'

interface BetweenOthersSectionProps {
  groupId: string
  items: readonly BetweenOthersView[]
  language: Language
  canChangePayments: boolean
  onMarkPaid: (payment: PaymentToRecord) => void
}

export function BetweenOthersSection({ groupId, items, language, canChangePayments, onMarkPaid }: BetweenOthersSectionProps) {
  const { t } = useTranslation()
  const headingId = useId()
  const listRef = useListAnimation<HTMLUListElement>()

  function renderRow(item: BetweenOthersView) {
    if (canChangePayments && item.cancellableSettlementId !== null) {
      return (
        <CancellableRow
          key={item.key}
          groupId={groupId}
          settlementId={item.cancellableSettlementId}
          item={item}
          language={language}
        />
      )
    }
    return (
      <RowFrame
        key={item.key}
        item={item}
        language={language}
        action={
          canChangePayments &&
          item.canMarkPaid && (
            <MarkPaidButton
              onClick={() =>
                onMarkPaid({
                  payer: item.payer,
                  receiver: item.receiver,
                  amountMinor: item.amountMinor,
                  currency: item.currency,
                })
              }
            />
          )
        }
        error={null}
      />
    )
  }

  return (
    <section aria-labelledby={headingId}>
      <h2 id={headingId} className={balancesSectionHeadingLook}>
        {t('balances.betweenOthers')}
      </h2>
      <ul ref={listRef} className="mb-1 flex flex-col gap-2">
        {items.map(renderRow)}
      </ul>
    </section>
  )
}

interface CancellableRowProps {
  groupId: string
  settlementId: string
  item: BetweenOthersView
  language: Language
}

function CancellableRow({ groupId, settlementId, item, language }: CancellableRowProps) {
  const { t } = useTranslation()
  const cancelPayment = useCancelWaitingPayment(groupId, settlementId)

  return (
    <RowFrame
      item={item}
      language={language}
      action={<CancelPaymentButton cancelPayment={cancelPayment} />}
      error={cancelPayment.error === null ? null : <KvitInlineError message={t(errorMessageKey(cancelPayment.error))} />}
    />
  )
}

interface RowFrameProps {
  item: BetweenOthersView
  language: Language
  action: ReactNode
  error: ReactNode
}

function RowFrame({ item, language, action, error }: RowFrameProps) {
  const { t } = useTranslation()

  return (
    <li className="flex items-center gap-2.5 rounded-2xl bg-card px-3 py-2.5 text-card-foreground">
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
        {error}
      </div>
      {action}
    </li>
  )
}
