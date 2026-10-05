import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { routes } from '@/core/router/routes'
import type { Group } from '@/core/services/groups/groupsService'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitEmojiTile } from '@/shared/components/KvitEmojiTile'
import { KvitError } from '@/shared/components/KvitError'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { kvitChipLook } from '@/shared/components/kvitChipLook'
import { GroupSizeLine } from '../../shared/components/GroupSizeLine'
import { useGroupIdParam } from '../../shared/useGroupIdParam'
import { useGroup } from '../hooks/useGroup'
import { shareInviteLink } from '../shareInviteLink'
import { AddNameSheet } from './AddNameSheet'
import { AddPeopleCard } from './AddPeopleCard'

export function GroupScreen() {
  const { t } = useTranslation()
  const groupId = useGroupIdParam()
  const groupQuery = useGroup(groupId)
  const [isAddingName, setIsAddingName] = useState<boolean>(false)

  async function shareLink(group: Group): Promise<void> {
    try {
      const outcome = await shareInviteLink(group.inviteToken)
      if (outcome === 'copied') {
        toast.success(t('group.linkCopied'))
      }
    } catch (error) {
      console.error('Sharing the invite link failed', error)
      toast.error(t('errors.generic'))
    }
  }

  function renderContent() {
    if (groupQuery.isPending) {
      return <KvitLoading />
    }
    if (groupQuery.isError) {
      return (
        <KvitError
          message={t(errorMessageKey(groupQuery.error))}
          onRetry={() => void groupQuery.refetch()}
        />
      )
    }
    const group = groupQuery.data
    return (
      <>
        <div className="flex flex-col items-center gap-2.5 pt-2 pb-3 text-center">
          <KvitEmojiTile emoji={group.emoji} size="large" />
          <h1 className="text-[1.75rem] leading-[1.15] font-extrabold tracking-[-0.02em] text-balance">
            {group.name}
          </h1>
          <GroupSizeLine memberCount={group.memberCount} currency={group.defaultCurrency} />
        </div>
        <div className="flex justify-center pb-4">
          <Link to={routes.groupSettings(group.id)} className={kvitChipLook}>
            {t('group.settings')}
          </Link>
        </div>
        {group.memberCount === 1 && (
          <AddPeopleCard
            onAddName={() => setIsAddingName(true)}
            onShareLink={() => void shareLink(group)}
          />
        )}
        <p className="pt-10 text-center text-[0.9375rem] text-muted-foreground">
          {t('group.expensesSoon')}
        </p>
        {isAddingName && (
          <AddNameSheet groupId={group.id} onClose={() => setIsAddingName(false)} />
        )}
      </>
    )
  }

  return (
    <KvitScreen>
      <div className="flex min-h-12">
        <KvitBackButton to={routes.groups} />
      </div>
      {renderContent()}
    </KvitScreen>
  )
}
