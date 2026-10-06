import { useId, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { Category } from '@/core/services/categories/categoriesService'
import type { ExpenseChanges } from '@/core/services/expenses/expensesService'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitForm } from '@/shared/components/KvitForm'
import { KvitTextField } from '@/shared/components/KvitTextField'
import { addDays, formatExpenseDay } from '@/shared/utils/formatDate'
import type { Currency } from '@/shared/utils/formatMoney'
import { parseMoneyInput } from '@/shared/utils/parseMoneyInput'
import { categoryNameKey, categoryOf } from '../expenseCategories'
import { personLabel, personOf, type ExpensePerson } from '../expensePeople'
import { noValueText } from '../expenseTitle'
import {
  buildSplitRequest,
  splitProgress,
  withSplitType,
  type SplitDraft,
} from '../splitDraft'
import { CategorySheet } from './CategorySheet'
import { DateSheet } from './DateSheet'
import { FormRow } from './FormRow'
import { PaidBySheet } from './PaidBySheet'
import { SplitSheet } from './SplitSheet'

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

type OpenSheet = 'paidBy' | 'split' | 'date' | 'category' | null

const otherCurrency: Record<Currency, Currency> = { MKD: 'EUR', EUR: 'MKD' }

const splitTypeKeys = {
  Exact: 'expense.splitExact',
  Percentage: 'expense.splitPercentage',
  Shares: 'expense.splitShares',
} as const

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
  const { language } = useLanguage()
  const amountId = useId()
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
  const [openSheet, setOpenSheet] = useState<OpenSheet>(null)

  const amountMinor = parseMoneyInput(amountText, currency)
  const progress = splitProgress(splitDraft, amountMinor ?? 0, currency)
  const canSubmit: boolean = amountMinor !== null && progress.kind === 'ok'
  const category = categoryOf(categories, categoryId)
  const currentPeople = people.filter((person) => person.isCurrentMember)

  function switchCurrency(): void {
    setCurrency(otherCurrency[currency])
    setSplitDraft(withSplitType(splitDraft, splitDraft.splitType))
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

  function dateText(): string {
    if (expenseDate === today) {
      return t('expenses.today')
    }
    if (expenseDate === addDays(today, -1)) {
      return t('expenses.yesterday')
    }
    return formatExpenseDay(expenseDate, today, language)
  }

  function splitText(): string {
    if (splitDraft.splitType !== 'Equal') {
      return t(splitTypeKeys[splitDraft.splitType])
    }
    const peopleIn = splitDraft.people.filter((person) => person.isIn)
    const who =
      peopleIn.length === splitDraft.people.length
        ? t('expense.everyone')
        : peopleIn.map((person) => personOf(people, person.memberId).name).join(', ') || noValueText
    return `${t('expense.equally')} · ${who}`
  }

  function renderSheet() {
    const close = (): void => setOpenSheet(null)
    switch (openSheet) {
      case 'paidBy':
        return (
          <PaidBySheet
            people={currentPeople}
            paidByMemberId={paidByMemberId}
            onPick={setPaidByMemberId}
            onClose={close}
          />
        )
      case 'split':
        return (
          <SplitSheet
            people={people}
            draft={splitDraft}
            amountMinor={amountMinor ?? 0}
            currency={currency}
            language={language}
            onChange={setSplitDraft}
            onClose={close}
          />
        )
      case 'date':
        return <DateSheet expenseDate={expenseDate} today={today} onPick={setExpenseDate} onClose={close} />
      case 'category':
        return (
          <CategorySheet
            categories={categories}
            categoryId={categoryId}
            onPick={setCategoryId}
            onClose={close}
          />
        )
      case null:
        return null
    }
  }

  return (
    <>
      <KvitForm
        submitLabel={t('groupSettings.save')}
        isPending={isPending}
        canSubmit={canSubmit}
        errorMessage={errorMessage}
        onSubmit={submit}
      >
        <div className="flex items-center justify-center gap-2 pt-2">
          <label htmlFor={amountId} className="sr-only">
            {t('expense.amount')}
          </label>
          <input
            id={amountId}
            type="text"
            inputMode={currency === 'EUR' ? 'decimal' : 'numeric'}
            autoComplete="off"
            autoFocus
            placeholder="0"
            value={amountText}
            onChange={(event) => setAmountText(event.target.value)}
            className="w-full min-w-0 flex-1 rounded-[14px] bg-transparent py-1 text-right text-5xl font-extrabold tracking-[-0.02em] text-foreground placeholder:text-muted-foreground/60 focus-visible:outline-offset-0"
          />
          <button
            type="button"
            aria-label={t('expense.currencyToggle', { currency })}
            onClick={switchCurrency}
            className="pressable min-h-11 flex-none rounded-full border border-field-border bg-field px-3 text-lg font-bold text-muted-foreground"
          >
            {currency}
          </button>
        </div>
        <div className="flex flex-col gap-2.5">
          <FormRow
            label={t('expense.paidBy')}
            value={personLabel(personOf(people, paidByMemberId), t)}
            onOpen={() => setOpenSheet('paidBy')}
          />
          <FormRow label={t('expense.split')} value={splitText()} onOpen={() => setOpenSheet('split')} />
          <FormRow label={t('expense.date')} value={dateText()} onOpen={() => setOpenSheet('date')} />
          <FormRow
            label={t('expense.category')}
            value={category === null ? noValueText : `${category.emoji} ${t(categoryNameKey(category))}`}
            onOpen={() => setOpenSheet('category')}
          />
        </div>
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
          <KvitButton type="button" variant="smallLink" className="w-auto self-start px-0" onClick={() => setIsNoteOpen(true)}>
            {t('expense.note')}
          </KvitButton>
        )}
      </KvitForm>
      {renderSheet()}
    </>
  )
}

function textOrNull(text: string): string | null {
  const trimmed = text.trim()
  return trimmed === '' ? null : trimmed
}
