import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { useSignedInMe } from '@/core/auth/useSignedInMe'
import { routes } from '@/core/router/routes'
import type { ExpenseChanges } from '@/core/services/expenses/expensesService'
import type { GroupMember } from '@/core/services/groups/membersService'
import { useGroup } from '@/features/groups/group/hooks/useGroup'
import { useMembers } from '@/features/groups/members/hooks/useMembers'
import { GroupLoadError } from '@/features/groups/shared/components/GroupLoadError'
import { useGroupIdParam } from '@/features/groups/shared/useGroupIdParam'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { todayInTimeZone } from '@/shared/utils/formatDate'
import { ExpenseForm } from '../../shared/components/ExpenseForm'
import { ExpenseFormTitle } from '../../shared/components/ExpenseFormTitle'
import { expensePeople } from '../../shared/expensePeople'
import { useCategories } from '../../shared/hooks/useCategories'
import { defaultSplitDraft } from '../../shared/splitDraft'
import { useCreateExpense } from '../hooks/useCreateExpense'

export function AddExpenseScreen() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const groupId = useGroupIdParam()
  const me = useSignedInMe()
  const groupQuery = useGroup(groupId)
  const membersQuery = useMembers(groupId)
  const categoriesQuery = useCategories()
  const createExpense = useCreateExpense(groupId)
  const [clientRequestId] = useState<string>(() => crypto.randomUUID())

  function save(changes: ExpenseChanges): void {
    createExpense.mutate(
      { clientRequestId, ...changes },
      {
        onSuccess: () => {
          toast.success(t('expenses.saved'))
          navigate(routes.group(groupId), { replace: true })
        },
      },
    )
  }

  function renderContent() {
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
    if (groupQuery.isPending || membersQuery.isPending || categoriesQuery.isPending) {
      return <KvitLoading />
    }
    const members = membersQuery.data.members
    const today = todayInTimeZone(me.timeZone, new Date())
    return (
      <ExpenseForm
        initialValues={{
          amountText: '',
          currency: groupQuery.data.defaultCurrency,
          paidByMemberId: ownRowOf(members).id,
          splitDraft: defaultSplitDraft(members.map((member) => member.id)),
          expenseDate: today,
          categoryId: null,
          title: '',
          note: '',
        }}
        people={expensePeople(members)}
        categories={categoriesQuery.data}
        today={today}
        isPending={createExpense.isPending}
        errorMessage={createExpense.isError ? t(errorMessageKey(createExpense.error)) : null}
        onSubmit={save}
      />
    )
  }

  return (
    <KvitScreen>
      <div className="flex min-h-12">
        <KvitBackButton to={routes.group(groupId)} />
      </div>
      <ExpenseFormTitle>{t('expense.addTitle')}</ExpenseFormTitle>
      {renderContent()}
    </KvitScreen>
  )
}

function ownRowOf(members: readonly GroupMember[]): GroupMember {
  const ownRow = members.find((member) => member.isYou)
  if (ownRow === undefined) {
    throw new Error('Expected the signed-in person to be one of the members of the group, found none')
  }
  return ownRow
}
