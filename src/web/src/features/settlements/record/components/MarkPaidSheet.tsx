import type { TFunction } from 'i18next'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessageKey } from '@/core/api/errors'
import type { GroupMember } from '@/core/services/groups/membersService'
import { useKvitSheet } from '@/shared/components/useKvitSheet'
import { moneyInputText, parseMoneyInput } from '@/shared/utils/parseMoneyInput'
import { useSavePayment } from '../hooks/useSavePayment'
import { whenItCountsText, type PaymentToRecord } from '../paymentToRecord'
import { PaymentSheet } from './PaymentSheet'

interface MarkPaidSheetProps {
  groupId: string
  members: readonly GroupMember[]
  payment: PaymentToRecord
  onClose: () => void
}

export function MarkPaidSheet({ groupId, members, payment, onClose }: MarkPaidSheetProps) {
  const { t } = useTranslation()
  const sheet = useKvitSheet()
  const savePayment = useSavePayment(groupId, members)
  const [clientRequestId] = useState<string>(() => crypto.randomUUID())
  const [amountText, setAmountText] = useState<string>(() => moneyInputText(payment.amountMinor, payment.currency))
  const amountMinor = parseMoneyInput(amountText, payment.currency)

  function save(): void {
    if (amountMinor === null) {
      throw new Error(`Save was pressed with an amount that is not valid: "${amountText}"`)
    }
    savePayment.save(
      {
        clientRequestId,
        fromMemberId: payment.payer.memberId,
        toMemberId: payment.receiver.memberId,
        amountMinor,
        currency: payment.currency,
      },
      { payerName: payment.payer.name, receiverName: payment.receiver.name },
      sheet.closeIfShown,
    )
  }

  return (
    <PaymentSheet
      title={titleOf(payment, t)}
      actionsRef={sheet.actionsRef}
      amountText={amountText}
      currency={payment.currency}
      note={whenItCountsText(payment.payer.memberId, payment.receiver.memberId, payment.receiver.name, members, t)}
      canSubmit={amountMinor !== null}
      isPending={savePayment.isPending}
      errorMessage={savePayment.error === null ? null : t(errorMessageKey(savePayment.error))}
      onAmountTextChange={setAmountText}
      onSubmit={save}
      onDismiss={sheet.close}
      onClose={onClose}
    />
  )
}

function titleOf(payment: PaymentToRecord, t: TFunction): string {
  if (payment.receiver.isYou) {
    return t('settlements.namePaidMe', { name: payment.payer.name })
  }
  if (payment.payer.isYou) {
    return t('settlements.iPaidName', { name: payment.receiver.name })
  }
  return t('settlements.fromPaidTo', { from: payment.payer.name, to: payment.receiver.name })
}
