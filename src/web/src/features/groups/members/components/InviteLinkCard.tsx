import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { routes } from '@/core/router/routes'
import type { Group } from '@/core/services/groups/groupsService'
import { KvitButton } from '@/shared/components/KvitButton'
import { showUndoToast } from '@/shared/toasts/showUndoToast'
import { useInviteLinkActions } from '../../group/useInviteLinkActions'
import { useResetInviteLink } from '../hooks/useResetInviteLink'
import { useUndoResetInviteLink } from '../hooks/useUndoResetInviteLink'

interface InviteLinkCardProps {
  group: Group
}

export function InviteLinkCard({ group }: InviteLinkCardProps) {
  const { t } = useTranslation()
  const inviteLinkActions = useInviteLinkActions()
  const resetLink = useResetInviteLink(group.id)
  const undoResetLink = useUndoResetInviteLink(group.id)
  const shownLink = `${window.location.host}${routes.join(group.inviteToken)}`

  function showFailure(error: Error): void {
    console.error('Changing the invite link failed', error)
    toast.error(t(errorMessageKey(error)))
  }

  function undoReset(): void {
    void undoResetLink.mutateAsync().catch(showFailure)
  }

  function resetNow(): void {
    resetLink.mutate(undefined, {
      onSuccess: () => {
        showUndoToast(t('members.invite.resetDone'), {
          danger: true,
          undoLabel: t('common.undo'),
          onUndo: undoReset,
        })
      },
      onError: showFailure,
    })
  }

  return (
    <section className="flex flex-col gap-2">
      <h2 className="text-[1.0625rem] font-bold">{t('members.invite.title')}</h2>
      <div className="flex flex-col gap-3 rounded-[20px] bg-card p-[18px] text-card-foreground">
        <p className="rounded-xl border border-field-border bg-field px-3 py-2.5 text-[0.8125rem] break-all text-field-foreground">
          {shownLink}
        </p>
        <div className="flex gap-2">
          <KvitButton
            className="min-h-12 min-w-0 flex-1 shrink px-3 text-base"
            onClick={() => void inviteLinkActions.share(group.inviteToken)}
          >
            {t('members.invite.share')}
          </KvitButton>
          <KvitButton
            variant="secondary"
            className="min-h-12 min-w-0 flex-1 shrink bg-field px-3 text-base"
            onClick={() => void inviteLinkActions.copy(group.inviteToken)}
          >
            {t('members.invite.copy')}
          </KvitButton>
        </div>
        {group.isOwner && (
          <KvitButton
            variant="danger"
            className="min-h-11"
            disabled={resetLink.isPending}
            onClick={resetNow}
          >
            {t('members.invite.reset')}
          </KvitButton>
        )}
      </div>
    </section>
  )
}
