import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { routes } from '@/core/router/routes'
import type { Group } from '@/core/services/groups/groupsService'
import { KvitButton } from '@/shared/components/KvitButton'
import { showDangerToast } from '@/shared/toasts/showUndoToast'
import { useLeaveGroup } from '../hooks/useLeaveGroup'

interface LeaveGroupSectionProps {
  group: Group
}

export function LeaveGroupSection({ group }: LeaveGroupSectionProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const leaveGroup = useLeaveGroup(group.id)

  function leaveNow(): void {
    leaveGroup.mutate(undefined, {
      onSuccess: () => {
        showDangerToast(t('groupSettings.left', { name: group.name }))
        navigate(routes.groups, { replace: true })
      },
      onError: (error) => {
        console.error('Leaving the group failed', error)
        toast.error(t(errorMessageKey(error)))
      },
    })
  }

  if (group.isOwner) {
    return (
      <div className="flex flex-col gap-2">
        <p className="text-[0.9375rem] text-pretty text-muted-foreground">
          {t('groupSettings.ownerCannotLeave')}
        </p>
        <KvitButton type="button" variant="danger" disabled>
          {t('groupSettings.leave')}
        </KvitButton>
      </div>
    )
  }
  return (
    <KvitButton
      type="button"
      variant="danger"
      disabled={leaveGroup.isPending}
      onClick={leaveNow}
    >
      {t('groupSettings.leave')}
    </KvitButton>
  )
}
