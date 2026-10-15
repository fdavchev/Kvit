import { useTranslation } from 'react-i18next'
import { errorMessageKey } from '@/core/api/errors'
import { isGroupStateChangedError } from '@/core/api/queryClient'
import type { DashboardClosingItem } from '@/core/services/dashboard/dashboardService'
import { balancesActionButtonLook } from '@/features/groups/balances/balancesLooks'
import { useConfirmClosing } from '@/features/groups/closing/hooks/useConfirmClosing'
import { useToastWhenGroupStateChanged } from '@/features/groups/closing/hooks/useToastWhenGroupStateChanged'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitInlineError } from '@/shared/components/KvitInlineError'
import { kvitGlassButtonLook, kvitGlassDangerButtonLook } from '@/shared/components/kvitGlassLooks'
import { cn } from '@/shared/utils/cn'
import { needsYouCardLook } from '../homeLooks'
import { NeedsYouCardText } from './NeedsYouCardText'

interface HomeClosingCardProps {
  item: DashboardClosingItem
  onObject: () => void
}

export function HomeClosingCard({ item, onObject }: HomeClosingCardProps) {
  const { t } = useTranslation()
  const confirmClosing = useConfirmClosing(item.groupId)
  const toastWhenGroupStateChanged = useToastWhenGroupStateChanged()

  return (
    <li className={needsYouCardLook}>
      <NeedsYouCardText
        groupId={item.groupId}
        sentence={t('closing.prompt', { name: item.startedByName, group: item.groupName })}
      />
      <div className="flex gap-2.5">
        <KvitButton
          type="button"
          className={cn(balancesActionButtonLook, kvitGlassDangerButtonLook, 'flex-1')}
          disabled={confirmClosing.isPending}
          onClick={onObject}
        >
          {t('closing.object')}
        </KvitButton>
        <KvitButton
          type="button"
          className={cn(balancesActionButtonLook, kvitGlassButtonLook, 'flex-1')}
          disabled={confirmClosing.isPending}
          onClick={() => confirmClosing.mutate(undefined, { onError: toastWhenGroupStateChanged })}
        >
          {t('settlements.confirm')}
        </KvitButton>
      </div>
      {confirmClosing.isError && !isGroupStateChangedError(confirmClosing.error) && (
        <KvitInlineError message={t(errorMessageKey(confirmClosing.error))} />
      )}
    </li>
  )
}
