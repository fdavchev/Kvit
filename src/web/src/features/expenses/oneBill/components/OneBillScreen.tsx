import { useId, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { useSignedInMe } from '@/core/auth/useSignedInMe'
import { useLanguage } from '@/core/i18n/useLanguage'
import { routes } from '@/core/router/routes'
import type { Category } from '@/core/services/categories/categoriesService'
import type { Me } from '@/core/services/me/meService'
import { GroupLoadError } from '@/features/groups/shared/components/GroupLoadError'
import { groupNameMaxLength } from '@/features/groups/shared/groupName'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitForm } from '@/shared/components/KvitForm'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitTextField } from '@/shared/components/KvitTextField'
import { formatDayWithoutYear, todayInTimeZone } from '@/shared/utils/formatDate'
import type { Currency } from '@/shared/utils/formatMoney'
import { parseMoneyInput } from '@/shared/utils/parseMoneyInput'
import { AmountField } from '../../shared/components/AmountField'
import { ExpenseFormTitle } from '../../shared/components/ExpenseFormTitle'
import { ExpenseRows } from '../../shared/components/ExpenseRows'
import type { ExpensePerson } from '../../shared/expensePeople'
import { useCategories } from '../../shared/hooks/useCategories'
import {
  defaultSplitDraft,
  splitProgress,
  withAutoFillRefreshed,
  withPersonAdded,
  withSplitType,
  withoutPerson,
  type SplitDraft,
} from '../../shared/splitDraft'
import { useCreateOneBill } from '../hooks/useCreateOneBill'
import { buildOneBillRequest, type BillName } from '../oneBillRequest'
import { BillNameSheet } from './BillNameSheet'
import { BillPeople } from './BillPeople'

interface OneBillFormProps {
  me: Me
  categories: readonly Category[]
}

const defaultBillCurrency: Currency = 'MKD'

export function OneBillScreen() {
  const { t } = useTranslation()
  const me = useSignedInMe()
  const categoriesQuery = useCategories()

  function renderContent() {
    if (categoriesQuery.isError) {
      return (
        <GroupLoadError
          error={categoriesQuery.error}
          onRetry={() => void categoriesQuery.refetch()}
        />
      )
    }
    if (categoriesQuery.isPending) {
      return <KvitLoading />
    }
    return <OneBillForm me={me} categories={categoriesQuery.data} />
  }

  return (
    <KvitScreen>
      <div className="flex min-h-12">
        <KvitBackButton to={routes.newGroup} />
      </div>
      <ExpenseFormTitle>{t('newGroup.oneBill')}</ExpenseFormTitle>
      {renderContent()}
    </KvitScreen>
  )
}

function OneBillForm({ me, categories }: OneBillFormProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const navigate = useNavigate()
  const titleId = useId()
  const createOneBill = useCreateOneBill()
  const [clientRequestId] = useState<string>(() => crypto.randomUUID())
  const today = todayInTimeZone(me.timeZone, new Date())
  const [amountText, setAmountText] = useState<string>('')
  const [currency, setCurrency] = useState<Currency>(defaultBillCurrency)
  const [names, setNames] = useState<readonly BillName[]>([])
  const [paidById, setPaidById] = useState<string>(me.id)
  const [splitDraft, setSplitDraft] = useState<SplitDraft>(() => defaultSplitDraft([me.id]))
  const [expenseDate, setExpenseDate] = useState<string>(today)
  const [categoryId, setCategoryId] = useState<string | null>(null)
  const [title, setTitle] = useState<string>('')
  const [isTitleTooLong, setIsTitleTooLong] = useState<boolean>(false)
  const [isAddingName, setIsAddingName] = useState<boolean>(false)

  const amountMinor = parseMoneyInput(amountText, currency)
  const progress = splitProgress(splitDraft, amountMinor ?? 0, currency)
  const canSubmit: boolean = amountMinor !== null && progress.kind === 'ok'
  const people: ExpensePerson[] = [
    {
      memberId: me.id,
      name: me.displayName,
      pictureUrl: me.pictureUrl,
      colorIndex: 0,
      isYou: true,
      isCurrentMember: true,
    },
    ...names.map((billName, index) => ({
      memberId: billName.id,
      name: billName.name,
      pictureUrl: null,
      colorIndex: index + 1,
      isYou: false,
      isCurrentMember: true,
    })),
  ]

  function changeCurrency(nextCurrency: Currency): void {
    setCurrency(nextCurrency)
    setSplitDraft(withSplitType(splitDraft, splitDraft.splitType))
  }

  function changeAmountText(nextAmountText: string): void {
    setAmountText(nextAmountText)
    setSplitDraft(withAutoFillRefreshed(splitDraft, parseMoneyInput(nextAmountText, currency) ?? 0, currency))
  }

  function addName(name: string): void {
    const id = crypto.randomUUID()
    setNames([...names, { id, name }])
    setSplitDraft(withPersonAdded(splitDraft, id))
  }

  function removeName(id: string): void {
    setNames(names.filter((billName) => billName.id !== id))
    setSplitDraft(withoutPerson(splitDraft, id))
    if (paidById === id) {
      setPaidById(me.id)
    }
  }

  function submit(): void {
    if (amountMinor === null) {
      throw new Error(`Save bill was pressed with an amount that is not valid: "${amountText}"`)
    }
    if (title.trim().length > groupNameMaxLength) {
      setIsTitleTooLong(true)
      createOneBill.reset()
      return
    }
    setIsTitleTooLong(false)
    createOneBill.mutate(
      buildOneBillRequest({
        clientRequestId,
        ownId: me.id,
        names,
        paidById,
        splitDraft,
        title,
        groupName: t('oneBill.groupName', { date: formatDayWithoutYear(expenseDate, language) }),
        amountMinor,
        currency,
        expenseDate,
        categoryId,
      }),
      {
        onSuccess: (created) => {
          toast.success(t('expenses.saved'))
          navigate(routes.group(created.groupId), { replace: true })
        },
      },
    )
  }

  function errorMessage(): string | null {
    if (isTitleTooLong) {
      return t('oneBill.titleTooLong')
    }
    return createOneBill.isError ? t(errorMessageKey(createOneBill.error)) : null
  }

  return (
    <>
      <KvitForm
        submitLabel={t('oneBill.save')}
        isPending={createOneBill.isPending}
        canSubmit={canSubmit}
        errorMessage={errorMessage()}
        onSubmit={submit}
      >
        <AmountField
          amountText={amountText}
          currency={currency}
          onAmountTextChange={changeAmountText}
          onCurrencyChange={changeCurrency}
        />
        <BillPeople people={people} onRemove={removeName} onAddName={() => setIsAddingName(true)} />
        <ExpenseRows
          people={people}
          paidByMemberId={paidById}
          splitDraft={splitDraft}
          expenseDate={expenseDate}
          categoryId={categoryId}
          categories={categories}
          today={today}
          amountMinor={amountMinor ?? 0}
          currency={currency}
          onPaidByChange={setPaidById}
          onSplitDraftChange={setSplitDraft}
          onExpenseDateChange={setExpenseDate}
          onCategoryChange={setCategoryId}
        />
        <KvitTextField
          id={titleId}
          label={t('expense.titleField')}
          value={title}
          onChange={setTitle}
          invalid={isTitleTooLong}
        />
      </KvitForm>
      {isAddingName && (
        <BillNameSheet
          takenNames={[me.displayName, ...names.map((billName) => billName.name)]}
          onAdd={addName}
          onClose={() => setIsAddingName(false)}
        />
      )}
    </>
  )
}
