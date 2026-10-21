import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { PersonalEntry } from '@/core/services/personalEntries/personalEntriesService'
import type { RecurringIncome } from '@/core/services/recurringIncomes/recurringIncomesService'
import { DateSheet } from '@/features/expenses/shared/components/DateSheet'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { addMonthsToDate } from '@/shared/utils/calendarMonth'
import { formatExpenseDay } from '@/shared/utils/formatDate'
import { earliestBudgetDate } from '../../budgetMonths'
import { RecurringIncomeEndsSheet, type EndsChoice } from '../../recurring/components/RecurringIncomeEndsSheet'
import { useRepeatingIncomeForm } from '../../recurring/hooks/useRepeatingIncomeForm'
import { recurringDaysFromStart, recurringRhythmText } from '../../recurring/recurringIncomeRhythm'
import { usePersonalEntryForm, type PersonalEntryStart } from '../hooks/usePersonalEntryForm'
import { personalEntryChipLook } from '../personalEntryChipLook'
import { PersonalEntryForm } from './PersonalEntryForm'

interface PersonalIncomeSheetProps {
  entry?: PersonalEntry
  recurringIncome?: RecurringIncome
  initialFrequency?: 'Monthly'
  opener?: Element | null
  onClose: () => void
}

const startWindowMonths = 12
const endWindowMonths = 120

function startOf(recurringIncome: RecurringIncome | undefined): PersonalEntryStart | undefined {
  if (recurringIncome === undefined) {
    return undefined
  }
  const { amountMinor, currency, startsOn, title } = recurringIncome
  return { amountMinor, currency, entryDate: startsOn, note: title }
}

export function PersonalIncomeSheet({ entry, recurringIncome, initialFrequency, opener, onClose }: PersonalIncomeSheetProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const entryForm = usePersonalEntryForm(entry, entry ?? startOf(recurringIncome))
  const repeating = useRepeatingIncomeForm(entryForm, recurringIncome, initialFrequency)
  const [isEndsOpen, setIsEndsOpen] = useState<boolean>(false)
  const [isEndDateOpen, setIsEndDateOpen] = useState<boolean>(false)
  const { frequency, endsOn } = repeating
  const form = frequency === null ? entryForm : repeating.form
  const startsOn = form.entryDate
  const isNew = entry === undefined && recurringIncome === undefined

  function closeEnds(choice: EndsChoice | null): void {
    setIsEndsOpen(false)
    if (choice?.kind === 'end') {
      repeating.changeEndsOn(choice.endsOn)
    }
    if (choice?.kind === 'pickDate') {
      setIsEndDateOpen(true)
    }
  }

  const repeatChips = entry === undefined && (
    <>
      <button type="button" disabled={form.isBusy} className={personalEntryChipLook} onClick={repeating.cycleRepeat}>
        <span aria-hidden="true">🔁</span>
        {t(`budget.income.repeats${repeating.repeat}`)}
      </button>
      {frequency !== null && (
        <button
          type="button"
          aria-invalid={form.isRefused('ends')}
          disabled={form.isBusy}
          className={personalEntryChipLook}
          onClick={() => setIsEndsOpen(true)}
        >
          <span aria-hidden="true">⏹</span>
          {endsOn === null
            ? t('budget.income.endsNever')
            : t('budget.income.endsOn', { day: formatExpenseDay(endsOn, form.today, language) })}
        </button>
      )}
    </>
  )

  return (
    <KvitSheet
      title={t(isNew ? 'budget.income.title' : 'budget.income.editTitle')}
      onClose={onClose}
      actionsRef={entryForm.sheet.actionsRef}
      opener={opener}
    >
      <PersonalEntryForm
        form={form}
        extraChip={repeatChips}
        belowChips={
          frequency !== null && (
            <p className="-mt-2 text-[0.9375rem] text-muted-foreground">
              {recurringRhythmText(recurringDaysFromStart(frequency, startsOn), language, t)}
            </p>
          )
        }
        footer={
          recurringIncome !== undefined && (
            <KvitButton variant="danger" disabled={form.isBusy} onClick={repeating.stop}>
              {t('budget.recurring.stop')}
            </KvitButton>
          )
        }
        isDateLocked={recurringIncome?.hasEntries === true}
        dateChipText={frequency === null ? undefined : (dayText) => t('budget.income.startsOn', { day: dayText })}
        dateWindow={
          frequency === null
            ? undefined
            : {
                earliestDate: earliestBudgetDate(form.today),
                latestDate: addMonthsToDate(form.today, startWindowMonths),
                hint: t('budget.income.startDateHint'),
              }
        }
        onSubmit={frequency === null ? () => entryForm.save({ kind: 'Income' }) : repeating.save}
      />
      {isEndsOpen && <RecurringIncomeEndsSheet startsOn={startsOn} endsOn={endsOn} onClose={closeEnds} />}
      {isEndDateOpen && (
        <DateSheet
          expenseDate={endsOn ?? startsOn}
          today={form.today}
          earliestDate={startsOn}
          latestDate={addMonthsToDate(startsOn, endWindowMonths)}
          hint={t('budget.income.endDateHint')}
          onPick={repeating.changeEndsOn}
          onClose={() => setIsEndDateOpen(false)}
        />
      )}
    </KvitSheet>
  )
}
