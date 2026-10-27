import { QueryClient, QueryObserver } from '@tanstack/react-query'
import { describe, expect, it, vi } from 'vitest'
import { testGroupId } from '@/test/groupTestData'
import { balancesQueryKey, groupQueryKey, groupsQueryKey } from '@/features/groups/shared/groupCache'
import { refreshAfterExpenseChange } from './expenseCache'

const otherGroupId = 'f0e1d2c3-b4a5-4968-8776-655443322110'

function seedGroup(queryClient: QueryClient, groupId: string): void {
  queryClient.setQueryData(['groups', groupId, 'balances'], { isEveryoneKvit: false, currencies: [] })
  queryClient.setQueryData(['groups', groupId, 'spending'], { currencies: [], plan: null })
  queryClient.setQueryData(['groups', groupId, 'activity'], [])
  queryClient.setQueryData(['groups', groupId, 'expenses'], [])
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

describe('refreshAfterExpenseChange', () => {
  it.each([
    ['the expenses of the group', ['groups', testGroupId, 'expenses']],
    ['the activity of the group', ['groups', testGroupId, 'activity']],
    ['the balances of the group', ['groups', testGroupId, 'balances']],
    ['the spending of the group, because the totals by category depend on the expenses', ['groups', testGroupId, 'spending']],
    ['the group itself, because canStartClosing and a One bill finishing depend on the expenses', ['groups', testGroupId]],
    ['the group list, because a group can move to Finished', ['groups']],
  ])('marks %s as out of date', async (_name, queryKey) => {
    const queryClient = seededClient()

    await refreshAfterExpenseChange(queryClient, testGroupId)

    expect(queryClient.getQueryState(queryKey)?.isInvalidated).toBe(true)
  })

  it.each([
    ['the expenses of another group', ['groups', otherGroupId, 'expenses']],
    ['the activity of another group', ['groups', otherGroupId, 'activity']],
    ['the balances of another group', ['groups', otherGroupId, 'balances']],
    ['the spending of another group', ['groups', otherGroupId, 'spending']],
    ['another group itself', ['groups', otherGroupId]],
    ['a query that is not about groups', ['other']],
  ])('does not mark %s as out of date', async (_name, queryKey) => {
    const queryClient = seededClient()

    await refreshAfterExpenseChange(queryClient, testGroupId)

    expect(queryClient.getQueryState(queryKey)?.isInvalidated).toBe(false)
  })

  it('asks the server again for the group that is on screen and resolves only after the answer came, so the ready bar can appear without a reload', async () => {
    const queryClient = seededClient()
    const loadGroup = vi.fn(() => Promise.resolve({ id: testGroupId, canStartClosing: true }))
    const watcher = new QueryObserver(queryClient, {
      queryKey: groupQueryKey(testGroupId),
      queryFn: loadGroup,
      staleTime: Infinity,
    })
    const stopWatching = watcher.subscribe(() => {})

    await refreshAfterExpenseChange(queryClient, testGroupId)

    expect(loadGroup).toHaveBeenCalledTimes(1)
    expect(queryClient.getQueryData(groupQueryKey(testGroupId))).toEqual({ id: testGroupId, canStartClosing: true })
    stopWatching()
  })

  it('asks the server again for the group list that is on screen', async () => {
    const queryClient = seededClient()
    const loadGroups = vi.fn(() => Promise.resolve([]))
    const watcher = new QueryObserver(queryClient, {
      queryKey: groupsQueryKey,
      queryFn: loadGroups,
      staleTime: Infinity,
    })
    const stopWatching = watcher.subscribe(() => {})

    await refreshAfterExpenseChange(queryClient, testGroupId)

    expect(loadGroups).toHaveBeenCalledTimes(1)
    stopWatching()
  })

  it('does not ask the server again for another group that is on screen', async () => {
    const queryClient = seededClient()
    const loadOtherGroup = vi.fn(() => Promise.resolve({ id: otherGroupId }))
    const watcher = new QueryObserver(queryClient, {
      queryKey: groupQueryKey(otherGroupId),
      queryFn: loadOtherGroup,
      staleTime: Infinity,
    })
    const stopWatching = watcher.subscribe(() => {})

    await refreshAfterExpenseChange(queryClient, testGroupId)

    expect(loadOtherGroup).not.toHaveBeenCalled()
    stopWatching()
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

  it('asks the server again for the spending that is on screen and resolves only after the answer came, so the totals change without a reload', async () => {
    const queryClient = seededClient()
    const loadSpending = vi.fn(() => Promise.resolve({ currencies: [], plan: null }))
    const watcher = new QueryObserver(queryClient, {
      queryKey: ['groups', testGroupId, 'spending'],
      queryFn: loadSpending,
      staleTime: Infinity,
    })
    const stopWatching = watcher.subscribe(() => {})

    await refreshAfterExpenseChange(queryClient, testGroupId)

    expect(loadSpending).toHaveBeenCalledTimes(1)
    stopWatching()
  })

  it('does not ask the server again for the spending of another group that is on screen', async () => {
    const queryClient = seededClient()
    const loadOtherSpending = vi.fn(() => Promise.resolve({ currencies: [], plan: null }))
    const watcher = new QueryObserver(queryClient, {
      queryKey: ['groups', otherGroupId, 'spending'],
      queryFn: loadOtherSpending,
      staleTime: Infinity,
    })
    const stopWatching = watcher.subscribe(() => {})

    await refreshAfterExpenseChange(queryClient, testGroupId)

    expect(loadOtherSpending).not.toHaveBeenCalled()
    stopWatching()
  })
})
