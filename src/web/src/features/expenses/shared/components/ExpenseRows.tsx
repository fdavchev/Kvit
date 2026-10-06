import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { Category } from '@/core/services/categories/categoriesService'
import { addDays, formatExpenseDay } from '@/shared/utils/formatDate'
import type { Currency } from '@/shared/utils/formatMoney'
import { categoryNameKey, categoryOf } from '../expenseCategories'
import { personLabel, personOf, type ExpensePerson } from '../expensePeople'
import { noValueText } from '../expenseTitle'
import type { SplitDraft } from '../splitDraft'
import { CategorySheet } from './CategorySheet'
import { DateSheet } from './DateSheet'
import { FormRow } from './FormRow'
import { PaidBySheet } from './PaidBySheet'
import { SplitSheet } from './SplitSheet'

interface ExpenseRowsProps {
  people: readonly ExpensePerson[]
  paidByMemberId: string
  splitDraft: SplitDraft
  expenseDate: string
  categoryId: string | null
  categories: readonly Category[]
  today: string
  amountMinor: number
  currency: Currency
  onPaidByChange: (memberId: string) => void
  onSplitDraftChange: (splitDraft: SplitDraft) => void
  onExpenseDateChange: (expenseDate: string) => void
  onCategoryChange: (categoryId: string) => void
}

type OpenSheet = 'paidBy' | 'split' | 'date' | 'category' | null

const splitTypeKeys = {
  Exact: 'expense.splitExact',
  Percentage: 'expense.splitPercentage',
  Shares: 'expense.splitShares',
} as const

export function ExpenseRows({
  people,
  paidByMemberId,
  splitDraft,
  expenseDate,
  categoryId,
  categories,
  today,
  amountMinor,
  currency,
  onPaidByChange,
  onSplitDraftChange,
  onExpenseDateChange,
  onCategoryChange,
}: ExpenseRowsProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const [openSheet, setOpenSheet] = useState<OpenSheet>(null)
  const category = categoryOf(categories, categoryId)

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
            people={people.filter((person) => person.isCurrentMember)}
            paidByMemberId={paidByMemberId}
            onPick={onPaidByChange}
            onClose={close}
          />
        )
      case 'split':
        return (
          <SplitSheet
            people={people}
            draft={splitDraft}
            amountMinor={amountMinor}
            currency={currency}
            language={language}
            onChange={onSplitDraftChange}
            onClose={close}
          />
        )
      case 'date':
        return <DateSheet expenseDate={expenseDate} today={today} onPick={onExpenseDateChange} onClose={close} />
      case 'category':
        return (
          <CategorySheet
            categories={categories}
            categoryId={categoryId}
            onPick={onCategoryChange}
            onClose={close}
          />
        )
      case null:
        return null
    }
  }

  return (
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
      {renderSheet()}
    </div>
  )
}
