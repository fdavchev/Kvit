import { describe, expect, it } from 'vitest'
import type { Group, GroupStatus } from '@/core/services/groups/groupsService'
import { groupOf } from '@/test/groupTestData'
import { canChangeGroup, canInviteToGroup, canLeaveGroup } from './groupAccess'

function groupWith(status: GroupStatus, isOwner: boolean, closedByAdmin: boolean): Group {
  return groupOf({ status, isOwner, closedByAdmin })
}

const statuses: GroupStatus[] = ['Open', 'Closing', 'Finished']

describe('canChangeGroup', () => {
  it.each([
    ['the owner of an open group', groupWith('Open', true, false), true],
    ['a member of an open group', groupWith('Open', false, false), true],
    ['the owner of a group that is being closed', groupWith('Closing', true, false), true],
    ['a member of a group that is being closed', groupWith('Closing', false, false), false],
    ['the owner of a finished group', groupWith('Finished', true, false), false],
    ['a member of a finished group', groupWith('Finished', false, false), false],
  ])('answers for %s', (_who, group, expected) => {
    expect(canChangeGroup(group)).toBe(expected)
  })

  it.each(statuses)('refuses the owner of a %s group that the owner of Kvit closed', (status) => {
    expect(canChangeGroup(groupWith(status, true, true))).toBe(false)
  })

  it.each(statuses)('refuses a member of a %s group that the owner of Kvit closed', (status) => {
    expect(canChangeGroup(groupWith(status, false, true))).toBe(false)
  })
})

describe('canInviteToGroup', () => {
  it('allows inviting to an open group', () => {
    expect(canInviteToGroup(groupWith('Open', true, false))).toBe(true)
  })

  it.each([true, false])('refuses inviting to an open group that the owner of Kvit closed (owner %j)', (isOwner) => {
    expect(canInviteToGroup(groupWith('Open', isOwner, true))).toBe(false)
  })
})

describe('canLeaveGroup', () => {
  it.each(['Open', 'Finished'] as const)('lets a member leave a %s group that the owner of Kvit closed', (status) => {
    expect(canLeaveGroup(groupWith(status, false, true))).toBe(true)
  })

  it('still lets a member leave an open group', () => {
    expect(canLeaveGroup(groupWith('Open', false, false))).toBe(true)
  })

  it('still keeps the owner from leaving a group that the owner of Kvit closed', () => {
    expect(canLeaveGroup(groupWith('Open', true, true))).toBe(false)
  })
})
