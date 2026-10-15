import type { QueryClient } from '@tanstack/react-query'

export const groupsQueryKey = ['groups'] as const

export function groupQueryKey(groupId: string): readonly ['groups', string] {
  return ['groups', groupId]
}

export function membersQueryKey(groupId: string): readonly ['groups', string, 'members'] {
  return ['groups', groupId, 'members']
}

export function activityQueryKey(groupId: string): readonly ['groups', string, 'activity'] {
  return ['groups', groupId, 'activity']
}

export function balancesQueryKey(groupId: string): readonly ['groups', string, 'balances'] {
  return ['groups', groupId, 'balances']
}

export async function forgetGroup(queryClient: QueryClient, groupId: string): Promise<void> {
  queryClient.removeQueries({ queryKey: groupQueryKey(groupId), exact: true })
  await queryClient.invalidateQueries({ queryKey: groupsQueryKey, exact: true })
}

export async function refreshGroupAndList(queryClient: QueryClient, groupId: string): Promise<void> {
  await Promise.all([
    queryClient.invalidateQueries({ queryKey: groupQueryKey(groupId), exact: true }),
    queryClient.invalidateQueries({ queryKey: groupsQueryKey, exact: true }),
  ])
}

export async function refreshWholeGroup(queryClient: QueryClient, groupId: string): Promise<void> {
  await Promise.all([
    queryClient.invalidateQueries({ queryKey: groupsQueryKey, exact: true }),
    queryClient.invalidateQueries({ queryKey: groupQueryKey(groupId) }),
  ])
}
