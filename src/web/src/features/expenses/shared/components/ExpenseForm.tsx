import { useId, useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { Category } from '@/core/services/categories/categoriesService'
import type { ExpenseChanges } from '@/core/services/expenses/expensesService'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitForm } from '@/shared/components/KvitForm'
import { KvitTextField } from '@/shared/components/KvitTextField'
import type { Currency } from '@/shared/utils/formatMoney'
import { parseMoneyInput } from '@/shared/utils/parseMoneyInput'
import type { ExpensePerson } from '../expensePeople'
import { textOrNull } from '../expenseTitle'
import {
  buildSplitRequest,
  splitProgress,
  withAutoFillRefreshed,
  withSplitType,
  type SplitDraft,
} from '../splitDraft'
import { AmountField } from './AmountField'
import { ExpenseRows } from './ExpenseRows'

export interface ExpenseFormValues {
  amountText: string
  currency: Currency
  paidByMemberId: string
  splitDraft: SplitDraft
  expenseDate: string
  categoryId: string | null
  title: string
  note: string
}

interface ExpenseFormProps {
  initialValues: ExpenseFormValues
  people: readonly ExpensePerson[]
  categories: readonly Category[]
  today: string
  isPending: boolean
  errorMessage: string | null
  onSubmit: (changes: ExpenseChanges) => void
}

export function ExpenseForm({
  initialValues,
  people,
  categories,
  today,
  isPending,
  errorMessage,
  onSubmit,
}: ExpenseFormProps) {
  const { t } = useTranslation()
  const noteId = useId()
  const titleId = useId()
  const [amountText, setAmountText] = useState<string>(initialValues.amountText)
  const [currency, setCurrency] = useState<Currency>(initialValues.currency)
  const [paidByMemberId, setPaidByMemberId] = useState<string>(initialValues.paidByMemberId)
  const [splitDraft, setSplitDraft] = useState<SplitDraft>(initialValues.splitDraft)
  const [expenseDate, setExpenseDate] = useState<string>(initialValues.expenseDate)
  const [categoryId, setCategoryId] = useState<string | null>(initialValues.categoryId)
  const [title, setTitle] = useState<string>(initialValues.title)
  const [note, setNote] = useState<string>(initialValues.note)
  const [isNoteOpen, setIsNoteOpen] = useState<boolean>(initialValues.note !== '')

  const amountMinor = parseMoneyInput(amountText, currency)
  const progress = splitProgress(splitDraft, amountMinor ?? 0, currency)
  const canSubmit: boolean = amountMinor !== null && progress.kind === 'ok'

  function changeCurrency(nextCurrency: Currency): void {
    setCurrency(nextCurrency)
    setSplitDraft(withSplitType(splitDraft, splitDraft.splitType))
  }

  function changeAmountText(nextAmountText: string): void {
    setAmountText(nextAmountText)
    setSplitDraft(withAutoFillRefreshed(splitDraft, parseMoneyInput(nextAmountText, currency) ?? 0, currency))
  }

  function submit(): void {
    if (amountMinor === null) {
      throw new Error(`Save was pressed with an amount that is not valid: "${amountText}"`)
    }
    onSubmit({
      title: textOrNull(title),
      note: textOrNull(note),
      amountMinor,
      currency,
      expenseDate,
      categoryId,
      paidByMemberId,
      splitType: splitDraft.splitType,
      shares: buildSplitRequest(splitDraft, currency),
    })
  }

  return (
    <KvitForm
      submitLabel={t('groupSettings.save')}
      isPending={isPending}
      canSubmit={canSubmit}
      errorMessage={errorMessage}
      onSubmit={submit}
    >
      <AmountField
        amountText={amountText}
        currency={currency}
        onAmountTextChange={changeAmountText}
        onCurrencyChange={changeCurrency}
      />
      <ExpenseRows
        people={people}
        paidByMemberId={paidByMemberId}
        splitDraft={splitDraft}
        expenseDate={expenseDate}
        categoryId={categoryId}
        categories={categories}
        today={today}
        amountMinor={amountMinor ?? 0}
        currency={currency}
        onPaidByChange={setPaidByMemberId}
        onSplitDraftChange={setSplitDraft}
        onExpenseDateChange={setExpenseDate}
        onCategoryChange={setCategoryId}
      />
      <KvitTextField id={titleId} label={t('expense.titleField')} value={title} onChange={setTitle} />
      {isNoteOpen ? (
        <div className="flex flex-col">
          <label htmlFor={noteId} className="mb-2 text-[0.9375rem] font-bold">
            {t('expense.note')}
          </label>
          <textarea
            id={noteId}
            value={note}
            rows={3}
            onChange={(event) => setNote(event.target.value)}
            className="min-h-24 w-full rounded-[14px] border-2 border-field-border bg-field px-4 py-3 text-[1.0625rem] font-semibold text-field-foreground focus:border-field-border-focus focus-visible:outline-offset-0"
          />
        </div>
      ) : (
        <KvitButton type="button" variant="smallLink" className="min-h-11 w-auto min-w-11 self-start px-0" onClick={() => setIsNoteOpen(true)}>
          {t('expense.note')}
        </KvitButton>
      )}
    </KvitForm>
  )
}
