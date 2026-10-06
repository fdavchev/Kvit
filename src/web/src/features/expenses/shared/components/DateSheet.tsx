import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { kvitChipLook } from '@/shared/components/kvitChipLook'
import { useKvitSheet } from '@/shared/components/useKvitSheet'
import { addDays } from '@/shared/utils/formatDate'

interface DateSheetProps {
  expenseDate: string
  today: string
  onPick: (expenseDate: string) => void
  onClose: () => void
}

const earliestDate = '2000-01-01'
const daysAhead = 365

export function DateSheet({ expenseDate, today, onPick, onClose }: DateSheetProps) {
  const { t } = useTranslation()
  const sheet = useKvitSheet()
  const fieldId = useId()
  const yesterday = addDays(today, -1)

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
          onClick={() => pick(today)}
          className={kvitChipLook}
        >
          {t('expenses.today')}
        </button>
        <button
          type="button"
          aria-pressed={expenseDate === yesterday}
          onClick={() => pick(yesterday)}
          className={kvitChipLook}
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
          value={expenseDate}
          min={earliestDate}
          max={addDays(today, daysAhead)}
          onChange={(event) => {
            if (event.target.value !== '') {
              pick(event.target.value)
            }
          }}
          className="min-h-14 w-full rounded-[14px] border-2 border-field-border bg-field px-4 text-[1.0625rem] font-semibold text-field-foreground focus:border-field-border-focus focus-visible:outline-offset-0"
        />
        <p className="mt-2 text-[0.9375rem] text-muted-foreground">{t('expense.dateHint')}</p>
      </div>
    </KvitSheet>
  )
}
