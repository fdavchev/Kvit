import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { kvitChipLook } from '@/shared/components/kvitChipLook'
import { useKvitSheet } from '@/shared/components/useKvitSheet'
import { endAfterMonths } from '../recurringIncomeRhythm'

export type EndsChoice = { kind: 'never' } | { kind: 'afterMonths'; months: number } | { kind: 'pickDate' }

interface EndsOption {
  labelKey: string
  monthsAfterStart: number | null
}

interface RecurringIncomeEndsSheetProps {
  startsOn: string
  endsOn: string | null
  onClose: (choice: EndsChoice | null) => void
}

const endsOptions: readonly EndsOption[] = [
  { labelKey: 'budget.income.endsOptionNever', monthsAfterStart: null },
  { labelKey: 'budget.income.endsIn3Months', monthsAfterStart: 3 },
  { labelKey: 'budget.income.endsIn6Months', monthsAfterStart: 6 },
  { labelKey: 'budget.income.endsIn1Year', monthsAfterStart: 12 },
]

const calendarEmoji = '\u{1F4C5}'

function endOf(option: EndsOption, startsOn: string): string | null {
  return option.monthsAfterStart === null ? null : endAfterMonths(startsOn, option.monthsAfterStart)
}

function choiceOf(option: EndsOption): EndsChoice {
  return option.monthsAfterStart === null ? { kind: 'never' } : { kind: 'afterMonths', months: option.monthsAfterStart }
}

export function RecurringIncomeEndsSheet({ startsOn, endsOn, onClose }: RecurringIncomeEndsSheetProps) {
  const { t } = useTranslation()
  const sheet = useKvitSheet()
  const [choice, setChoice] = useState<EndsChoice | null>(null)
  const isPickedDate = endsOptions.every((option) => endOf(option, startsOn) !== endsOn)

  function choose(chosen: EndsChoice): void {
    if (choice !== null) {
      return
    }
    setChoice(chosen)
    sheet.close()
  }

  return (
    <KvitSheet title={t('budget.income.endsTitle')} onClose={() => onClose(choice)} actionsRef={sheet.actionsRef}>
      <div className="flex flex-wrap gap-2">
        {endsOptions.map((option) => {
          const optionEnd = endOf(option, startsOn)
          return (
            <button
              key={option.labelKey}
              type="button"
              aria-pressed={optionEnd === endsOn}
              className={kvitChipLook}
              onClick={() => choose(choiceOf(option))}
            >
              {t(option.labelKey)}
            </button>
          )
        })}
        <button
          type="button"
          aria-pressed={isPickedDate}
          className={kvitChipLook}
          onClick={() => choose({ kind: 'pickDate' })}
        >
          <span aria-hidden="true" className="me-1.5">
            {calendarEmoji}
          </span>
          {t('budget.income.endsPickDate')}
        </button>
      </div>
    </KvitSheet>
  )
}
