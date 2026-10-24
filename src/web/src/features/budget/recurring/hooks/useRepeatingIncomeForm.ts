import { useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { hasErrorCode } from '@/core/api/errors'
import type {
  RecurringIncome,
  RecurringIncomeFields,
  RecurringIncomeFrequency,
} from '@/core/services/recurringIncomes/recurringIncomesService'
import { textOrNull } from '@/features/expenses/shared/expenseTitle'
import { useSaveOnce } from '../../categories/hooks/useSaveOnce'
import type { PersonalEntryFieldName, PersonalEntryFormState } from '../../entries/hooks/usePersonalEntryForm'
import { refreshRecurringIncomes } from '../recurringIncomeCache'
import { endAfterMonths, recurringDaysFromStart } from '../recurringIncomeRhythm'
import { useCreateRecurringIncome, useEditRecurringIncome } from './useSaveRecurringIncome'
import { recurringIncomeNotFoundCode, useStopRecurringIncome } from './useStopRecurringIncome'

export type IncomeRepeat = 'Never' | RecurringIncomeFrequency

export interface RepeatingIncomeFormState {
  repeat: IncomeRepeat
  frequency: RecurringIncomeFrequency | null
  endsOn: string | null
  form: PersonalEntryFormState
  cycleRepeat: () => void
  changeEndsOn: (endsOn: string | null) => void
  changeEndsAfterMonths: (months: number) => void
  save: () => void
  stop: () => void
}

type IncomeEnd = { kind: 'afterMonths'; months: number } | { kind: 'onDate'; endsOn: string | null }

const repeatAfter: Record<IncomeRepeat, IncomeRepeat> = { Never: 'Monthly', Monthly: 'Weekly', Weekly: 'Never' }
const otherFrequency: Record<RecurringIncomeFrequency, RecurringIncomeFrequency> = { Monthly: 'Weekly', Weekly: 'Monthly' }

const refusedCodesOf: Record<PersonalEntryFieldName, readonly string[]> = {
  amount: [
    'RECURRING_INCOME_AMOUNT_NOT_POSITIVE',
    'RECURRING_INCOME_AMOUNT_TOO_LARGE',
    'RECURRING_INCOME_CURRENCY_INVALID',
    'MONEY_NOT_ON_CURRENCY_STEP',
  ],
  date: ['RECURRING_INCOME_START_DATE_INVALID', 'RECURRING_INCOME_START_CANNOT_CHANGE', 'RECURRING_INCOME_DAY_INVALID'],
  note: ['RECURRING_INCOME_TITLE_INVALID'],
  category: [],
  ends: ['RECURRING_INCOME_END_DATE_INVALID'],
}

export function useRepeatingIncomeForm(
  entryForm: PersonalEntryFormState,
  recurringIncome: RecurringIncome | undefined,
  initialFrequency: RecurringIncomeFrequency | undefined,
): RepeatingIncomeFormState {
  const queryClient = useQueryClient()
  const oneAtATime = useSaveOnce()
  const create = useCreateRecurringIncome(oneAtATime)
  const edit = useEditRecurringIncome(oneAtATime)
  const stopper = useStopRecurringIncome(oneAtATime)
  const [clientRequestId] = useState<string>(() => crypto.randomUUID())
  const [repeat, setRepeat] = useState<IncomeRepeat>(recurringIncome?.frequency ?? initialFrequency ?? 'Never')
  const [end, setEnd] = useState<IncomeEnd>({ kind: 'onDate', endsOn: recurringIncome?.endsOn ?? null })
  const endsOn = end.kind === 'afterMonths' ? endAfterMonths(entryForm.entryDate, end.months) : end.endsOn

  const saveError = recurringIncome === undefined ? create.error : edit.error
  const shownError = saveError !== null && !hasErrorCode(saveError, recurringIncomeNotFoundCode) ? saveError : null
  const frequency = repeat === 'Never' ? null : repeat

  function handleSaveRefusal(error: Error): void {
    if (hasErrorCode(error, recurringIncomeNotFoundCode)) {
      void refreshRecurringIncomes(queryClient)
      entryForm.closeBecauseGone(error)
    }
  }

  function save(): void {
    const { amountMinor, currency, entryDate } = entryForm
    if (amountMinor === null) {
      throw new Error(`Save was pressed without a valid amount ("${entryForm.amountText}")`)
    }
    if (frequency === null) {
      throw new Error('A repeating income was saved while Repeats says Never')
    }
    entryForm.clearChangedMarks()
    const fields: RecurringIncomeFields = {
      amountMinor,
      currency,
      title: textOrNull(entryForm.note),
      ...recurringDaysFromStart(frequency, entryDate),
      startsOn: entryDate,
      endsOn,
    }
    const onDone = (): void => entryForm.closeAfterSave(currency)
    if (recurringIncome === undefined) {
      create.run({ clientRequestId, ...fields }, onDone, handleSaveRefusal)
    } else {
      edit.run({ recurringIncomeId: recurringIncome.id, fields }, onDone, handleSaveRefusal)
    }
  }

  function stop(): void {
    if (recurringIncome === undefined) {
      throw new Error('Stop was pressed while adding a new income')
    }
    stopper.stop(recurringIncome.id, entryForm.sheet.close)
  }

  return {
    repeat,
    frequency,
    endsOn,
    form: {
      ...entryForm,
      isBusy: entryForm.isBusy || create.isPending || edit.isPending || stopper.isPending,
      shownError,
      isRefused: (field) =>
        refusedCodesOf[field].some((code) => hasErrorCode(shownError, code)) && !entryForm.isChangedSinceSave(field),
    },
    cycleRepeat: () => {
      setRepeat((current) =>
        recurringIncome === undefined || current === 'Never' ? repeatAfter[current] : otherFrequency[current],
      )
    },
    changeEndsOn: (nextEndsOn) => {
      setEnd({ kind: 'onDate', endsOn: nextEndsOn })
      entryForm.markChanged('ends')
    },
    changeEndsAfterMonths: (months) => {
      setEnd({ kind: 'afterMonths', months })
      entryForm.markChanged('ends')
    },
    save,
    stop,
  }
}
