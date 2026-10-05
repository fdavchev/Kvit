import { useTranslation } from 'react-i18next'
import type { RemovedMember } from '@/core/services/groups/membersService'
import { KvitButton } from '@/shared/components/KvitButton'

interface RemovedMembersProps {
  removed: RemovedMember[]
  pendingMemberId: string | null
  onLetBackIn: (member: RemovedMember) => void
}

export function RemovedMembers({ removed, pendingMemberId, onLetBackIn }: RemovedMembersProps) {
  const { t } = useTranslation()

  return (
    <section className="flex flex-col gap-2">
      <h2 className="text-[1.0625rem] font-bold">{t('members.removedTitle')}</h2>
      <ul className="flex flex-col gap-2.5">
        {removed.map((member) => (
          <li
            key={member.id}
            className="flex min-h-14 items-center gap-3 rounded-2xl bg-card px-3.5 py-3 text-card-foreground"
          >
            <span className="min-w-0 flex-1 font-semibold break-words">{member.displayName}</span>
            <KvitButton
              className="min-h-11 w-auto flex-none rounded-full px-4 text-[0.9375rem] font-semibold"
              disabled={pendingMemberId === member.id}
              onClick={() => onLetBackIn(member)}
            >
              {t('members.letBackIn')}
            </KvitButton>
          </li>
        ))}
      </ul>
    </section>
  )
}
