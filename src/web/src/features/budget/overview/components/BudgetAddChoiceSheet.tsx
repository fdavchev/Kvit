import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { PersonalEntryKind } from '@/core/services/personalEntries/personalEntriesService'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { useKvitSheet } from '@/shared/components/useKvitSheet'

interface BudgetAddChoiceSheetProps {
  onClose: (chosenKind: PersonalEntryKind | null) => void
}

interface AddChoice {
  kind: PersonalEntryKind
  emoji: string
  labelKey: string
}

const addChoices: readonly AddChoice[] = [
  { kind: 'Spending', emoji: '\u{1F4B8}', labelKey: 'budget.add.spending' },
  { kind: 'Income', emoji: '\u{1F4B0}', labelKey: 'budget.add.income' },
]

const choiceLook =
  'pressable flex min-h-20 min-w-0 flex-col items-center justify-center gap-1.5 rounded-[18px] border border-field-border bg-field px-3 py-3 font-semibold text-field-foreground hover:border-field-border-focus'

export function BudgetAddChoiceSheet({ onClose }: BudgetAddChoiceSheetProps) {
  const { t } = useTranslation()
  const sheet = useKvitSheet()
  const [chosenKind, setChosenKind] = useState<PersonalEntryKind | null>(null)

  function choose(kind: PersonalEntryKind): void {
    if (chosenKind !== null) {
      return
    }
    setChosenKind(kind)
    sheet.close()
  }

  return (
    <KvitSheet
      title={t('budget.add.title')}
      onClose={() => onClose(chosenKind)}
      actionsRef={sheet.actionsRef}
      returnsFocusOnClose={() => chosenKind === null}
    >
      <div className="grid grid-cols-2 gap-3">
        {addChoices.map((choice) => (
          <button key={choice.kind} type="button" className={choiceLook} onClick={() => choose(choice.kind)}>
            <span aria-hidden="true" className="text-[1.75rem] leading-none">
              {choice.emoji}
            </span>
            <span className="w-full text-center break-words">{t(choice.labelKey)}</span>
          </button>
        ))}
      </div>
    </KvitSheet>
  )
}
