import { useTranslation } from 'react-i18next'
import { KvitButton } from '@/shared/components/KvitButton'
import { balancesTextButtonLook } from '../balancesLooks'
import type { CancelWaitingPayment } from '../hooks/useCancelWaitingPayment'

interface CancelPaymentButtonProps {
  cancelPayment: CancelWaitingPayment
}

export function CancelPaymentButton({ cancelPayment }: CancelPaymentButtonProps) {
  const { t } = useTranslation()

  return (
    <KvitButton
      variant="underlinedLink"
      className={balancesTextButtonLook}
      disabled={cancelPayment.isPending}
      onClick={cancelPayment.cancel}
    >
      {t('settlements.cancel')}
    </KvitButton>
  )
}
