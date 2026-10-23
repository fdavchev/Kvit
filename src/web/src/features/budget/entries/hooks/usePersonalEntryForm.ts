import { useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey, hasErrorCode } from '@/core/api/errors'
import { useSignedInMe } from '@/core/auth/useSignedInMe'
import type {
  PersonalEntry,
  PersonalEntryFields,
  PersonalIncomeFields,
  PersonalSpendingFields,
} from '@/core/services/personalEntries/personalEntriesService'
import { textOrNull } from '@/features/expenses/shared/expenseTitle'
import { useKvitSheet, type KvitSheetControl } from '@/shared/components/useKvitSheet'
import { showUndoToast } from '@/shared/toasts/showUndoToast'
import { todayInTimeZone } from '@/shared/utils/formatDate'
import type { Currency } from '@/shared/utils/formatMoney'
import { moneyInputText, parseMoneyInput } from '@/shared/utils/parseMoneyInput'
import { useSaveOnce } from '../../categories/hooks/useSaveOnce'
import { readLastEntryCurrency, saveLastEntryCurrency } from '../lastEntryCurrency'
import { refreshPersonalEntries } from '../personalEntryCache'
import { useAddPersonalEntry } from './useAddPersonalEntry'
import { useDeletePersonalEntry } from './useDeletePersonalEntry'
import { useEditPersonalEntry } from './useEditPersonalEntry'
import { useRestorePersonalEntry } from './useRestorePersonalEntry'

export type PersonalEntryFieldName = 'amount' | 'date' | 'note' | 'category' | 'ends'

export type PersonalEntryStart = Pick<PersonalEntry, 'amountMinor' | 'currency' | 'entryDate' | 'note'>

export type PersonalEntryKindFields = Pick<PersonalSpendingFields, 'kind' | 'categoryId'> | Pick<PersonalIncomeFields, 'kind'>

export interface PersonalEntryFormState {
  entry: PersonalEntry | undefined
  sheet: KvitSheetControl
  today: string
  amountText: string
  currency: Currency
  amountMinor: number | null
  entryDate: string
  note: string
  isNoteEmptyAtStart: boolean
  isNoteOpen: boolean
  isDateOpen: boolean
  isBusy: boolean
  shownError: Error | null
  changeAmountText: (amountText: string) => void
  changeCurrency: (currency: Currency) => void
  changeEntryDate: (entryDate: string) => void
  changeNote: (note: string) => void
  openNote: () => void
  setIsDateOpen: (isOpen: boolean) => void
  markChanged: (field: PersonalEntryFieldName) => void
  isChangedSinceSave: (field: PersonalEntryFieldName) => boolean
  clearChangedMarks: () => void
  isRefused: (field: PersonalEntryFieldName) => boolean
  closeAfterSave: (savedCurrency: Currency) => void
  closeBecauseGone: (error: Error) => void
  save: (kindFields: PersonalEntryKindFields) => void
  deleteEntry: (deleted: PersonalEntry) => void
}

const entryNotFoundCode = 'PERSONAL_ENTRY_NOT_FOUND'

const refusedCodesOf: Record<PersonalEntryFieldName, readonly string[]> = {
  amount: [
    'PERSONAL_ENTRY_AMOUNT_NOT_POSITIVE',
    'PERSONAL_ENTRY_AMOUNT_TOO_LARGE',
    'MONEY_NOT_ON_CURRENCY_STEP',
    'PERSONAL_ENTRY_CURRENCY_INVALID',
  ],
  date: ['PERSONAL_ENTRY_DATE_INVALID', 'PERSONAL_ENTRY_DATE_LOCKED'],
  note: ['PERSONAL_ENTRY_NOTE_INVALID'],
  category: ['PERSONAL_ENTRY_CATEGORY_INVALID'],
  ends: [],
}

const nothingChanged: Record<PersonalEntryFieldName, boolean> = {
  amount: false,
  date: false,
  note: false,
  category: false,
  ends: false,
}

export function usePersonalEntryForm(
  entry: PersonalEntry | undefined,
  start: PersonalEntryStart | undefined = entry,
): PersonalEntryFormState {
  const { t } = useTranslation()
  const me = useSignedInMe()
  const queryClient = useQueryClient()
  const sheet = useKvitSheet()
  const oneAtATime = useSaveOnce()
  const add = useAddPersonalEntry(oneAtATime)
  const edit = useEditPersonalEntry(oneAtATime)
  const remove = useDeletePersonalEntry(oneAtATime)
  const restore = useRestorePersonalEntry()
  const initialNote = start?.note ?? ''
  const [clientRequestId] = useState<string>(() => crypto.randomUUID())
  const [today] = useState<string>(() => todayInTimeZone(me.timeZone, new Date()))
  const [amountText, setAmountText] = useState<string>(
    start === undefined ? '' : moneyInputText(start.amountMinor, start.currency),
  )
  const [currency, setCurrency] = useState<Currency>(() => start?.currency ?? readLastEntryCurrency(me.id))
  const [entryDate, setEntryDate] = useState<string>(start?.entryDate ?? today)
  const [note, setNote] = useState<string>(initialNote)
  const [isNoteOpen, setIsNoteOpen] = useState<boolean>(initialNote !== '')
  const [isDateOpen, setIsDateOpen] = useState<boolean>(false)
  const [changedSinceSave, setChangedSinceSave] = useState<Record<PersonalEntryFieldName, boolean>>(nothingChanged)

  const amountMinor = parseMoneyInput(amountText, currency)
  const saveError = entry === undefined ? add.error : edit.error
  const shownError = saveError !== null && !hasErrorCode(saveError, entryNotFoundCode) ? saveError : null

  function markChanged(field: PersonalEntryFieldName): void {
    setChangedSinceSave((current) => ({ ...current, [field]: true }))
  }

  function closeBecauseGone(error: Error): void {
    toast.error(t(errorMessageKey(error)))
    void refreshPersonalEntries(queryClient)
    sheet.close()
  }

  function handleSaveRefusal(error: Error): void {
    if (hasErrorCode(error, entryNotFoundCode)) {
      closeBecauseGone(error)
    }
  }

  function closeAfterSave(savedCurrency: Currency): void {
    saveLastEntryCurrency(me.id, savedCurrency)
    toast.success(t('groupSettings.saved'))
    sheet.close()
  }

  function save(kindFields: PersonalEntryKindFields): void {
    if (amountMinor === null) {
      throw new Error(`Save was pressed without a valid amount ("${amountText}")`)
    }
    setChangedSinceSave(nothingChanged)
    const fields: PersonalEntryFields = { ...kindFields, amountMinor, currency, entryDate, note: textOrNull(note) }
    const onDone = (): void => closeAfterSave(fields.currency)
    if (entry === undefined) {
      add.run({ clientRequestId, ...fields }, onDone, handleSaveRefusal)
    } else {
      edit.run({ entryId: entry.id, fields }, onDone, handleSaveRefusal)
    }
  }

  function undoDelete(entryId: string): void {
    void restore
      .mutateAsync(entryId)
      .then(() => {
        toast.success(t('budget.entries.restored'))
      })
      .catch((error: unknown) => {
        toast.error(t(errorMessageKey(error)))
      })
  }

  function deleteEntry(deleted: PersonalEntry): void {
    remove.run(
      deleted.id,
      () => {
        showUndoToast(t('budget.entries.deleted'), {
          danger: true,
          undoLabel: t('common.undo'),
          onUndo: () => undoDelete(deleted.id),
        })
        sheet.close()
      },
      (error) => {
        if (hasErrorCode(error, entryNotFoundCode)) {
          closeBecauseGone(error)
          return
        }
        toast.error(t(errorMessageKey(error)))
      },
    )
  }

  return {
    entry,
    sheet,
    today,
    amountText,
    currency,
    amountMinor,
    entryDate,
    note,
    isNoteEmptyAtStart: initialNote === '',
    isNoteOpen,
    isDateOpen,
    isBusy: add.isPending || edit.isPending || remove.isPending,
    shownError,
    changeAmountText: (nextAmountText) => {
      setAmountText(nextAmountText)
      markChanged('amount')
    },
    changeCurrency: (nextCurrency) => {
      setCurrency(nextCurrency)
      markChanged('amount')
    },
    changeEntryDate: (nextEntryDate) => {
      setEntryDate(nextEntryDate)
      markChanged('date')
    },
    changeNote: (nextNote) => {
      setNote(nextNote)
      markChanged('note')
    },
    openNote: () => setIsNoteOpen(true),
    setIsDateOpen,
    markChanged,
    isChangedSinceSave: (field) => changedSinceSave[field],
    clearChangedMarks: () => setChangedSinceSave(nothingChanged),
    isRefused: (field) => refusedCodesOf[field].some((code) => hasErrorCode(shownError, code)) && !changedSinceSave[field],
    closeAfterSave,
    closeBecauseGone,
    save,
    deleteEntry,
  }
}
