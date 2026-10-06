import { useTranslation } from 'react-i18next'
import { Navigate, useNavigate } from 'react-router'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { useSignedInMe } from '@/core/auth/useSignedInMe'
import { routes } from '@/core/router/routes'
import type { ExpenseChanges, ExpenseDetail } from '@/core/services/expenses/expensesService'
import type { GroupMember } from '@/core/services/groups/membersService'
import { useGroup } from '@/features/groups/group/hooks/useGroup'
import { useMembers } from '@/features/groups/members/hooks/useMembers'
import { GroupLoadError } from '@/features/groups/shared/components/GroupLoadError'
import { useGroupIdParam } from '@/features/groups/shared/useGroupIdParam'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { todayInTimeZone } from '@/shared/utils/formatDate'
import { moneyInputText } from '@/shared/utils/parseMoneyInput'
import { ExpenseForm, type ExpenseFormValues } from '../../shared/components/ExpenseForm'
import { ExpenseFormTitle } from '../../shared/components/ExpenseFormTitle'
import { ExpenseLoadError } from '../../shared/components/ExpenseLoadError'
import { expensePeople, type ExpensePerson } from '../../shared/expensePeople'
import { useCategories } from '../../shared/hooks/useCategories'
import { useExpense } from '../../shared/hooks/useExpense'
import { draftFromShares } from '../../shared/splitDraft'
import { useExpenseIdParam } from '../../shared/useExpenseIdParam'
import { useUpdateExpense } from '../hooks/useUpdateExpense'

export function EditExpenseScreen() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const groupId = useGroupIdParam()
  const expenseId = useExpenseIdParam()
  const me = useSignedInMe()
  const expenseQuery = useExpense(groupId, expenseId)
  const groupQuery = useGroup(groupId)
  const membersQuery = useMembers(groupId)
  const categoriesQuery = useCategories()
  const updateExpense = useUpdateExpense(groupId, expenseId)
  const detailRoute = routes.groupExpense(groupId, expenseId)

  function save(changes: ExpenseChanges): void {
    updateExpense.mutate(changes, {
      onSuccess: () => {
        toast.success(t('expenses.saved'))
        navigate(detailRoute, { replace: true })
      },
    })
  }

  function renderContent() {
    if (expenseQuery.isError) {
      return (
        <ExpenseLoadError error={expenseQuery.error} onRetry={() => void expenseQuery.refetch()} />
      )
    }
    if (groupQuery.isError) {
      return <GroupLoadError error={groupQuery.error} onRetry={() => void groupQuery.refetch()} />
    }
    if (membersQuery.isError) {
      return <GroupLoadError error={membersQuery.error} onRetry={() => void membersQuery.refetch()} />
    }
    if (categoriesQuery.isError) {
      return (
        <GroupLoadError
          error={categoriesQuery.error}
          onRetry={() => void categoriesQuery.refetch()}
        />
      )
    }
    if (
      expenseQuery.isPending ||
      groupQuery.isPending ||
      membersQuery.isPending ||
      categoriesQuery.isPending
    ) {
      return <KvitLoading />
    }
    const expense = expenseQuery.data
    if (!expense.canEdit) {
      return <Navigate to={detailRoute} replace />
    }
    const people = peopleOf(expense, membersQuery.data.members)
    return (
      <ExpenseForm
        initialValues={formValuesOf(expense, people)}
        people={people}
        categories={categoriesQuery.data}
        today={todayInTimeZone(me.timeZone, new Date())}
        isPending={updateExpense.isPending}
        errorMessage={updateExpense.isError ? t(errorMessageKey(updateExpense.error)) : null}
        onSubmit={save}
      />
    )
  }

  return (
    <KvitScreen>
      <div className="flex min-h-12">
        <KvitBackButton to={detailRoute} />
      </div>
      <ExpenseFormTitle>{t('expense.editTitle')}</ExpenseFormTitle>
      {renderContent()}
    </KvitScreen>
  )
}

function peopleOf(expense: ExpenseDetail, members: readonly GroupMember[]): ExpensePerson[] {
  return expensePeople(members, [
    ...expense.shares.map((share) => ({ memberId: share.memberId, name: share.name })),
    { memberId: expense.paidByMemberId, name: expense.paidByName },
  ])
}

function formValuesOf(expense: ExpenseDetail, people: readonly ExpensePerson[]): ExpenseFormValues {
  return {
    amountText: moneyInputText(expense.amountMinor, expense.currency),
    currency: expense.currency,
    paidByMemberId: expense.paidByMemberId,
    splitDraft: draftFromShares(
      expense.splitType,
      people.map((person) => person.memberId),
      expense.shares,
      expense.currency,
    ),
    expenseDate: expense.expenseDate,
    categoryId: expense.categoryId,
    title: expense.title ?? '',
    note: expense.note ?? '',
  }
}
