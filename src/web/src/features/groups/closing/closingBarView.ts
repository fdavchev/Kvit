import type { Group } from '@/core/services/groups/groupsService'
import type { GroupMember } from '@/core/services/groups/membersService'

export type ClosingBarView =
  | { kind: 'ready' }
  | { kind: 'waiting'; confirmedNames: string[]; waitingNames: string[]; endsAt: string }
  | { kind: 'prompt'; ownerName: string; endsAt: string }
  | { kind: 'confirmed'; waitingNames: string[]; endsAt: string }
  | { kind: 'finished'; canUnlock: boolean }

export function needsMembersForClosingBar(group: Group): boolean {
  return group.status === 'Closing' && !group.isOwner
}

export function closingBarView(group: Group, members: readonly GroupMember[] | undefined): ClosingBarView | null {
  switch (group.status) {
    case 'Open':
      return group.isOwner && group.canStartClosing ? { kind: 'ready' } : null
    case 'Finished':
      return { kind: 'finished', canUnlock: group.isOwner }
    case 'Closing':
      return closingView(group, members)
  }
}

function closingView(group: Group, members: readonly GroupMember[] | undefined): ClosingBarView | null {
  const endsAt = endOf(group)
  const waitingNames = group.closingWaitingFor.map((person) => person.name)
  if (group.isOwner) {
    return {
      kind: 'waiting',
      confirmedNames: group.closingConfirmedBy.map((person) => person.name),
      waitingNames,
      endsAt,
    }
  }
  if (members === undefined) {
    return null
  }
  const me = members.find((member) => member.isYou)
  if (me === undefined) {
    throw new Error(`The members of the group ${group.id} do not include the signed-in person`)
  }
  if (group.closingWaitingFor.some((person) => person.id === me.id)) {
    return { kind: 'prompt', ownerName: ownerNameOf(group, members), endsAt }
  }
  if (group.closingConfirmedBy.some((person) => person.id === me.id)) {
    return { kind: 'confirmed', waitingNames, endsAt }
  }
  return null
}

function endOf(group: Group): string {
  if (group.closingEndsAt === null) {
    throw new Error(`The group ${group.id} is being closed but has no closingEndsAt`)
  }
  return group.closingEndsAt
}

function ownerNameOf(group: Group, members: readonly GroupMember[]): string {
  const owner = members.find((member) => member.isOwner)
  if (owner === undefined) {
    throw new Error(`The members of the group ${group.id} include no owner`)
  }
  return owner.displayName
}
