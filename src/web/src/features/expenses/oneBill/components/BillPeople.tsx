import { useTranslation } from 'react-i18next'
import { KvitAvatar } from '@/shared/components/KvitAvatar'
import type { ExpensePerson } from '../../shared/expensePeople'

interface BillPeopleProps {
  people: readonly ExpensePerson[]
  onRemove: (personId: string) => void
  onAddName: () => void
}

const circleLook = 'flex w-16 flex-col items-center gap-1'
const circleLabelLook = 'max-w-16 truncate text-[0.8125rem] text-muted-foreground'

export function BillPeople({ people, onRemove, onAddName }: BillPeopleProps) {
  const { t } = useTranslation()

  return (
    <section className="flex flex-col gap-2.5">
      <h2 className="text-[1.0625rem] font-bold">{t('oneBill.people')}</h2>
      <ul className="flex flex-wrap items-start gap-2.5">
        {people.map((person) => (
          <li key={person.memberId} className={`relative ${circleLook}`}>
            <KvitAvatar
              name={person.name}
              pictureUrl={person.pictureUrl}
              colorIndex={person.colorIndex}
              size="large"
            />
            <span className={circleLabelLook}>{person.isYou ? t('expense.me') : person.name}</span>
            {!person.isYou && (
              <button
                type="button"
                aria-label={t('oneBill.removeName', { name: person.name })}
                onClick={() => onRemove(person.memberId)}
                className="pressable absolute -top-3.5 -right-2.5 grid min-h-11 min-w-11 place-items-center rounded-full"
              >
                <span
                  aria-hidden="true"
                  className="grid size-6 place-items-center rounded-full border border-field-border bg-field text-xs font-bold text-field-foreground"
                >
                  ✕
                </span>
              </button>
            )}
          </li>
        ))}
        <li className={circleLook}>
          <button
            type="button"
            onClick={onAddName}
            className={`pressable group min-h-11 min-w-11 focus-visible:outline-none ${circleLook}`}
          >
            <span
              aria-hidden="true"
              className="grid size-13 place-items-center rounded-full border-2 border-dashed border-field-border text-[1.4rem] text-foreground group-focus-visible:outline-3 group-focus-visible:outline-offset-2 group-focus-visible:outline-ring"
            >
              +
            </span>
            <span className={circleLabelLook}>{t('oneBill.nameLabel')}</span>
          </button>
        </li>
      </ul>
    </section>
  )
}
