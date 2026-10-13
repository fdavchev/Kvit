import { useTranslation } from 'react-i18next'
import { KvitButton } from '@/shared/components/KvitButton'
import { kvitGlassButtonLook } from '@/shared/components/kvitGlassLooks'
import { cn } from '@/shared/utils/cn'
import { balancesActionButtonLook } from '../balancesLooks'

interface MarkPaidButtonProps {
  onClick: () => void
}

export function MarkPaidButton({ onClick }: MarkPaidButtonProps) {
  const { t } = useTranslation()

  return (
    <KvitButton className={cn(balancesActionButtonLook, kvitGlassButtonLook)} onClick={onClick}>
      {t('settlements.markPaid')}
    </KvitButton>
  )
}
