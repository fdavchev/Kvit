import { useTranslation } from 'react-i18next'
import { errorMessageKey } from '@/core/api/errors'
import type { ExpensePerson } from '@/features/expenses/shared/expensePeople'
import { useCancelSettlement } from '@/features/settlements/pending/hooks/useCancelSettlement'
import { KvitAvatar } from '@/shared/components/KvitAvatar'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitInlineError } from '@/shared/components/KvitInlineError'
import { balancesTextButtonLook } from '../balancesLooks'

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
  const cancelSettlement = useCancelSettlement(groupId, settlementId)

  return (
    <li className="flex flex-col gap-2 border-t border-field-border pt-2.5">
      <div className="flex items-center gap-3">
        <KvitAvatar name={receiver.name} pictureUrl={receiver.pictureUrl} colorIndex={receiver.colorIndex} />
        <div className="flex min-w-0 flex-1 flex-col">
          <p className="font-bold break-words">{title}</p>
          <p className="text-[0.8125rem] text-muted-foreground">{detail}</p>
        </div>
        {canCancel && (
          <KvitButton
            variant="underlinedLink"
            className={balancesTextButtonLook}
            disabled={cancelSettlement.isPending}
            onClick={() => cancelSettlement.mutate()}
          >
            {t('settlements.cancel')}
          </KvitButton>
        )}
      </div>
      {cancelSettlement.isError && <KvitInlineError message={t(errorMessageKey(cancelSettlement.error))} />}
    </li>
  )
}
