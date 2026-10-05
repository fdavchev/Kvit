import type { InvitePreview } from '@/core/services/invites/invitesService'
import { defaultGroupEmoji, testGroupId } from './groupTestData'

export const testInviteGroupName = 'Greece trip'

export const openInvitePreview: InvitePreview = {
  status: 'Open',
  groupId: null,
  name: testInviteGroupName,
  emoji: defaultGroupEmoji,
  memberNames: ['Filip', 'Ana'],
  unclaimedNames: [],
}

export const openInvitePreviewWithNames: InvitePreview = {
  ...openInvitePreview,
  memberNames: ['Filip', 'Ana', 'Marko', 'Grandma'],
  unclaimedNames: [
    { id: 'b1b2c3d4-0003-4aaa-8bbb-000000000003', name: 'Marko' },
    { id: 'b1b2c3d4-0004-4aaa-8bbb-000000000004', name: 'Grandma' },
  ],
}

export const alreadyMemberInvitePreview: InvitePreview = {
  ...openInvitePreview,
  status: 'AlreadyMember',
  groupId: testGroupId,
}

export const removedInvitePreview: InvitePreview = {
  ...openInvitePreview,
  status: 'Removed',
}

export const unsafeJoinTokens: readonly string[] = [
  'https://evil.example',
  '//evil.example',
  'a/b',
  'a?b=c',
  'a#b',
  'x y',
]

export function joinPathOf(token: string): string {
  return `/join/${encodeURIComponent(token)}`
}
