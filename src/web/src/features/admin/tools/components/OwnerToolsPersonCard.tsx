import { useTranslation } from 'react-i18next'
import { countedText } from '@/core/i18n/pluralForm'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { OwnerToolsPerson } from '@/core/services/ownerTools/ownerToolsService'
import { balancesCardLook } from '@/features/groups/balances/balancesLooks'
import { KvitButton } from '@/shared/components/KvitButton'
import { cn } from '@/shared/utils/cn'
import { ownerToolsBadgeLook } from '../ownerToolsLooks'

export type PersonActionKind = 'suspend' | 'unsuspend' | 'eraseName'

interface OwnerToolsPersonCardProps {
  person: OwnerToolsPerson
  isMe: boolean
  onAction: (kind: PersonActionKind) => void
}

export function OwnerToolsPersonCard({ person, isMe, onAction }: OwnerToolsPersonCardProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const badgeKeys = shownBadgeKeys(person)
  const actionKinds = isMe ? [] : actionKindsFor(person)

  return (
    <div className={cn(balancesCardLook, 'flex flex-col gap-2')}>
      <div className="flex min-w-0 flex-col">
        <p className="font-bold wrap-anywhere">{person.displayName}</p>
        {person.email !== null && <p className="text-[0.9375rem] wrap-anywhere text-muted-foreground">{person.email}</p>}
        <p className="text-[0.9375rem] text-muted-foreground">
          {countedText(t, 'ownerTools.people.groupCount', person.groupCount, language)}
        </p>
      </div>
      {badgeKeys.length > 0 && (
        <div className="flex flex-wrap gap-1.5">
          {badgeKeys.map((key) => (
            <span key={key} className={ownerToolsBadgeLook}>
              {t(`ownerTools.badge.${key}`)}
            </span>
          ))}
        </div>
      )}
      {actionKinds.length > 0 && (
        <div className="flex flex-col gap-2 pt-1">
          {actionKinds.map((kind) => (
            <KvitButton
              key={kind}
              type="button"
              variant={kind === 'unsuspend' ? 'secondary' : 'danger'}
              onClick={() => onAction(kind)}
            >
              {t(`ownerTools.${kind}`)}
            </KvitButton>
          ))}
        </div>
      )}
    </div>
  )
}

function shownBadgeKeys(person: OwnerToolsPerson): string[] {
  const badges: [boolean, string][] = [
    [person.isSuspended, 'suspended'],
    [person.isGuest, 'guest'],
    [person.isAdmin, 'admin'],
    [person.deletionPending, 'deletionPending'],
    [person.isDeleted, 'deleted'],
  ]
  return badges.filter(([isShown]) => isShown).map(([, key]) => key)
}

function actionKindsFor(person: OwnerToolsPerson): PersonActionKind[] {
  if (person.isAdmin) {
    return []
  }
  const suspendKind: PersonActionKind = person.isSuspended ? 'unsuspend' : 'suspend'
  return person.isDeleted ? [suspendKind] : [suspendKind, 'eraseName']
}
