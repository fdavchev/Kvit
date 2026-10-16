import { useSignedInMe } from '@/core/auth/useSignedInMe'
import { useSettlements } from '@/features/settlements/list/hooks/useSettlements'
import { useMembers } from '@/features/groups/members/hooks/useMembers'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitSkeleton } from '@/shared/components/KvitSkeleton'
import type { Currency } from '@/shared/utils/formatMoney'
import { GroupLoadError } from '../../shared/components/GroupLoadError'
import { balancesView } from '../balancesView'
import { useBalances } from '../hooks/useBalances'
import { BalancesOverview } from './BalancesOverview'

interface GroupBalancesProps {
  groupId: string
  defaultCurrency: Currency
  canChangePayments: boolean
}

export function GroupBalances({ groupId, defaultCurrency, canChangePayments }: GroupBalancesProps) {
  const me = useSignedInMe()
  const balancesQuery = useBalances(groupId)
  const settlementsQuery = useSettlements(groupId)
  const membersQuery = useMembers(groupId)

  if (balancesQuery.isError) {
    return <GroupLoadError error={balancesQuery.error} onRetry={() => void balancesQuery.refetch()} />
  }
  if (settlementsQuery.isError) {
    return <GroupLoadError error={settlementsQuery.error} onRetry={() => void settlementsQuery.refetch()} />
  }
  if (membersQuery.isError) {
    return <GroupLoadError error={membersQuery.error} onRetry={() => void membersQuery.refetch()} />
  }
  if (balancesQuery.isPending || settlementsQuery.isPending || membersQuery.isPending) {
    return (
      <KvitLoading>
        <div className="flex flex-col gap-2 pb-6">
          <KvitSkeleton className="mb-1.5 h-26 rounded-[20px]" />
          <KvitSkeleton className="h-16" />
          <KvitSkeleton className="h-16" />
        </div>
      </KvitLoading>
    )
  }
  const members = membersQuery.data.members
  return (
    <BalancesOverview
      groupId={groupId}
      view={balancesView(balancesQuery.data, settlementsQuery.data, members, me.id)}
      members={members}
      defaultCurrency={defaultCurrency}
      timeZone={me.timeZone}
      canChangePayments={canChangePayments}
    />
  )
}
