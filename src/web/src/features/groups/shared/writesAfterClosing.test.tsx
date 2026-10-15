import { QueryObserver, type QueryClient } from '@tanstack/react-query'
import { act, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { groupStateChangedCodes } from '@/core/api/queryClient'
import { captureError, problemResponse, stubFetch } from '@/test/apiTestHelpers'
import { closingGroup, finishedGroup } from '@/test/closingTestData'
import { useDeleteExpense } from '@/features/expenses/detail/hooks/useDeleteExpense'
import { greeceGroupRow, groupListOf, testGroup, testGroupId } from '@/test/groupTestData'
import { filipMember, markoMember } from '@/test/memberTestData'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { useRecordSettlement } from '@/features/settlements/record/hooks/useRecordSettlement'
import { useConfirmClosing } from '../closing/hooks/useConfirmClosing'
import { useObjectToClosing } from '../closing/hooks/useObjectToClosing'
import { useStartClosing } from '../closing/hooks/useStartClosing'
import { useUnlockGroup } from '../closing/hooks/useUnlockGroup'
import { useAddMember } from '../group/hooks/useAddMember'

interface Mounted {
  queryClient: QueryClient
  run: () => Promise<unknown>
}

type Mount = () => Promise<Mounted>

const expenseId = 'e0000000-0000-4000-8000-000000000004'

function seedGroupAndList(queryClient: QueryClient): void {
  queryClient.setQueryData(['groups', testGroupId], testGroup)
  queryClient.setQueryData(['groups'], groupListOf({ groups: [greeceGroupRow] }))
  queryClient.setQueryData(['groups', testGroupId, 'activity'], [])
}

const mountAddMember: Mount = async () => {
  const { result, queryClient } = await renderHookWithProviders(() => useAddMember(testGroupId), {
    seedCache: seedGroupAndList,
  })
  return { queryClient, run: () => act(() => result.current.mutateAsync('Grandma')) }
}

const mountRecordSettlement: Mount = async () => {
  const { result, queryClient } = await renderHookWithProviders(() => useRecordSettlement(testGroupId), {
    seedCache: seedGroupAndList,
  })
  return {
    queryClient,
    run: () =>
      act(() =>
        result.current.mutateAsync({
          clientRequestId: '5b1f2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d',
          fromMemberId: markoMember.id,
          toMemberId: filipMember.id,
          amountMinor: 80000,
          currency: 'MKD',
        }),
      ),
  }
}

const mountDeleteExpense: Mount = async () => {
  const { result, queryClient } = await renderHookWithProviders(() => useDeleteExpense(testGroupId, expenseId), {
    seedCache: seedGroupAndList,
  })
  return { queryClient, run: () => act(() => result.current.mutateAsync()) }
}

const mountConfirmClosing: Mount = async () => {
  const { result, queryClient } = await renderHookWithProviders(() => useConfirmClosing(testGroupId), {
    seedCache: seedGroupAndList,
  })
  return { queryClient, run: () => act(() => result.current.mutateAsync()) }
}

const mountObjectToClosing: Mount = async () => {
  const { result, queryClient } = await renderHookWithProviders(() => useObjectToClosing(testGroupId), {
    seedCache: seedGroupAndList,
  })
  return { queryClient, run: () => act(() => result.current.mutateAsync('Not yet')) }
}

const mountStartClosing: Mount = async () => {
  const { result, queryClient } = await renderHookWithProviders(() => useStartClosing(testGroupId), {
    seedCache: seedGroupAndList,
  })
  return { queryClient, run: () => act(() => result.current.mutateAsync()) }
}

const mountUnlockGroup: Mount = async () => {
  const { result, queryClient } = await renderHookWithProviders(() => useUnlockGroup(testGroupId), {
    seedCache: seedGroupAndList,
  })
  return { queryClient, run: () => act(() => result.current.mutateAsync()) }
}

const writes: [string, Mount][] = [
  ['adding a name', mountAddMember],
  ['recording a payment', mountRecordSettlement],
  ['deleting an expense', mountDeleteExpense],
  ['confirming a closing', mountConfirmClosing],
  ['objecting to a closing', mountObjectToClosing],
  ['starting a closing', mountStartClosing],
  ['unlocking a group', mountUnlockGroup],
]

describe.each(writes)('a failed write: %s', (_name, mount) => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(groupStateChangedCodes)('marks the group as out of date when the server answers 400 %s, so a stale screen updates', async (errorCode) => {
    stubFetch(problemResponse(400, errorCode))
    const { run, queryClient } = await mount()

    await captureError(run())

    await waitFor(() => {
      expect(queryClient.getQueryState(['groups', testGroupId])?.isInvalidated).toBe(true)
    })
  })

  it.each(groupStateChangedCodes)('marks the group list as out of date when the server answers 400 %s', async (errorCode) => {
    stubFetch(problemResponse(400, errorCode))
    const { run, queryClient } = await mount()

    await captureError(run())

    await waitFor(() => {
      expect(queryClient.getQueryState(['groups'])?.isInvalidated).toBe(true)
    })
  })

  it.each(groupStateChangedCodes)('asks the server again for the group that is on screen after 400 %s', async (errorCode) => {
    stubFetch(problemResponse(400, errorCode))
    const { run, queryClient } = await mount()
    const loadGroup = vi.fn(() => Promise.resolve(errorCode === 'GROUP_CLOSING' ? closingGroup : finishedGroup))
    const watcher = new QueryObserver(queryClient, {
      queryKey: ['groups', testGroupId],
      queryFn: loadGroup,
      staleTime: Infinity,
    })
    const stopWatching = watcher.subscribe(() => {})

    await captureError(run())

    await waitFor(() => {
      expect(loadGroup).toHaveBeenCalledTimes(1)
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
    [400, 'MEMBER_NAME_TAKEN'],
    [400, 'SETTLEMENT_AMOUNT_TOO_LARGE'],
    [400, 'GROUP_NO_EXPENSES'],
    [400, 'GROUP_NOT_KVIT'],
    [400, 'CLOSING_ALREADY_ANSWERED'],
    [404, 'GROUP_NOT_FOUND'],
  ])('leaves the group as it is when the server answers %i %s', async (status, errorCode) => {
    stubFetch(problemResponse(status, errorCode))
    const { run, queryClient } = await mount()

    await captureError(run())

    expect(queryClient.getQueryState(['groups', testGroupId])?.isInvalidated).toBe(false)
    expect(queryClient.getQueryState(['groups'])?.isInvalidated).toBe(false)
  })
})
