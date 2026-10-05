import type {
  DeletedGroupListRow,
  Group,
  GroupList,
  GroupListRow,
} from '@/core/services/groups/groupsService'
import { testMe } from './testMe'

export const groupEmojis: readonly string[] = [
  '\u{1F3D6}\u{FE0F}',
  '\u{2708}\u{FE0F}',
  '\u{1F3E0}',
  '\u{1F355}',
  '\u{1F389}',
  '\u{1F697}',
]

export const defaultGroupEmoji: string = groupEmojis[0]

export const groupNameMaxLength = 60

export const testGroupId = '7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e'

export const testInviteToken = 'q3Fz8-mXk2_Lw9Tn5Vb1Rc7Yh0JdAeSgUiOpKfXzM4'

export const testGroup: Group = {
  id: testGroupId,
  kind: 'Group',
  name: 'Greece trip',
  emoji: defaultGroupEmoji,
  defaultCurrency: 'MKD',
  status: 'Open',
  ownerUserId: testMe.id,
  isOwner: true,
  memberCount: 1,
  inviteToken: testInviteToken,
}

export const flatGroupRow: GroupListRow = {
  id: '2a9f6c10-5d34-4b8e-8c71-0f3e2d1c4b5a',
  name: 'Flat 4B',
  emoji: '\u{1F3E0}',
  defaultCurrency: 'EUR',
  memberCount: 3,
}

export const greeceGroupRow: GroupListRow = {
  id: testGroupId,
  name: 'Greece trip',
  emoji: defaultGroupEmoji,
  defaultCurrency: 'MKD',
  memberCount: 1,
}

export const summerFinishedRow: GroupListRow = {
  id: '5e8b1d22-7a46-4c9f-b3d0-9a8b7c6d5e4f',
  name: 'Summer 2025',
  emoji: '\u{2600}\u{FE0F}',
  defaultCurrency: 'MKD',
  memberCount: 5,
}

export const dinnerDeletedRow: DeletedGroupListRow = {
  id: '9d3c2b1a-8e7f-4a6b-95c4-d3e2f1a0b9c8',
  name: 'Birthday dinner',
  emoji: '\u{1F355}',
  defaultCurrency: 'MKD',
  memberCount: 4,
  deletedAt: '2026-10-03T12:00:00Z',
  restorableUntil: '2026-11-02T12:00:00Z',
}

export const emptyGroupList: GroupList = {
  groups: [],
  finishedGroups: [],
  recentlyDeleted: [],
}

export function groupOf(changes: Partial<Group>): Group {
  return { ...testGroup, ...changes }
}

export function groupListOf(changes: Partial<GroupList>): GroupList {
  return { ...emptyGroupList, ...changes }
}
