import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { routes } from '@/core/router/routes'
import type { Group } from '@/core/services/groups/groupsService'
import type {
  GroupMember,
  GroupMembers,
  RemovedMember,
} from '@/core/services/groups/membersService'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitError } from '@/shared/components/KvitError'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'
import { AddNameSheet } from '../../group/components/AddNameSheet'
import { useGroup } from '../../group/hooks/useGroup'
import { LeaveGroupSection } from '../../settings/components/LeaveGroupSection'
import { useGroupIdParam } from '../../shared/useGroupIdParam'
import { useClaimName } from '../hooks/useClaimName'
import { useLetBackIn } from '../hooks/useLetBackIn'
import { useMembers } from '../hooks/useMembers'
import { InviteLinkCard } from './InviteLinkCard'
import { MemberListItem } from './MemberListItem'
import { MemberOptionsSheet } from './MemberOptionsSheet'
import { RemovedMembers } from './RemovedMembers'

export function MembersScreen() {
  const { t } = useTranslation()
  const groupId = useGroupIdParam()
  const groupQuery = useGroup(groupId)
  const membersQuery = useMembers(groupId)
  const claimName = useClaimName(groupId)
  const letBackIn = useLetBackIn(groupId)
  const [isAddingName, setIsAddingName] = useState<boolean>(false)
  const [optionsMember, setOptionsMember] = useState<GroupMember | null>(null)

  function showFailure(error: Error): void {
    console.error('A member action failed', error)
    toast.error(t(errorMessageKey(error)))
  }

  function claim(member: GroupMember): void {
    claimName.mutate(member.id, {
      onSuccess: () => {
        toast.success(t('members.claimed', { name: member.displayName }))
      },
      onError: showFailure,
    })
  }

  function letBackInNow(member: RemovedMember): void {
    letBackIn.mutate(member.id, {
      onSuccess: () => {
        toast.success(t('members.backInGroup', { name: member.displayName }))
      },
      onError: showFailure,
    })
  }

  function retry(): void {
    void groupQuery.refetch()
    void membersQuery.refetch()
  }

  function renderMembers(group: Group, groupMembers: GroupMembers) {
    return (
      <div className="flex flex-col gap-6">
        <div className="flex flex-col gap-1">
          <ul className="flex flex-col gap-2.5">
            {groupMembers.members.map((member) => (
              <MemberListItem
                key={member.id}
                member={member}
                showsTakenName={group.isOwner}
                canClaim={groupMembers.canClaimNames && member.isNameOnly}
                isClaimPending={claimName.isPending}
                hasOptions={group.isOwner && !member.isOwner}
                onClaim={() => claim(member)}
                onOpenOptions={() => setOptionsMember(member)}
              />
            ))}
          </ul>
          <button
            type="button"
            onClick={() => setIsAddingName(true)}
            className="pressable inline-flex min-h-11 items-center self-start rounded-xl px-3.5 text-[0.9375rem] font-semibold text-link hover:underline"
          >
            {t('members.addNameLink')}
          </button>
        </div>
        <InviteLinkCard group={group} />
        {group.isOwner && groupMembers.removed.length > 0 && (
          <RemovedMembers
            removed={groupMembers.removed}
            pendingMemberId={letBackIn.isPending ? letBackIn.variables : null}
            onLetBackIn={letBackInNow}
          />
        )}
        {!group.isOwner && <LeaveGroupSection group={group} />}
        {isAddingName && (
          <AddNameSheet groupId={group.id} onClose={() => setIsAddingName(false)} />
        )}
        {optionsMember !== null && (
          <MemberOptionsSheet
            groupId={group.id}
            member={optionsMember}
            onClose={() => setOptionsMember(null)}
          />
        )}
      </div>
    )
  }

  function renderContent() {
    if (groupQuery.isError || membersQuery.isError) {
      const error = groupQuery.isError ? groupQuery.error : membersQuery.error
      return <KvitError message={t(errorMessageKey(error))} onRetry={retry} />
    }
    if (groupQuery.isPending || membersQuery.isPending) {
      return <KvitLoading />
    }
    return renderMembers(groupQuery.data, membersQuery.data)
  }

  return (
    <KvitScreen>
      <div className="flex min-h-12">
        <KvitBackButton to={routes.group(groupId)} />
      </div>
      <KvitScreenTitle>{t('members.title')}</KvitScreenTitle>
      {renderContent()}
    </KvitScreen>
  )
}
