import { useTranslation } from 'react-i18next'
import { errorMessageKey } from '@/core/api/errors'
import type { ExpensePerson } from '@/features/expenses/shared/expensePeople'
import { KvitAvatar } from '@/shared/components/KvitAvatar'
import { KvitInlineError } from '@/shared/components/KvitInlineError'
import { cn } from '@/shared/utils/cn'
import { balancesRowLook, balancesRowTextLook } from '../balancesLooks'
import { useCancelWaitingPayment } from '../hooks/useCancelWaitingPayment'
import { CancelPaymentButton } from './CancelPaymentButton'

interface WaitingPaymentRowProps {
  groupId: string
  settlementId: string
  receiver: ExpensePerson
  title: string
  detail: string
  canCancel: boolean
}

export function WaitingPaymentRow({
  groupId,
  settlementId,
  receiver,
  title,
  detail,
  canCancel,
}: WaitingPaymentRowProps) {
  const { t } = useTranslation()
  const cancelPayment = useCancelWaitingPayment(groupId, settlementId)

  return (
    <li className="flex flex-col gap-2 border-t border-field-border pt-2.5">
      <div className={cn(balancesRowLook, 'gap-3')}>
        <KvitAvatar name={receiver.name} pictureUrl={receiver.pictureUrl} colorIndex={receiver.colorIndex} />
        <div className={balancesRowTextLook}>
          <p className="font-bold break-words">{title}</p>
          <p className="text-[0.8125rem] text-muted-foreground">{detail}</p>
        </div>
        {canCancel && <CancelPaymentButton cancelPayment={cancelPayment} />}
      </div>
      {cancelPayment.error !== null && <KvitInlineError message={t(errorMessageKey(cancelPayment.error))} />}
    </li>
  )
}
