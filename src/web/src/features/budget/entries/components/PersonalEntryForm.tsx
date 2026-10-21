import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessageKey } from '@/core/api/errors'
import { useLanguage } from '@/core/i18n/useLanguage'
import { AmountField } from '@/features/expenses/shared/components/AmountField'
import { DateSheet } from '@/features/expenses/shared/components/DateSheet'
import { NoteField } from '@/features/expenses/shared/components/NoteField'
import { expenseDayText } from '@/features/expenses/shared/expenseDayText'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitForm } from '@/shared/components/KvitForm'
import { earliestBudgetDate } from '../../budgetMonths'
import type { PersonalEntryFormState } from '../hooks/usePersonalEntryForm'
import { personalEntryChipLook } from '../personalEntryChipLook'

export interface PersonalEntryDateWindow {
  earliestDate: string
  latestDate: string
  hint: string
}

interface PersonalEntryFormProps {
  form: PersonalEntryFormState
  onSubmit: () => void
  extraChip?: ReactNode
  belowChips?: ReactNode
  footer?: ReactNode
  dateChipText?: (dayText: string) => string
  dateWindow?: PersonalEntryDateWindow
  isDateLocked?: boolean
}

export function PersonalEntryForm({
  form,
  onSubmit,
  extraChip,
  belowChips,
  footer,
  dateChipText,
  dateWindow,
  isDateLocked = false,
}: PersonalEntryFormProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const { entry } = form
  const dayText = expenseDayText(form.entryDate, form.today, language, t)
  const shownDateWindow = dateWindow ?? {
    earliestDate: earliestBudgetDate(form.today),
    latestDate: form.today,
    hint: t('budget.entries.dateHint'),
  }

  return (
    <>
      <KvitForm
        submitLabel={t('groupSettings.save')}
        isPending={form.isBusy}
        canSubmit={form.amountMinor !== null}
        errorMessage={form.shownError === null ? null : t(errorMessageKey(form.shownError))}
        onSubmit={onSubmit}
        footer={
          <>
            {entry !== undefined && (
              <KvitButton variant="danger" disabled={form.isBusy} onClick={() => form.deleteEntry(entry)}>
                {t('expense.delete')}
              </KvitButton>
            )}
            {footer}
          </>
        }
      >
        <AmountField
          amountText={form.amountText}
          currency={form.currency}
          invalid={form.isRefused('amount')}
          onAmountTextChange={form.changeAmountText}
          onCurrencyChange={form.changeCurrency}
        />
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            aria-invalid={form.isRefused('date')}
            disabled={form.isBusy || isDateLocked}
            className={personalEntryChipLook}
            onClick={() => form.setIsDateOpen(true)}
          >
            <span aria-hidden="true">📅</span>
            {dateChipText === undefined ? dayText : dateChipText(dayText)}
          </button>
          {!form.isNoteOpen && (
            <button type="button" disabled={form.isBusy} className={personalEntryChipLook} onClick={form.openNote}>
              <span aria-hidden="true">📝</span>
              {t('expense.note')}
            </button>
          )}
          {extraChip}
        </div>
        {belowChips}
        {form.isNoteOpen && (
          <NoteField
            value={form.note}
            invalid={form.isRefused('note')}
            autoFocus={form.isNoteEmptyAtStart}
            onChange={form.changeNote}
          />
        )}
      </KvitForm>
      {form.isDateOpen && (
        <DateSheet
          expenseDate={form.entryDate}
          today={form.today}
          earliestDate={shownDateWindow.earliestDate}
          latestDate={shownDateWindow.latestDate}
          hint={shownDateWindow.hint}
          onPick={form.changeEntryDate}
          onClose={() => form.setIsDateOpen(false)}
        />
      )}
    </>
  )
}
