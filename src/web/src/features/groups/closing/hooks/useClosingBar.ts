import type { Group } from '@/core/services/groups/groupsService'
import { useMembers } from '../../members/hooks/useMembers'
import { closingBarView, needsMembersForClosingBar, type ClosingBarView } from '../closingBarView'

export function useClosingBar(group: Group): ClosingBarView | null {
  const needsMembers = needsMembersForClosingBar(group)
  const membersQuery = useMembers(group.id, needsMembers)
  return closingBarView(group, needsMembers ? membersQuery.data?.members : undefined)
}
