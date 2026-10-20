import { useId, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { kvitChipLook } from '@/shared/components/kvitChipLook'
import { useKvitSheet } from '@/shared/components/useKvitSheet'
import { cn } from '@/shared/utils/cn'
import { addDays, isCalendarDate } from '@/shared/utils/formatDate'

interface DateSheetProps {
  expenseDate: string
  today: string
  earliestDate?: string
  latestDate?: string
  hint?: string
  onPick: (expenseDate: string) => void
  onClose: () => void
}

const expenseEarliestDate = '2000-01-01'
const expenseDaysAhead = 365
const dayChipLook = cn(kvitChipLook, 'disabled:pointer-events-none disabled:opacity-50')

export function DateSheet({
  expenseDate,
  today,
  earliestDate = expenseEarliestDate,
  latestDate = addDays(today, expenseDaysAhead),
  hint,
  onPick,
  onClose,
}: DateSheetProps) {
  const { t } = useTranslation()
  const sheet = useKvitSheet()
  const fieldId = useId()
  const [typedDate, setTypedDate] = useState<string>(expenseDate)
  const yesterday = addDays(today, -1)
  const isAllowed = (date: string): boolean => date >= earliestDate && date <= latestDate
  const canApplyTypedDate: boolean = isCalendarDate(typedDate) && isAllowed(typedDate)

  function pick(pickedDate: string): void {
    onPick(pickedDate)
    sheet.close()
  }

  return (
    <KvitSheet title={t('expense.pickDate')} onClose={onClose} actionsRef={sheet.actionsRef}>
      <div className="flex flex-wrap gap-2">
        <button
          type="button"
          aria-pressed={expenseDate === today}
          disabled={!isAllowed(today)}
          onClick={() => pick(today)}
          className={dayChipLook}
        >
          {t('expenses.today')}
        </button>
        <button
          type="button"
          aria-pressed={expenseDate === yesterday}
          disabled={!isAllowed(yesterday)}
          onClick={() => pick(yesterday)}
          className={dayChipLook}
        >
          {t('expenses.yesterday')}
        </button>
      </div>
      <div className="flex flex-col">
        <label htmlFor={fieldId} className="mb-2 text-[0.9375rem] font-bold">
          {t('expense.date')}
        </label>
        <input
          id={fieldId}
          type="date"
          value={typedDate}
          min={earliestDate}
          max={latestDate}
          onChange={(event) => setTypedDate(event.target.value)}
          className="min-h-14 w-full rounded-[14px] border-2 border-field-border-raised bg-field px-4 text-[1.0625rem] font-semibold text-field-foreground focus:border-field-border-focus focus-visible:outline-offset-0"
        />
        <p className="mt-2 text-[0.9375rem] text-muted-foreground">{hint ?? t('expense.dateHint')}</p>
      </div>
      <KvitButton disabled={!canApplyTypedDate} onClick={() => pick(typedDate)}>
        {t('expense.done')}
      </KvitButton>
    </KvitSheet>
  )
}
