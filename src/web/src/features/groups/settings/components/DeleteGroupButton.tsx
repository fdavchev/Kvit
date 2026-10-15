import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { routes } from '@/core/router/routes'
import type { Group } from '@/core/services/groups/groupsService'
import { KvitButton } from '@/shared/components/KvitButton'
import { showUndoToast } from '@/shared/toasts/showUndoToast'
import { useRestoreGroup } from '../../list/hooks/useRestoreGroup'
import { useDeleteGroup } from '../hooks/useDeleteGroup'

interface DeleteGroupButtonProps {
  group: Group
}

export function DeleteGroupButton({ group }: DeleteGroupButtonProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const deleteGroup = useDeleteGroup(group.id)
  const restoreGroup = useRestoreGroup()

  function showFailure(error: Error): void {
    console.error('A group settings action failed', error)
    toast.error(t(errorMessageKey(error)))
  }

  function undoDelete(): void {
    void restoreGroup.mutateAsync(group.id).catch(showFailure)
  }

  function deleteNow(): void {
    deleteGroup.mutate(undefined, {
      onSuccess: () => {
        showUndoToast(t('groupSettings.deleted'), {
          danger: true,
          undoLabel: t('common.undo'),
          onUndo: undoDelete,
        })
        navigate(routes.groups, { replace: true })
      },
      onError: showFailure,
    })
  }

  return (
    <KvitButton type="button" variant="danger" disabled={deleteGroup.isPending} onClick={deleteNow}>
      {t('groupSettings.delete')}
    </KvitButton>
  )
}
