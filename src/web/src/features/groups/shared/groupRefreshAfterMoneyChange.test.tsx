import { QueryObserver, type QueryClient } from '@tanstack/react-query'
import { act, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { stubFetch } from '@/test/apiTestHelpers'
import { markoPaidFilipPending } from '@/test/balancesTestData'
import { readyToCloseGroup } from '@/test/closingTestData'
import { accommodationCategory, hotelDetail, hotelExpenseId } from '@/test/expenseTestData'
import { greeceGroupRow, groupListOf, testGroup, testGroupId } from '@/test/groupTestData'
import { anaMember, filipMember, markoMember } from '@/test/memberTestData'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { useCreateExpense } from '@/features/expenses/add/hooks/useCreateExpense'
import { useDeleteExpense } from '@/features/expenses/detail/hooks/useDeleteExpense'
import { useUpdateExpense } from '@/features/expenses/edit/hooks/useUpdateExpense'
import { useRestoreExpense } from '@/features/expenses/shared/hooks/useRestoreExpense'
import { useDeleteSettlement } from '@/features/settlements/delete/hooks/useDeleteSettlement'
import { useRestoreSettlement } from '@/features/settlements/delete/hooks/useRestoreSettlement'
import { useAnswerSettlement } from '@/features/settlements/pending/hooks/useAnswerSettlement'
import { useCancelSettlement } from '@/features/settlements/pending/hooks/useCancelSettlement'
import { useRecordSettlement } from '@/features/settlements/record/hooks/useRecordSettlement'

interface Mounted {
  queryClient: QueryClient
  run: () => Promise<unknown>
  answer: Response
}

type Mount = () => Promise<Mounted>

const otherGroupId = 'f0e1d2c3-b4a5-4968-8776-655443322110'
const settlementId = markoPaidFilipPending.id

const expenseChanges = {
  title: 'Hotel',
  note: null,
  amountMinor: 300000,
  currency: 'MKD' as const,
  expenseDate: '2026-10-03',
  categoryId: accommodationCategory.id,
  paidByMemberId: filipMember.id,
  splitType: 'Equal' as const,
  shares: [
    { memberId: filipMember.id, inputValue: 0 },
    { memberId: anaMember.id, inputValue: 0 },
  ],
}

function seedGroupsAndList(queryClient: QueryClient): void {
  queryClient.setQueryData(['groups', testGroupId], testGroup)
  queryClient.setQueryData(['groups', otherGroupId], { ...testGroup, id: otherGroupId })
  queryClient.setQueryData(['groups'], groupListOf({ groups: [greeceGroupRow] }))
}

function noContent(): Response {
  return new Response(null, { status: 204 })
}

const mountCreateExpense: Mount = async () => {
  const { result, queryClient } = await renderHookWithProviders(() => useCreateExpense(testGroupId), {
    seedCache: seedGroupsAndList,
  })
  return {
    queryClient,
    answer: Response.json(hotelDetail),
    run: () => act(() => result.current.mutateAsync({ clientRequestId: '5b1f2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d', ...expenseChanges })),
  }
}

const mountUpdateExpense: Mount = async () => {
  const { result, queryClient } = await renderHookWithProviders(() => useUpdateExpense(testGroupId, hotelExpenseId), {
    seedCache: seedGroupsAndList,
  })
  return { queryClient, answer: noContent(), run: () => act(() => result.current.mutateAsync(expenseChanges)) }
}

const mountDeleteExpense: Mount = async () => {
  const { result, queryClient } = await renderHookWithProviders(() => useDeleteExpense(testGroupId, hotelExpenseId), {
    seedCache: seedGroupsAndList,
  })
  return { queryClient, answer: noContent(), run: () => act(() => result.current.mutateAsync()) }
}

const mountRestoreExpense: Mount = async () => {
  const { result, queryClient } = await renderHookWithProviders(() => useRestoreExpense(testGroupId), {
    seedCache: seedGroupsAndList,
  })
  return { queryClient, answer: noContent(), run: () => act(() => result.current.mutateAsync(hotelExpenseId)) }
}

const mountRecordSettlement: Mount = async () => {
  const { result, queryClient } = await renderHookWithProviders(() => useRecordSettlement(testGroupId), {
    seedCache: seedGroupsAndList,
  })
  return {
    queryClient,
    answer: Response.json(markoPaidFilipPending),
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

function mountAnswerSettlement(answer: 'confirm' | 'reject'): Mount {
  return async () => {
    const { result, queryClient } = await renderHookWithProviders(() => useAnswerSettlement(testGroupId, settlementId), {
      seedCache: seedGroupsAndList,
    })
    return { queryClient, answer: noContent(), run: () => act(() => result.current.mutateAsync(answer)) }
  }
}

const mountCancelSettlement: Mount = async () => {
  const { result, queryClient } = await renderHookWithProviders(() => useCancelSettlement(testGroupId, settlementId), {
    seedCache: seedGroupsAndList,
  })
  return { queryClient, answer: noContent(), run: () => act(() => result.current.mutateAsync()) }
}

const mountDeleteSettlement: Mount = async () => {
  const { result, queryClient } = await renderHookWithProviders(() => useDeleteSettlement(testGroupId, settlementId), {
    seedCache: seedGroupsAndList,
  })
  return { queryClient, answer: noContent(), run: () => act(() => result.current.mutateAsync()) }
}

const mountRestoreSettlement: Mount = async () => {
  const { result, queryClient } = await renderHookWithProviders(() => useRestoreSettlement(testGroupId), {
    seedCache: seedGroupsAndList,
  })
  return { queryClient, answer: noContent(), run: () => act(() => result.current.mutateAsync(settlementId)) }
}

const moneyChanges: [string, Mount][] = [
  ['adding an expense', mountCreateExpense],
  ['editing an expense', mountUpdateExpense],
  ['deleting an expense', mountDeleteExpense],
  ['restoring an expense', mountRestoreExpense],
  ['recording a payment', mountRecordSettlement],
  ['confirming a payment', mountAnswerSettlement('confirm')],
  ['rejecting a payment', mountAnswerSettlement('reject')],
  ['cancelling a payment', mountCancelSettlement],
  ['deleting a payment', mountDeleteSettlement],
  ['restoring a payment', mountRestoreSettlement],
]

describe.each(moneyChanges)('the group after a successful money change: %s', (_name, mount) => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('marks the group as out of date, because its status and canStartClosing depend on the money', async () => {
    const { run, queryClient, answer } = await mount()
    stubFetch(answer)

    await run()

    expect(queryClient.getQueryState(['groups', testGroupId])?.isInvalidated).toBe(true)
  })

  it('marks the group list as out of date, because a group can move to Finished', async () => {
    const { run, queryClient, answer } = await mount()
    stubFetch(answer)

    await run()

    expect(queryClient.getQueryState(['groups'])?.isInvalidated).toBe(true)
  })

  it('does not mark another group as out of date', async () => {
    const { run, queryClient, answer } = await mount()
    stubFetch(answer)

    await run()

    expect(queryClient.getQueryState(['groups', otherGroupId])?.isInvalidated).toBe(false)
  })

  it('asks the server again for the group that is on screen, so a new status shows without a reload', async () => {
    const { run, queryClient, answer } = await mount()
    stubFetch(answer)
    const loadGroup = vi.fn(() => Promise.resolve(readyToCloseGroup))
    const watcher = new QueryObserver(queryClient, {
      queryKey: ['groups', testGroupId],
      queryFn: loadGroup,
      staleTime: Infinity,
    })
    const stopWatching = watcher.subscribe(() => {})

    await run()

    await waitFor(() => {
      expect(loadGroup).toHaveBeenCalledTimes(1)
    })
    expect(queryClient.getQueryData(['groups', testGroupId])).toEqual(readyToCloseGroup)
    stopWatching()
  })
})
