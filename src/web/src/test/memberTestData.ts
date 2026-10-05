import type {
  GroupMember,
  GroupMembers,
  RemovedMember,
} from '@/core/services/groups/membersService'
import { testMe } from './testMe'

export const anaUserId = '3d9e8f7a-6b5c-4d4e-8f3a-2b1c0d9e8f7a'
export const petarUserId = '8c7b6a59-4837-4261-9a0b-1c2d3e4f5a6b'

export const filipMember: GroupMember = {
  id: 'a1b2c3d4-0001-4aaa-8bbb-000000000001',
  name: testMe.displayName,
  displayName: testMe.displayName,
  userId: testMe.id,
  isOwner: true,
  isYou: true,
  isNameOnly: false,
  claimedName: null,
}

export const anaMember: GroupMember = {
  id: 'a1b2c3d4-0002-4aaa-8bbb-000000000002',
  name: 'Ana',
  displayName: 'Ana',
  userId: anaUserId,
  isOwner: false,
  isYou: false,
  isNameOnly: false,
  claimedName: null,
}

export const markoMember: GroupMember = {
  id: 'a1b2c3d4-0003-4aaa-8bbb-000000000003',
  name: 'Marko',
  displayName: 'Marko',
  userId: null,
  isOwner: false,
  isYou: false,
  isNameOnly: true,
  claimedName: null,
}

export const grandmaMember: GroupMember = {
  id: 'a1b2c3d4-0004-4aaa-8bbb-000000000004',
  name: 'Grandma',
  displayName: 'Grandma',
  userId: null,
  isOwner: false,
  isYou: false,
  isNameOnly: true,
  claimedName: null,
}

export const petarMember: GroupMember = {
  id: 'a1b2c3d4-0005-4aaa-8bbb-000000000005',
  name: 'Darko',
  displayName: 'Petar',
  userId: petarUserId,
  isOwner: false,
  isYou: false,
  isNameOnly: false,
  claimedName: 'Darko',
}

export const bojanRemoved: RemovedMember = {
  id: 'a1b2c3d4-0006-4aaa-8bbb-000000000006',
  displayName: 'Bojan',
}

export const ownerViewMembers: GroupMembers = {
  members: [filipMember, anaMember, markoMember, grandmaMember, petarMember],
  removed: [bojanRemoved],
  canClaimNames: false,
}

export const anaViewMembers: GroupMembers = {
  members: [
    { ...filipMember, isYou: false },
    { ...anaMember, isYou: true },
    markoMember,
    grandmaMember,
    petarMember,
  ],
  removed: [],
  canClaimNames: true,
}

export function membersOf(changes: Partial<GroupMembers>): GroupMembers {
  return { ...ownerViewMembers, ...changes }
}
