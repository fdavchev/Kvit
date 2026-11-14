import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import { countedText } from '@/core/i18n/pluralForm'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { GroupStatus } from '@/core/services/groups/groupsService'
import type { OwnerToolsGroup } from '@/core/services/ownerTools/ownerToolsService'
import { balancesCardLook } from '@/features/groups/balances/balancesLooks'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitEmojiTile } from '@/shared/components/KvitEmojiTile'
import { cn } from '@/shared/utils/cn'
import { ownerToolsBadgeLook } from '../ownerToolsLooks'

export type GroupActionKind = 'closeGroup' | 'reopenGroup'

interface OwnerToolsGroupCardProps {
  group: OwnerToolsGroup
  onAction: (kind: GroupActionKind) => void
}

const statusKeys: Record<GroupStatus, string> = {
  Open: 'open',
  Closing: 'closing',
  Finished: 'finished',
}

export function OwnerToolsGroupCard({ group, onAction }: OwnerToolsGroupCardProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const nameId = useId()
  const isClosedByOwner = group.closedByAdminAt !== null

  return (
    <section aria-labelledby={nameId} className={cn(balancesCardLook, 'flex flex-col gap-3')}>
      <div className="flex min-w-0 items-center gap-3">
        <KvitEmojiTile emoji={group.emoji} />
        <div className="flex min-w-0 flex-col">
          <h2 id={nameId} className="font-bold wrap-anywhere">
            {group.name}
          </h2>
          <p className="text-[0.9375rem] wrap-anywhere text-muted-foreground">
            {t('ownerTools.groups.owner', { name: group.ownerName })}
          </p>
        </div>
      </div>
      <div className="flex flex-wrap items-center gap-2">
        <span className="text-[0.9375rem] text-muted-foreground">
          {countedText(t, 'ownerTools.groups.members', group.memberCount, language)}
        </span>
        <span className={ownerToolsBadgeLook}>{t(`ownerTools.groups.status.${statusKeyOf(group)}`)}</span>
      </div>
      <KvitButton
        type="button"
        variant={isClosedByOwner ? 'secondary' : 'danger'}
        onClick={() => onAction(isClosedByOwner ? 'reopenGroup' : 'closeGroup')}
      >
        {t(isClosedByOwner ? 'ownerTools.groups.reopen' : 'ownerTools.groups.close')}
      </KvitButton>
    </section>
  )
}

function statusKeyOf(group: OwnerToolsGroup): string {
  if (group.closedByAdminAt !== null) {
    return 'closedByOwner'
  }
  if (group.isDeleted) {
    return 'deleted'
  }
  return statusKeys[group.status]
}
