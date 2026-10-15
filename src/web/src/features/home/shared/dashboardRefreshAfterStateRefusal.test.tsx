import { QueryObserver, type QueryClient } from '@tanstack/react-query'
import { act, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { groupStateChangedCodes } from '@/core/api/queryClient'
import { captureError, problemResponse, stubFetch } from '@/test/apiTestHelpers'
import { markoPaidFilipPending } from '@/test/balancesTestData'
import { fullDashboard, quietDashboard } from '@/test/dashboardTestData'
import { greeceGroupRow, groupListOf, testGroup, testGroupId } from '@/test/groupTestData'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { useConfirmClosing } from '@/features/groups/closing/hooks/useConfirmClosing'
import { useObjectToClosing } from '@/features/groups/closing/hooks/useObjectToClosing'
import { useAnswerSettlement } from '@/features/settlements/pending/hooks/useAnswerSettlement'
import { dashboardKey } from './dashboardCache'

interface Mounted {
  queryClient: QueryClient
  run: () => Promise<unknown>
}

type Mount = () => Promise<Mounted>

function seedDashboardGroupAndList(queryClient: QueryClient): void {
  queryClient.setQueryData(dashboardKey, fullDashboard)
  queryClient.setQueryData(['groups', testGroupId], testGroup)
  queryClient.setQueryData(['groups'], groupListOf({ groups: [greeceGroupRow] }))
}

const mountConfirmClosing: Mount = async () => {
  const { result, queryClient } = await renderHookWithProviders(() => useConfirmClosing(testGroupId), {
    seedCache: seedDashboardGroupAndList,
  })
  return { queryClient, run: () => act(() => result.current.mutateAsync()) }
}

const mountObjectToClosing: Mount = async () => {
  const { result, queryClient } = await renderHookWithProviders(() => useObjectToClosing(testGroupId), {
    seedCache: seedDashboardGroupAndList,
  })
  return { queryClient, run: () => act(() => result.current.mutateAsync('Not yet')) }
}

const mountConfirmPayment: Mount = async () => {
  const { result, queryClient } = await renderHookWithProviders(
    () => useAnswerSettlement(testGroupId, markoPaidFilipPending.id),
    { seedCache: seedDashboardGroupAndList },
  )
  return { queryClient, run: () => act(() => result.current.mutateAsync('confirm')) }
}

const answersFromHome: [string, Mount][] = [
  ['confirming a closing', mountConfirmClosing],
  ['objecting to a closing', mountObjectToClosing],
  ['confirming a payment', mountConfirmPayment],
]

describe.each(answersFromHome)('a refused answer from the home screen: %s', (_name, mount) => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(groupStateChangedCodes)(
    'marks the dashboard as out of date when the server answers 400 %s, so a stale card goes away',
    async (errorCode) => {
      stubFetch(problemResponse(400, errorCode))
      const { run, queryClient } = await mount()

      await captureError(run())

      await waitFor(() => {
        expect(queryClient.getQueryState(dashboardKey)?.isInvalidated).toBe(true)
      })
    },
  )

  it.each(groupStateChangedCodes)('asks the server again for the dashboard that is on screen after 400 %s', async (errorCode) => {
    stubFetch(problemResponse(400, errorCode))
    const { run, queryClient } = await mount()
    const loadDashboard = vi.fn(() => Promise.resolve(quietDashboard))
    const watcher = new QueryObserver(queryClient, {
      queryKey: dashboardKey,
      queryFn: loadDashboard,
      staleTime: Infinity,
    })
    const stopWatching = watcher.subscribe(() => {})

    await captureError(run())

    await waitFor(() => {
      expect(loadDashboard).toHaveBeenCalledTimes(1)
    })
    stopWatching()
  })

  it.each(groupStateChangedCodes)('still exposes the ApiError with the code %s', async (errorCode) => {
    stubFetch(problemResponse(400, errorCode))
    const { run } = await mount()

    const error = await captureError(run())

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 400, errorCode })
  })

  it.each([
    [403, 'GROUP_NOT_OWNER'],
    [400, 'CLOSING_ALREADY_ANSWERED'],
    [404, 'GROUP_NOT_FOUND'],
  ])('leaves the dashboard as it is when the server answers %i %s', async (status, errorCode) => {
    stubFetch(problemResponse(status, errorCode))
    const { run, queryClient } = await mount()

    await captureError(run())

    expect(queryClient.getQueryState(dashboardKey)?.isInvalidated).toBe(false)
  })
})
