import { QueryClient, QueryObserver } from '@tanstack/react-query'
import { describe, expect, it, vi } from 'vitest'
import { testGroupId } from '@/test/groupTestData'
import { balancesQueryKey } from '@/features/groups/shared/groupCache'
import { refreshAfterSettlementChange, settlementsQueryKey } from './settlementCache'

const otherGroupId = 'f0e1d2c3-b4a5-4968-8776-655443322110'

function seedGroup(queryClient: QueryClient, groupId: string): void {
  queryClient.setQueryData(['groups', groupId, 'balances'], { isEveryoneKvit: false, currencies: [] })
  queryClient.setQueryData(['groups', groupId, 'settlements'], [])
  queryClient.setQueryData(['groups', groupId, 'activity'], [])
  queryClient.setQueryData(['groups', groupId, 'expenses'], [])
  queryClient.setQueryData(['groups', groupId, 'members'], { members: [], removed: [], canClaimNames: false })
  queryClient.setQueryData(['groups', groupId], { id: groupId })
}

function seededClient(): QueryClient {
  const queryClient = new QueryClient()
  seedGroup(queryClient, testGroupId)
  seedGroup(queryClient, otherGroupId)
  queryClient.setQueryData(['groups'], [])
  queryClient.setQueryData(['other'], [])
  return queryClient
}

describe('balancesQueryKey', () => {
  it('is the key groups, the group id, balances', () => {
    expect(balancesQueryKey(testGroupId)).toEqual(['groups', testGroupId, 'balances'])
  })
})

describe('settlementsQueryKey', () => {
  it('is the key groups, the group id, settlements', () => {
    expect(settlementsQueryKey(testGroupId)).toEqual(['groups', testGroupId, 'settlements'])
  })
})

describe('refreshAfterSettlementChange', () => {
  it.each([
    ['the balances of the group', ['groups', testGroupId, 'balances']],
    ['the payments of the group', ['groups', testGroupId, 'settlements']],
    ['the activity of the group', ['groups', testGroupId, 'activity']],
    ['the expenses of the group', ['groups', testGroupId, 'expenses']],
  ])('marks %s as out of date', async (_name, queryKey) => {
    const queryClient = seededClient()

    await refreshAfterSettlementChange(queryClient, testGroupId)

    expect(queryClient.getQueryState(queryKey)?.isInvalidated).toBe(true)
  })

  it.each([
    ['the balances of another group', ['groups', otherGroupId, 'balances']],
    ['the payments of another group', ['groups', otherGroupId, 'settlements']],
    ['the activity of another group', ['groups', otherGroupId, 'activity']],
    ['the expenses of another group', ['groups', otherGroupId, 'expenses']],
    ['the group list', ['groups']],
    ['a query that is not about groups', ['other']],
  ])('does not mark %s as out of date', async (_name, queryKey) => {
    const queryClient = seededClient()

    await refreshAfterSettlementChange(queryClient, testGroupId)

    expect(queryClient.getQueryState(queryKey)?.isInvalidated).toBe(false)
  })

  it('asks the server again for the balances that are on screen and resolves only after the answer came', async () => {
    const queryClient = seededClient()
    const loadBalances = vi.fn(() => Promise.resolve({ isEveryoneKvit: true, currencies: [] }))
    const watcher = new QueryObserver(queryClient, {
      queryKey: balancesQueryKey(testGroupId),
      queryFn: loadBalances,
      staleTime: Infinity,
    })
    const stopWatching = watcher.subscribe(() => {})

    await refreshAfterSettlementChange(queryClient, testGroupId)

    expect(loadBalances).toHaveBeenCalledTimes(1)
    expect(queryClient.getQueryData(balancesQueryKey(testGroupId))).toEqual({
      isEveryoneKvit: true,
      currencies: [],
    })
    stopWatching()
  })
})
