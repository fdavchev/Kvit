import { act, render, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest'
import { requestCountTo, stubFetch, stubFetchByPath } from '@/test/apiTestHelpers'
import { testMe } from '@/test/testMe'
import { createTestPersister } from '@/test/testPersister'

const wakeUrl = 'https://kvit-mk-api.onrender.com/wake'
const twoMinutes = 2 * 60 * 1000
const threeSeconds = 3000
const visitedThirtySecondsAgo = 30_000
const waitLeft = twoMinutes - visitedThirtySecondsAgo
const meAddress = '/api/me'
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

async function expectText(text: string): Promise<void> {
  await vi.waitFor(() => {
    expect(screen.getByText(text)).toBeTruthy()
  })
}

async function renderWithRetry(): Promise<void> {
  vi.resetModules()
  const { QueryClientProvider, useQuery } = await import('@tanstack/react-query')
  const { createQueryClient } = await import('@/core/api/queryClient')
  const { apiRequest } = await import('@/core/api/apiClient')
  const { useMe } = await import('@/core/auth/useMe')
  const { RetryWhileServerWakes } = await import('./RetryWhileServerWakes')
  const queryClient = createQueryClient(createTestPersister())
  const defaultOptions = queryClient.getDefaultOptions()
  queryClient.setDefaultOptions({ ...defaultOptions, queries: { ...defaultOptions.queries, retry: false } })

  function MeProbe() {
    const meQuery = useMe()
    return <p>me: {meQuery.data?.displayName ?? meQuery.status}</p>
  }

  function GroupsProbe() {
    const groupsQuery = useQuery({ queryKey: ['groups'], queryFn: () => apiRequest(groupsAddress) })
    return <p>groups: {groupsQuery.status}</p>
  }

  render(
    <QueryClientProvider client={queryClient}>
      <RetryWhileServerWakes />
      <MeProbe />
      <GroupsProbe />
    </QueryClientProvider>,
  )
  await letTimePass(0)
}

let fetchMock: Mock<typeof fetch>

beforeEach(() => {
  vi.stubGlobal('location', { pathname: '/', search: '', assign: vi.fn<(url: string) => void>() })
  window.sessionStorage.clear()
  vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout', 'setInterval', 'clearInterval'] })
})

afterEach(() => {
  vi.useRealTimers()
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
  window.sessionStorage.clear()
})

describe('RetryWhileServerWakes', () => {
  it('asks "who am I" again every 3 seconds while waiting after a wake visit, until the server answers', async () => {
    rememberWakeVisitAgo(visitedThirtySecondsAgo)
    fetchMock = stubFetchByPath({
      [meAddress]: [asleepAnswer(), asleepAnswer(), Response.json(testMe)],
      [groupsAddress]: asleepAnswer(),
    })
    await renderWithRetry()

    await letTimePass(threeSeconds - 1)
    expect(requestCountTo(fetchMock, meAddress)).toBe(1)

    await letTimePass(1)
    expect(requestCountTo(fetchMock, meAddress)).toBe(2)

    await letTimePass(threeSeconds)
    expect(requestCountTo(fetchMock, meAddress)).toBe(3)
    await expectText(`me: ${testMe.displayName}`)
  })

  it('also asks again for another query that failed while waiting', async () => {
    rememberWakeVisitAgo(visitedThirtySecondsAgo)
    fetchMock = stubFetchByPath({
      [meAddress]: asleepAnswer(),
      [groupsAddress]: [asleepAnswer(), Response.json([])],
    })
    await renderWithRetry()

    await letTimePass(threeSeconds)

    expect(requestCountTo(fetchMock, groupsAddress)).toBe(2)
    await expectText('groups: success')
  })

  it('stops asking once the server has answered', async () => {
    rememberWakeVisitAgo(visitedThirtySecondsAgo)
    fetchMock = stubFetchByPath({
      [meAddress]: [asleepAnswer(), Response.json(testMe)],
      [groupsAddress]: [asleepAnswer(), Response.json([])],
    })
    await renderWithRetry()
    await letTimePass(threeSeconds)

    await letTimePass(threeSeconds * 5)

    expect(requestCountTo(fetchMock, meAddress)).toBe(2)
  })

  it('stops asking once the 2 minutes after the wake visit are over with the server still asleep', async () => {
    rememberWakeVisitAgo(visitedThirtySecondsAgo)
    fetchMock = stubFetch(asleepAnswer())
    await renderWithRetry()
    await letTimePass(waitLeft)
    const countWhenTheWaitEnded = requestCountTo(fetchMock, meAddress)

    await letTimePass(threeSeconds * 5)

    expect(countWhenTheWaitEnded).toBeGreaterThan(1)
    expect(requestCountTo(fetchMock, meAddress)).toBe(countWhenTheWaitEnded)
  })

  it('does not ask again by itself when the server is asleep and the last wake visit is 2 minutes old', async () => {
    rememberWakeVisitAgo(twoMinutes)
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new DOMException('Storage is full', 'QuotaExceededError')
    })
    vi.spyOn(console, 'error').mockImplementation(() => undefined)
    fetchMock = stubFetch(asleepAnswer())
    await renderWithRetry()

    await letTimePass(threeSeconds * 5)

    expect(requestCountTo(fetchMock, meAddress)).toBe(1)
  })
})
