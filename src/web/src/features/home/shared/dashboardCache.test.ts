import { QueryClient, QueryObserver } from '@tanstack/react-query'
import { describe, expect, it, vi } from 'vitest'
import { fullDashboard } from '@/test/dashboardTestData'
import { testGroupId } from '@/test/groupTestData'
import {
  forgetGroup,
  refreshGroupAndList,
  refreshWholeGroup,
} from '@/features/groups/shared/groupCache'
import { refreshAfterExpenseChange } from '@/features/expenses/shared/expenseCache'
import { refreshAfterSettlementChange } from '@/features/settlements/shared/settlementCache'
import { dashboardKey, invalidateDashboard } from './dashboardCache'

function seededClient(): QueryClient {
  const queryClient = new QueryClient()
  queryClient.setQueryData(['dashboard'], fullDashboard)
  queryClient.setQueryData(['groups'], [])
  queryClient.setQueryData(['groups', testGroupId], { id: testGroupId })
  queryClient.setQueryData(['me'], { id: 'me' })
  return queryClient
}

function isDashboardOutOfDate(queryClient: QueryClient): boolean | undefined {
  return queryClient.getQueryState(['dashboard'])?.isInvalidated
}

describe('dashboardKey', () => {
  it('is the key dashboard', () => {
    expect(dashboardKey).toEqual(['dashboard'])
  })
})

describe('invalidateDashboard', () => {
  it('marks the dashboard as out of date', async () => {
    const queryClient = seededClient()

    await invalidateDashboard(queryClient)

    expect(isDashboardOutOfDate(queryClient)).toBe(true)
  })

  it.each([
    ['the group list', ['groups']],
    ['a group', ['groups', testGroupId]],
    ['the signed-in person', ['me']],
  ])('does not mark %s as out of date', async (_name, queryKey) => {
    const queryClient = seededClient()

    await invalidateDashboard(queryClient)

    expect(queryClient.getQueryState(queryKey)?.isInvalidated).toBe(false)
  })

  it('loads the dashboard again when someone is looking at it, and waits for the answer', async () => {
    const queryClient = seededClient()
    const loadDashboard = vi.fn(() => Promise.resolve(fullDashboard))
    const watcher = new QueryObserver(queryClient, {
      queryKey: dashboardKey,
      queryFn: loadDashboard,
      staleTime: Infinity,
    })
    const stopWatching = watcher.subscribe(() => {})

    await invalidateDashboard(queryClient)

    expect(loadDashboard).toHaveBeenCalledTimes(1)
    stopWatching()
  })
})

describe('the shared refresh helpers also refresh the dashboard', () => {
  const helpers: [string, (queryClient: QueryClient) => Promise<void>][] = [
    ['refreshGroupAndList', (queryClient) => refreshGroupAndList(queryClient, testGroupId)],
    ['refreshWholeGroup', (queryClient) => refreshWholeGroup(queryClient, testGroupId)],
    ['forgetGroup', (queryClient) => forgetGroup(queryClient, testGroupId)],
    ['refreshAfterExpenseChange', (queryClient) => refreshAfterExpenseChange(queryClient, testGroupId)],
    ['refreshAfterSettlementChange', (queryClient) => refreshAfterSettlementChange(queryClient, testGroupId)],
  ]

  it.each(helpers)('marks the dashboard as out of date in %s', async (_name, refresh) => {
    const queryClient = seededClient()

    await refresh(queryClient)

    expect(isDashboardOutOfDate(queryClient)).toBe(true)
  })
})
