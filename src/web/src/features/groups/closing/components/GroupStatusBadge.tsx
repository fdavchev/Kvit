import { useTranslation } from 'react-i18next'
import type { GroupStatus } from '@/core/services/groups/groupsService'

interface GroupStatusBadgeProps {
  status: GroupStatus
}

export function GroupStatusBadge({ status }: GroupStatusBadgeProps) {
  const { t } = useTranslation()

  if (status === 'Open') {
    return null
  }
  return (
    <span className="rounded-full bg-pill px-2.5 py-0.5 text-xs font-bold whitespace-nowrap text-foreground">
      {t(status === 'Closing' ? 'closing.badgeClosing' : 'closing.badgeFinished')}
    </span>
  )
}
