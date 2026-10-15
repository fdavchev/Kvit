import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { useDeleteSettlement } from '@/features/settlements/delete/hooks/useDeleteSettlement'
import { useRestoreSettlement } from '@/features/settlements/delete/hooks/useRestoreSettlement'
import { KvitAvatar } from '@/shared/components/KvitAvatar'
import { KvitButton } from '@/shared/components/KvitButton'
import { showUndoToast } from '@/shared/toasts/showUndoToast'
import { cn } from '@/shared/utils/cn'
import { balancesRowLook, balancesRowTextLook, balancesTextButtonLook } from '../balancesLooks'
import type { ConfirmedPaymentView } from '../balancesView'

interface PaymentRowProps {
  groupId: string
  payment: ConfirmedPaymentView
  detail: string
  canDelete: boolean
}

export function PaymentRow({ groupId, payment, detail, canDelete }: PaymentRowProps) {
  const { t } = useTranslation()
  const deleteSettlement = useDeleteSettlement(groupId, payment.settlementId)
  const restoreSettlement = useRestoreSettlement(groupId)

  function showFailure(error: Error): void {
    console.error('A payment action failed', error)
    toast.error(t(errorMessageKey(error)))
  }

  function undoDelete(): void {
    void restoreSettlement
      .mutateAsync(payment.settlementId)
      .then(() => {
        toast.success(t('settlements.restored'))
      })
      .catch(showFailure)
  }

  function deleteNow(): void {
    void deleteSettlement
      .mutateAsync()
      .then(() => {
        showUndoToast(t('settlements.deleted'), {
          danger: true,
          undoLabel: t('common.undo'),
          onUndo: undoDelete,
        })
      })
      .catch(showFailure)
  }

  return (
    <li className={cn(balancesRowLook, 'gap-2.5 rounded-2xl bg-card px-3 py-2.5 text-card-foreground')}>
      <KvitAvatar name={payment.payer.name} pictureUrl={payment.payer.pictureUrl} colorIndex={payment.payer.colorIndex} />
      <div className={balancesRowTextLook}>
        <p className="text-[0.9375rem] font-semibold break-words">
          {t('settlements.fromPaidTo', { from: payment.payer.name, to: payment.receiver.name })}
        </p>
        <p className="text-[0.8125rem] text-muted-foreground">{detail}</p>
      </div>
      {canDelete && (
        <KvitButton
          variant="smallLink"
          className={cn(balancesTextButtonLook, 'text-danger')}
          disabled={deleteSettlement.isPending}
          onClick={deleteNow}
        >
          {t('expense.delete')}
        </KvitButton>
      )}
    </li>
  )
}
