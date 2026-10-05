import { describe, expect, it } from 'vitest'
import { endpoints } from './endpoints'

const groupId = '7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e'

describe('endpoints', () => {
  it.each([
    ['googleLogIn', '/api/auth/google'],
    ['googleSignUp', '/api/auth/google/sign-up'],
    ['setPassword', '/api/auth/set-password'],
    ['groups', '/api/groups'],
  ] as const)('has the path %s at %s', (name, path) => {
    expect(endpoints[name]).toBe(path)
  })

  it.each([
    ['group', '/api/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e'],
    ['groupRestore', '/api/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e/restore'],
    ['groupLeave', '/api/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e/leave'],
    ['groupMembers', '/api/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e/members'],
  ] as const)('builds the path %s for a group id as %s', (name, path) => {
    expect(endpoints[name](groupId)).toBe(path)
  })
})
