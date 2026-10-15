import { describe, expect, it } from 'vitest'
import { endpoints } from './endpoints'

const groupId = '7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e'

describe('endpoints for closing a group', () => {
  it.each([
    ['groupClosing', '/api/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e/closing'],
    ['groupClosingConfirm', '/api/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e/closing/confirm'],
    ['groupClosingObject', '/api/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e/closing/object'],
    ['groupUnlock', '/api/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e/unlock'],
  ] as const)('builds the path %s for a group id as %s', (name, path) => {
    expect(endpoints[name](groupId)).toBe(path)
  })
})
