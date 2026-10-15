import type { Group } from '@/core/services/groups/groupsService'

export function canChangeGroup(group: Group): boolean {
  return group.status === 'Open' || (group.status === 'Closing' && group.isOwner)
}

export function canInviteToGroup(group: Group): boolean {
  return group.status === 'Open'
}

export function canLeaveGroup(group: Group): boolean {
  return !group.isOwner && group.status !== 'Closing'
}
