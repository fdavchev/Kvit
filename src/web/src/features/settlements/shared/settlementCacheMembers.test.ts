import { QueryClient, QueryObserver } from '@tanstack/react-query'
import { describe, expect, it, vi } from 'vitest'
import { testGroupId } from '@/test/groupTestData'
import { membersQueryKey } from '@/features/groups/shared/groupCache'
import { refreshAfterSettlementChange } from './settlementCache'

const otherGroupId = 'f0e1d2c3-b4a5-4968-8776-655443322110'
const membersWithoutClaimableNames = { members: [], removed: [], canClaimNames: false }

function seededClient(): QueryClient {
  const queryClient = new QueryClient()
  queryClient.setQueryData(['groups', testGroupId, 'members'], membersWithoutClaimableNames)
  queryClient.setQueryData(['groups', otherGroupId, 'members'], membersWithoutClaimableNames)
  return queryClient
}

describe('membersQueryKey', () => {
  it('is the key groups, the group id, members', () => {
    expect(membersQueryKey(testGroupId)).toEqual(['groups', testGroupId, 'members'])
  })
})

describe('refreshAfterSettlementChange and the members of the group', () => {
  it('marks the members of the group as out of date, because a payment decides who can still be claimed with That is me', async () => {
    const queryClient = seededClient()

    await refreshAfterSettlementChange(queryClient, testGroupId)

    expect(queryClient.getQueryState(membersQueryKey(testGroupId))?.isInvalidated).toBe(true)
  })

  it('does not mark the members of another group as out of date', async () => {
    const queryClient = seededClient()

    await refreshAfterSettlementChange(queryClient, testGroupId)

    expect(queryClient.getQueryState(membersQueryKey(otherGroupId))?.isInvalidated).toBe(false)
  })

  it('asks the server again for the members that are on screen and resolves only after the answer came', async () => {
    const queryClient = seededClient()
    const loadMembers = vi.fn(() => Promise.resolve({ members: [], removed: [], canClaimNames: true }))
    const watcher = new QueryObserver(queryClient, {
      queryKey: membersQueryKey(testGroupId),
      queryFn: loadMembers,
      staleTime: Infinity,
    })
    const stopWatching = watcher.subscribe(() => {})

    await refreshAfterSettlementChange(queryClient, testGroupId)

    expect(loadMembers).toHaveBeenCalledTimes(1)
    expect(queryClient.getQueryData(membersQueryKey(testGroupId))).toEqual({
      members: [],
      removed: [],
      canClaimNames: true,
    })
    stopWatching()
  })

  it('does not ask the server again for the members of another group that are on screen', async () => {
    const queryClient = seededClient()
    const loadOtherMembers = vi.fn(() => Promise.resolve(membersWithoutClaimableNames))
    const watcher = new QueryObserver(queryClient, {
      queryKey: membersQueryKey(otherGroupId),
      queryFn: loadOtherMembers,
      staleTime: Infinity,
    })
    const stopWatching = watcher.subscribe(() => {})

    await refreshAfterSettlementChange(queryClient, testGroupId)

    expect(loadOtherMembers).not.toHaveBeenCalled()
    stopWatching()
  })
})
