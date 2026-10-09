import { useTranslation } from 'react-i18next'
import { useSignedInMe } from '@/core/auth/useSignedInMe'
import { useLanguage } from '@/core/i18n/useLanguage'
import { routes } from '@/core/router/routes'
import type { GroupMember } from '@/core/services/groups/membersService'
import { neutralColorIndex } from '@/features/expenses/shared/expensePeople'
import { useCategories } from '@/features/expenses/shared/hooks/useCategories'
import { useExpenses } from '@/features/expenses/list/hooks/useExpenses'
import { useMembers } from '@/features/groups/members/hooks/useMembers'
import { KvitEmpty } from '@/shared/components/KvitEmpty'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { formatHowLongAgo, todayInTimeZone } from '@/shared/utils/formatDate'
import { GroupLoadError } from '../../shared/components/GroupLoadError'
import { activityLines } from '../activityLines'
import { useActivity } from '../hooks/useActivity'
import { ActivityRow } from './ActivityRow'

interface GroupActivityProps {
  groupId: string
}

interface ActorLook {
  pictureUrl: string | null
  colorIndex: number
}

export function GroupActivity({ groupId }: GroupActivityProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const me = useSignedInMe()
  const activityQuery = useActivity(groupId)
  const membersQuery = useMembers(groupId)
  const expensesQuery = useExpenses(groupId)
  const categoriesQuery = useCategories()

  if (activityQuery.isError) {
    return <GroupLoadError error={activityQuery.error} onRetry={() => void activityQuery.refetch()} />
  }
  if (membersQuery.isError) {
    return <GroupLoadError error={membersQuery.error} onRetry={() => void membersQuery.refetch()} />
  }
  if (expensesQuery.isError) {
    return <GroupLoadError error={expensesQuery.error} onRetry={() => void expensesQuery.refetch()} />
  }
  if (categoriesQuery.isError) {
    return <GroupLoadError error={categoriesQuery.error} onRetry={() => void categoriesQuery.refetch()} />
  }
  if (
    activityQuery.isPending ||
    membersQuery.isPending ||
    expensesQuery.isPending ||
    categoriesQuery.isPending
  ) {
    return <KvitLoading />
  }
  if (activityQuery.data.length === 0) {
    return <KvitEmpty message={t('activity.empty')} />
  }
  const now = new Date()
  const members = membersQuery.data.members
  const lines = activityLines(activityQuery.data, {
    t,
    language,
    today: todayInTimeZone(me.timeZone, now),
    categories: categoriesQuery.data,
    expenses: expensesQuery.data,
  })
  return (
    <ul className="flex flex-col pb-6">
      {lines.map((line) => {
        const look = actorLookOf(members, line.actorUserId)
        return (
          <ActivityRow
            key={line.key}
            actorName={line.actorName}
            pictureUrl={look.pictureUrl}
            colorIndex={look.colorIndex}
            sentence={line.sentence}
            expenseTitle={line.expenseTitle}
            when={formatHowLongAgo(line.createdAt, now, me.timeZone, language, t)}
            linkTo={line.linkedExpenseId === null ? null : routes.groupExpense(groupId, line.linkedExpenseId)}
          />
        )
      })}
    </ul>
  )
}

function actorLookOf(members: readonly GroupMember[], actorUserId: string): ActorLook {
  const index = members.findIndex((member) => member.userId === actorUserId)
  if (index === -1) {
    return { pictureUrl: null, colorIndex: neutralColorIndex }
  }
  return { pictureUrl: members[index].pictureUrl, colorIndex: index }
}
