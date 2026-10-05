import { useTranslation } from 'react-i18next'
import type { GroupMember } from '@/core/services/groups/membersService'
import { KvitButton } from '@/shared/components/KvitButton'

interface MemberListItemProps {
  member: GroupMember
  showsTakenName: boolean
  canClaim: boolean
  isClaimPending: boolean
  hasOptions: boolean
  onClaim: () => void
  onOpenOptions: () => void
}

const badgeLook = 'rounded-full px-2 py-0.5 text-xs font-bold whitespace-nowrap'

export function MemberListItem({
  member,
  showsTakenName,
  canClaim,
  isClaimPending,
  hasOptions,
  onClaim,
  onOpenOptions,
}: MemberListItemProps) {
  const { t } = useTranslation()

  function renderSubline() {
    if (member.isNameOnly) {
      return t('members.nameOnly')
    }
    if (showsTakenName && member.claimedName !== null) {
      return t('members.tookName', { name: member.claimedName })
    }
    return null
  }

  const subline = renderSubline()

  return (
    <li className="flex min-h-14 items-center gap-3 rounded-2xl bg-card px-3.5 py-3 text-card-foreground">
      <span className="flex min-w-0 flex-1 flex-col gap-0.5">
        <span className="flex flex-wrap items-center gap-x-1.5 gap-y-1">
          <span className="font-semibold break-words">{member.displayName}</span>
          {member.isOwner && (
            <span className={`${badgeLook} bg-track-active text-foreground`}>
              {t('members.owner')}
            </span>
          )}
          {member.isYou && (
            <span className={`${badgeLook} border border-field-border text-muted-foreground`}>
              {t('members.you')}
            </span>
          )}
        </span>
        {subline !== null && (
          <span className="text-[0.8125rem] text-muted-foreground">{subline}</span>
        )}
      </span>
      {canClaim && (
        <KvitButton
          variant="secondary"
          className="min-h-11 w-auto flex-none rounded-full bg-track-active px-4 text-[0.9375rem]"
          disabled={isClaimPending}
          onClick={onClaim}
        >
          {t('members.thatsMe')}
        </KvitButton>
      )}
      {hasOptions && (
        <button
          type="button"
          aria-label={t('members.optionsFor', { name: member.displayName })}
          onClick={onOpenOptions}
          className="pressable grid size-11 flex-none place-items-center rounded-full text-xl leading-none font-bold hover:bg-secondary-hover"
        >
          <span aria-hidden="true">⋯</span>
        </button>
      )}
    </li>
  )
}
