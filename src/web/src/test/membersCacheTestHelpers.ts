import type { QueryClient } from '@tanstack/react-query'
import { greeceGroupRow, groupListOf, testGroup, testGroupId } from './groupTestData'
import { ownerViewMembers } from './memberTestData'

export function seedGroupMembersAndList(queryClient: QueryClient): void {
  queryClient.setQueryData(['groups', testGroupId], testGroup)
  queryClient.setQueryData(['groups', testGroupId, 'members'], ownerViewMembers)
  queryClient.setQueryData(['groups', testGroupId, 'activity'], [])
  queryClient.setQueryData(['groups'], groupListOf({ groups: [greeceGroupRow] }))
}

export function seedGroupMembersListAndOther(queryClient: QueryClient): void {
  seedGroupMembersAndList(queryClient)
  queryClient.setQueryData(['other'], ownerViewMembers)
}
