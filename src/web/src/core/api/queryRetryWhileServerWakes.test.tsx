import { act, render, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { requestCountTo, stubFetchByPath } from '@/test/apiTestHelpers'
import { createTestPersister } from '@/test/testPersister'

const wakeUrl = 'https://kvit-mk-api.onrender.com/wake'
const twoMinutes = 2 * 60 * 1000
const visitedThirtySecondsAgo = 30_000
const waitLeft = twoMinutes - visitedThirtySecondsAgo
const groupsAddress = '/api/groups'

function asleepAnswer(): Response {
  return new Response('Kvit proxy: the API server is asleep', {
    status: 503,
    headers: { 'content-type': 'text/plain; charset=utf-8', 'x-kvit-server-asleep': '1', 'x-kvit-wake-url': wakeUrl },
  })
}

function rememberWakeVisitAgo(milliseconds: number): void {
  window.sessionStorage.setItem('kvit.lastWakeVisitAt', String(Date.now() - milliseconds))
}

async function letTimePass(milliseconds: number): Promise<void> {
  await act(async () => {
    await vi.advanceTimersByTimeAsync(milliseconds)
  })
}

function shownGroupsStatus(): string | null {
  return screen.getByTestId('groups-status').textContent
}

async function renderGroupsQuery(): Promise<void> {
  vi.resetModules()
  const { QueryClientProvider, useQuery } = await import('@tanstack/react-query')
  const { createQueryClient } = await import('@/core/api/queryClient')
  const { apiRequest } = await import('@/core/api/apiClient')
  const queryClient = createQueryClient(createTestPersister())

  function GroupsProbe() {
    const groupsQuery = useQuery({ queryKey: ['groups'], queryFn: () => apiRequest(groupsAddress) })
    return <p data-testid="groups-status">{groupsQuery.status}</p>
  }

  render(
    <QueryClientProvider client={queryClient}>
      <GroupsProbe />
    </QueryClientProvider>,
  )
  await letTimePass(0)
}

beforeEach(() => {
  vi.stubGlobal('location', { pathname: '/', search: '', assign: vi.fn<(url: string) => void>() })
  window.sessionStorage.clear()
  vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout', 'setInterval', 'clearInterval', 'Date'] })
})

afterEach(() => {
  vi.useRealTimers()
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
  window.sessionStorage.clear()
})

describe('a query without saved data while waiting after a wake visit', () => {
  it('stays loading instead of failing while the server keeps answering that it is asleep', async () => {
    rememberWakeVisitAgo(visitedThirtySecondsAgo)
    const fetchMock = stubFetchByPath({ [groupsAddress]: asleepAnswer() })
    await renderGroupsQuery()

    await letTimePass(30_000)

    expect(requestCountTo(fetchMock, groupsAddress)).toBeGreaterThan(4)
    expect(shownGroupsStatus()).toBe('pending')
  })

  it('shows the answer once the server is awake', async () => {
    rememberWakeVisitAgo(visitedThirtySecondsAgo)
    stubFetchByPath({ [groupsAddress]: [asleepAnswer(), asleepAnswer(), asleepAnswer(), asleepAnswer(), Response.json([])] })
    await renderGroupsQuery()

    await letTimePass(4 * 3000 + 1000)

    expect(shownGroupsStatus()).toBe('success')
  })

  it('fails once the 2 minutes after the wake visit are over with the server still asleep', async () => {
    rememberWakeVisitAgo(visitedThirtySecondsAgo)
    stubFetchByPath({ [groupsAddress]: asleepAnswer() })
    await renderGroupsQuery()

    await letTimePass(waitLeft + 2 * 3000)

    expect(shownGroupsStatus()).toBe('error')
  })

  it('fails after three retries as before when there was no wake visit', async () => {
    const fetchMock = stubFetchByPath({ [groupsAddress]: asleepAnswer() })
    await renderGroupsQuery()

    await letTimePass(1000 + 2000 + 4000 + 1000)

    expect(requestCountTo(fetchMock, groupsAddress)).toBe(4)

    expect(shownGroupsStatus()).toBe('error')
  })
})
