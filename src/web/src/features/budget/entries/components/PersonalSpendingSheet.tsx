import { useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey, hasErrorCode } from '@/core/api/errors'
import { useSignedInMe } from '@/core/auth/useSignedInMe'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { PersonalCategory } from '@/core/services/personalCategories/personalCategoriesService'
import type {
  PersonalEntry,
  PersonalEntryFields,
} from '@/core/services/personalEntries/personalEntriesService'
import { AmountField } from '@/features/expenses/shared/components/AmountField'
import { DateSheet } from '@/features/expenses/shared/components/DateSheet'
import { NoteField } from '@/features/expenses/shared/components/NoteField'
import { expenseDayText } from '@/features/expenses/shared/expenseDayText'
import { textOrNull } from '@/features/expenses/shared/expenseTitle'
import { KvitButton } from '@/shared/components/KvitButton'
import { kvitChipLook } from '@/shared/components/kvitChipLook'
import { KvitForm } from '@/shared/components/KvitForm'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { useKvitSheet } from '@/shared/components/useKvitSheet'
import { showUndoToast } from '@/shared/toasts/showUndoToast'
import { cn } from '@/shared/utils/cn'
import { todayInTimeZone } from '@/shared/utils/formatDate'
import type { Currency } from '@/shared/utils/formatMoney'
import { moneyInputText, parseMoneyInput } from '@/shared/utils/parseMoneyInput'
import { PersonalCategoryGrid } from '../../categories/components/PersonalCategoryGrid'
import { useAddPersonalEntry } from '../hooks/useAddPersonalEntry'
import { useDeletePersonalEntry } from '../hooks/useDeletePersonalEntry'
import { useEditPersonalEntry } from '../hooks/useEditPersonalEntry'
import { useRestorePersonalEntry } from '../hooks/useRestorePersonalEntry'
import { readLastEntryCurrency, saveLastEntryCurrency } from '../lastEntryCurrency'
import { refreshPersonalEntries } from '../personalEntryCache'
import { SpendingCategoryHeading } from './SpendingCategoryHeading'

interface PersonalSpendingSheetProps {
  entry?: PersonalEntry
  onClose: () => void
}

type FieldName = 'amount' | 'date' | 'note' | 'category'

const entryNotFoundCode = 'PERSONAL_ENTRY_NOT_FOUND'

const refusedCodesOf: Record<FieldName, readonly string[]> = {
  amount: ['EXPENSE_AMOUNT_NOT_POSITIVE', 'EXPENSE_AMOUNT_TOO_LARGE', 'MONEY_NOT_ON_CURRENCY_STEP', 'EXPENSE_CURRENCY_INVALID'],
  date: ['PERSONAL_ENTRY_DATE_INVALID'],
  note: ['EXPENSE_NOTE_INVALID'],
  category: ['PERSONAL_ENTRY_CATEGORY_INVALID'],
}

const nothingChanged: Record<FieldName, boolean> = { amount: false, date: false, note: false, category: false }

const chipLook = cn(kvitChipLook, 'gap-1.5 aria-invalid:border-destructive')

export function PersonalSpendingSheet({ entry, onClose }: PersonalSpendingSheetProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const me = useSignedInMe()
  const queryClient = useQueryClient()
  const sheet = useKvitSheet()
  const add = useAddPersonalEntry()
  const edit = useEditPersonalEntry()
  const remove = useDeletePersonalEntry()
  const restore = useRestorePersonalEntry()
  const initialNote = entry?.note ?? ''
  const [clientRequestId] = useState<string>(() => crypto.randomUUID())
  const [today] = useState<string>(() => todayInTimeZone(me.timeZone, new Date()))
  const [category, setCategory] = useState<PersonalCategory | null>(entry?.category ?? null)
  const [isPickingCategory, setIsPickingCategory] = useState<boolean>(false)
  const [amountText, setAmountText] = useState<string>(
    entry === undefined ? '' : moneyInputText(entry.amountMinor, entry.currency),
  )
  const [currency, setCurrency] = useState<Currency>(() => entry?.currency ?? readLastEntryCurrency(me.id))
  const [entryDate, setEntryDate] = useState<string>(entry?.entryDate ?? today)
  const [note, setNote] = useState<string>(initialNote)
  const [isNoteOpen, setIsNoteOpen] = useState<boolean>(initialNote !== '')
  const [isDateOpen, setIsDateOpen] = useState<boolean>(false)
  const [changedSinceSave, setChangedSinceSave] = useState<Record<FieldName, boolean>>(nothingChanged)

  const amountMinor = parseMoneyInput(amountText, currency)
  const saveError = entry === undefined ? add.error : edit.error
  const shownError = saveError !== null && !hasErrorCode(saveError, entryNotFoundCode) ? saveError : null

  function markChanged(field: FieldName): void {
    setChangedSinceSave((current) => ({ ...current, [field]: true }))
  }

  function isRefused(field: FieldName): boolean {
    return refusedCodesOf[field].some((code) => hasErrorCode(shownError, code)) && !changedSinceSave[field]
  }

  function pickCategory(picked: PersonalCategory): void {
    setCategory(picked)
    setIsPickingCategory(false)
    markChanged('category')
  }

  function closeBecauseEntryIsGone(error: Error): void {
    toast.error(t(errorMessageKey(error)))
    void refreshPersonalEntries(queryClient)
    sheet.close()
  }

  function finishSave(savedCurrency: Currency): void {
    saveLastEntryCurrency(me.id, savedCurrency)
    toast.success(t('groupSettings.saved'))
    sheet.close()
  }

  function handleSaveRefusal(error: Error): void {
    if (hasErrorCode(error, entryNotFoundCode)) {
      closeBecauseEntryIsGone(error)
    }
  }

  function submit(): void {
    if (amountMinor === null || category === null) {
      throw new Error(`Save was pressed without a valid amount ("${amountText}") or a category`)
    }
    setChangedSinceSave(nothingChanged)
    const fields: PersonalEntryFields = {
      categoryId: category.id,
      amountMinor,
      currency,
      entryDate,
      note: textOrNull(note),
    }
    const onDone = (): void => finishSave(fields.currency)
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

  function deleteNow(deleted: PersonalEntry): void {
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
          closeBecauseEntryIsGone(error)
          return
        }
        toast.error(t(errorMessageKey(error)))
      },
    )
  }

  function renderDetails() {
    return (
      <KvitForm
        submitLabel={t('groupSettings.save')}
        isPending={add.isPending || edit.isPending}
        canSubmit={amountMinor !== null}
        errorMessage={shownError === null ? null : t(errorMessageKey(shownError))}
        onSubmit={submit}
        footer={
          entry !== undefined && (
            <KvitButton variant="danger" disabled={remove.isPending} onClick={() => deleteNow(entry)}>
              {t('expense.delete')}
            </KvitButton>
          )
        }
      >
        <AmountField
          amountText={amountText}
          currency={currency}
          invalid={isRefused('amount')}
          onAmountTextChange={(nextAmountText) => {
            setAmountText(nextAmountText)
            markChanged('amount')
          }}
          onCurrencyChange={(nextCurrency) => {
            setCurrency(nextCurrency)
            markChanged('amount')
          }}
        />
        <div className="flex flex-wrap gap-2">
          <button type="button" aria-invalid={isRefused('date')} className={chipLook} onClick={() => setIsDateOpen(true)}>
            <span aria-hidden="true">📅</span>
            {expenseDayText(entryDate, today, language, t)}
          </button>
          {!isNoteOpen && (
            <button type="button" className={chipLook} onClick={() => setIsNoteOpen(true)}>
              <span aria-hidden="true">📝</span>
              {t('expense.note')}
            </button>
          )}
          {entry !== undefined && (
            <button
              type="button"
              aria-invalid={isRefused('category')}
              className={chipLook}
              onClick={() => setIsPickingCategory(true)}
            >
              <span aria-hidden="true">🏷️</span>
              {t('expense.category')}
            </button>
          )}
        </div>
        {isNoteOpen && (
          <NoteField
            value={note}
            invalid={isRefused('note')}
            autoFocus={initialNote === ''}
            onChange={(nextNote) => {
              setNote(nextNote)
              markChanged('note')
            }}
          />
        )}
      </KvitForm>
    )
  }

  const isShowingGrid = category === null || isPickingCategory

  return (
    <KvitSheet
      title={
        isShowingGrid ? (
          t('budget.entries.pickCategory')
        ) : (
          <SpendingCategoryHeading category={category} invalid={isRefused('category') && entry === undefined} />
        )
      }
      onClose={onClose}
      actionsRef={sheet.actionsRef}
    >
      {isShowingGrid ? <PersonalCategoryGrid onPick={pickCategory} /> : renderDetails()}
      {isDateOpen && (
        <DateSheet
          expenseDate={entryDate}
          today={today}
          onPick={(pickedDate) => {
            setEntryDate(pickedDate)
            markChanged('date')
          }}
          onClose={() => setIsDateOpen(false)}
        />
      )}
    </KvitSheet>
  )
}
