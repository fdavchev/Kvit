import { QueryClient, QueryObserver } from '@tanstack/react-query'
import { describe, expect, it, vi } from 'vitest'
import { testGroupId } from '@/test/groupTestData'
import { balancesQueryKey } from '@/features/groups/shared/groupCache'
import { refreshAfterExpenseChange } from './expenseCache'

const otherGroupId = 'f0e1d2c3-b4a5-4968-8776-655443322110'

function seedGroup(queryClient: QueryClient, groupId: string): void {
  queryClient.setQueryData(['groups', groupId, 'balances'], { isEveryoneKvit: false, currencies: [] })
  queryClient.setQueryData(['groups', groupId, 'activity'], [])
  queryClient.setQueryData(['groups', groupId, 'expenses'], [])
}

function seededClient(): QueryClient {
  const queryClient = new QueryClient()
  seedGroup(queryClient, testGroupId)
  seedGroup(queryClient, otherGroupId)
  queryClient.setQueryData(['other'], [])
  return queryClient
}

describe('refreshAfterExpenseChange', () => {
  it.each([
    ['the expenses of the group', ['groups', testGroupId, 'expenses']],
    ['the activity of the group', ['groups', testGroupId, 'activity']],
    ['the balances of the group', ['groups', testGroupId, 'balances']],
  ])('marks %s as out of date', async (_name, queryKey) => {
    const queryClient = seededClient()

    await refreshAfterExpenseChange(queryClient, testGroupId)

    expect(queryClient.getQueryState(queryKey)?.isInvalidated).toBe(true)
  })

  it.each([
    ['the expenses of another group', ['groups', otherGroupId, 'expenses']],
    ['the activity of another group', ['groups', otherGroupId, 'activity']],
    ['the balances of another group', ['groups', otherGroupId, 'balances']],
    ['a query that is not about groups', ['other']],
  ])('does not mark %s as out of date', async (_name, queryKey) => {
    const queryClient = seededClient()

    await refreshAfterExpenseChange(queryClient, testGroupId)

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

    await refreshAfterExpenseChange(queryClient, testGroupId)

    expect(loadBalances).toHaveBeenCalledTimes(1)
    stopWatching()
  })
})
