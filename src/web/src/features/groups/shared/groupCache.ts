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

export async function forgetGroup(queryClient: QueryClient, groupId: string): Promise<void> {
  queryClient.removeQueries({ queryKey: groupQueryKey(groupId), exact: true })
  await queryClient.invalidateQueries({ queryKey: groupsQueryKey, exact: true })
}
