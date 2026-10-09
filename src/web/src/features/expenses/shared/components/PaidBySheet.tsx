import { useTranslation } from 'react-i18next'
import { KvitAvatar } from '@/shared/components/KvitAvatar'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { useKvitSheet } from '@/shared/components/useKvitSheet'
import { personLabel, type ExpensePerson } from '../expensePeople'
import { Tick } from './Tick'

interface PaidBySheetProps {
  people: readonly ExpensePerson[]
  paidByMemberId: string
  onPick: (memberId: string) => void
  onClose: () => void
}

export function PaidBySheet({ people, paidByMemberId, onPick, onClose }: PaidBySheetProps) {
  const { t } = useTranslation()
  const sheet = useKvitSheet()

  function pick(memberId: string): void {
    onPick(memberId)
    sheet.close()
  }

  return (
    <KvitSheet title={t('expense.paidBy')} onClose={onClose} actionsRef={sheet.actionsRef}>
      <div className="-mx-1 flex max-h-[60dvh] flex-col overflow-y-auto">
        {people.map((person) => {
          const isPayer: boolean = person.memberId === paidByMemberId
          return (
            <button
              key={person.memberId}
              type="button"
              aria-pressed={isPayer}
              onClick={() => pick(person.memberId)}
              className="pressable flex min-h-13 items-center gap-3 rounded-xl px-1 text-left hover:bg-secondary-hover"
            >
              <KvitAvatar name={person.name} pictureUrl={person.pictureUrl} colorIndex={person.colorIndex} />
              <span className="min-w-0 flex-1 font-semibold break-words">{personLabel(person, t)}</span>
              <Tick isOn={isPayer} />
            </button>
          )
        })}
      </div>
    </KvitSheet>
  )
}
