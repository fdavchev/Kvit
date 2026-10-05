import { describe, expect, it } from 'vitest'
import { endpoints } from './endpoints'

const groupId = '7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e'
const memberId = 'a1b2c3d4-0002-4aaa-8bbb-000000000002'

describe('endpoints', () => {
  it.each([
    ['googleLogIn', '/api/auth/google'],
    ['googleSignUp', '/api/auth/google/sign-up'],
    ['setPassword', '/api/auth/set-password'],
    ['groups', '/api/groups'],
    ['invitePreview', '/api/invites/preview'],
    ['inviteJoin', '/api/invites/join'],
  ] as const)('has the path %s at %s', (name, path) => {
    expect(endpoints[name]).toBe(path)
  })

  it.each([
    ['group', '/api/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e'],
    ['groupRestore', '/api/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e/restore'],
    ['groupLeave', '/api/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e/leave'],
    ['groupMembers', '/api/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e/members'],
    ['groupOwner', '/api/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e/owner'],
    ['groupInviteReset', '/api/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e/invite/reset'],
    ['groupInviteUndoReset', '/api/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e/invite/undo-reset'],
  ] as const)('builds the path %s for a group id as %s', (name, path) => {
    expect(endpoints[name](groupId)).toBe(path)
  })

  it.each([
    ['groupMember', '/api/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e/members/a1b2c3d4-0002-4aaa-8bbb-000000000002'],
    ['groupMemberLetBackIn', '/api/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e/members/a1b2c3d4-0002-4aaa-8bbb-000000000002/let-back-in'],
    ['groupMemberClaim', '/api/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e/members/a1b2c3d4-0002-4aaa-8bbb-000000000002/claim'],
    ['groupMemberUndoClaim', '/api/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e/members/a1b2c3d4-0002-4aaa-8bbb-000000000002/undo-claim'],
  ] as const)('builds the path %s for a group id and a member id as %s', (name, path) => {
    expect(endpoints[name](groupId, memberId)).toBe(path)
  })
})
