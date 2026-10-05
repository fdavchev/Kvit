import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import type { GroupMember } from '@/core/services/groups/membersService'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { showUndoToast } from '@/shared/toasts/showUndoToast'
import { useLetBackIn } from '../hooks/useLetBackIn'
import { useMakeOwner } from '../hooks/useMakeOwner'
import { useRemoveMember } from '../hooks/useRemoveMember'
import { useUndoClaim } from '../hooks/useUndoClaim'

interface MemberOptionsSheetProps {
  groupId: string
  member: GroupMember
  onClose: () => void
}

export function MemberOptionsSheet({ groupId, member, onClose }: MemberOptionsSheetProps) {
  const { t } = useTranslation()
  const makeOwner = useMakeOwner(groupId)
  const undoClaim = useUndoClaim(groupId)
  const removeMember = useRemoveMember(groupId)
  const letBackIn = useLetBackIn(groupId)
  const isPending = makeOwner.isPending || undoClaim.isPending || removeMember.isPending

  function showFailure(error: Error): void {
    console.error('A member action failed', error)
    toast.error(t(errorMessageKey(error)))
  }

  function makeOwnerNow(): void {
    makeOwner.mutate(member.id, {
      onSuccess: () => {
        toast.success(t('members.ownerNow', { name: member.displayName }))
        onClose()
      },
      onError: showFailure,
    })
  }

  function undoClaimNow(claimedName: string): void {
    undoClaim.mutate(member.id, {
      onSuccess: () => {
        toast.success(
          t('members.claimUndone', { name: member.displayName, claimed: claimedName }),
        )
        onClose()
      },
      onError: showFailure,
    })
  }

  function undoRemove(): void {
    void letBackIn.mutateAsync(member.id).catch(showFailure)
  }

  function removeNow(): void {
    removeMember.mutate(member.id, {
      onSuccess: () => {
        showUndoToast(t('members.removed', { name: member.displayName }), {
          danger: true,
          undoLabel: t('common.undo'),
          onUndo: undoRemove,
        })
        onClose()
      },
      onError: showFailure,
    })
  }

  const { claimedName } = member

  return (
    <KvitSheet title={member.displayName} onClose={onClose}>
      <div className="flex flex-col gap-2">
        {!member.isNameOnly && (
          <KvitButton
            variant="secondary"
            className="bg-field"
            disabled={isPending}
            onClick={makeOwnerNow}
          >
            {t('members.makeOwner')}
          </KvitButton>
        )}
        {claimedName !== null && (
          <KvitButton
            variant="secondary"
            className="bg-field"
            disabled={isPending}
            onClick={() => undoClaimNow(claimedName)}
          >
            {t('members.undoClaim')}
          </KvitButton>
        )}
        <KvitButton variant="danger" className="bg-field" disabled={isPending} onClick={removeNow}>
          {t('members.remove')}
        </KvitButton>
        <KvitButton variant="link" onClick={onClose}>
          {t('common.close')}
        </KvitButton>
      </div>
    </KvitSheet>
  )
}
