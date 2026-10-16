import { QueryObserver, type QueryClient } from '@tanstack/react-query'
import { act, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { captureError, problemResponse, stubFetch } from '@/test/apiTestHelpers'
import { markoPaidFilipPending } from '@/test/balancesTestData'
import { fullDashboard, quietDashboard } from '@/test/dashboardTestData'
import { accommodationCategory, hotelDetail, hotelExpenseId } from '@/test/expenseTestData'
import { greeceGroupRow, groupListOf, testGroup, testGroupId } from '@/test/groupTestData'
import { anaMember, filipMember, markoMember } from '@/test/memberTestData'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { useCreateExpense } from '@/features/expenses/add/hooks/useCreateExpense'
import { useDeleteExpense } from '@/features/expenses/detail/hooks/useDeleteExpense'
import { useUpdateExpense } from '@/features/expenses/edit/hooks/useUpdateExpense'
import { useCreateOneBill } from '@/features/expenses/oneBill/hooks/useCreateOneBill'
import { useRestoreExpense } from '@/features/expenses/shared/hooks/useRestoreExpense'
import { useConfirmClosing } from '@/features/groups/closing/hooks/useConfirmClosing'
import { useObjectToClosing } from '@/features/groups/closing/hooks/useObjectToClosing'
import { useStartClosing } from '@/features/groups/closing/hooks/useStartClosing'
import { useUnlockGroup } from '@/features/groups/closing/hooks/useUnlockGroup'
import { useAddMember } from '@/features/groups/group/hooks/useAddMember'
import { useJoinGroup } from '@/features/groups/join/hooks/useJoinGroup'
import { useRestoreGroup } from '@/features/groups/list/hooks/useRestoreGroup'
import { useClaimName } from '@/features/groups/members/hooks/useClaimName'
import { useLetBackIn } from '@/features/groups/members/hooks/useLetBackIn'
import { useMakeOwner } from '@/features/groups/members/hooks/useMakeOwner'
import { useRemoveMember } from '@/features/groups/members/hooks/useRemoveMember'
import { useResetInviteLink } from '@/features/groups/members/hooks/useResetInviteLink'
import { useUndoClaim } from '@/features/groups/members/hooks/useUndoClaim'
import { useUndoResetInviteLink } from '@/features/groups/members/hooks/useUndoResetInviteLink'
import { useCreateGroup } from '@/features/groups/newGroup/hooks/useCreateGroup'
import { useDeleteGroup } from '@/features/groups/settings/hooks/useDeleteGroup'
import { useLeaveGroup } from '@/features/groups/settings/hooks/useLeaveGroup'
import { useUpdateGroup } from '@/features/groups/settings/hooks/useUpdateGroup'
import { useDeleteSettlement } from '@/features/settlements/delete/hooks/useDeleteSettlement'
import { useRestoreSettlement } from '@/features/settlements/delete/hooks/useRestoreSettlement'
import { useAnswerSettlement } from '@/features/settlements/pending/hooks/useAnswerSettlement'
import { useCancelSettlement } from '@/features/settlements/pending/hooks/useCancelSettlement'
import { useRecordSettlement } from '@/features/settlements/record/hooks/useRecordSettlement'
import { dashboardKey } from './dashboardCache'

interface Mounted {
  queryClient: QueryClient
  run: () => Promise<unknown>
  answer: Response
}

type Mount = () => Promise<Mounted>

const settlementId = markoPaidFilipPending.id
const clientRequestId = '5b1f2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d'

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

const oneBillInput = {
  clientRequestId,
  title: null,
  groupName: null,
  names: ['Marko'],
  note: null,
  amountMinor: 30000,
  currency: 'MKD' as const,
  expenseDate: '2026-10-06',
  categoryId: null,
  paidByPersonIndex: 0,
  splitType: 'Equal' as const,
  shares: [
    { personIndex: 0, inputValue: 0 },
    { personIndex: 1, inputValue: 0 },
  ],
}

function seedDashboardGroupsAndList(queryClient: QueryClient): void {
  queryClient.setQueryData(dashboardKey, fullDashboard)
  queryClient.setQueryData(['groups', testGroupId], testGroup)
  queryClient.setQueryData(['groups'], groupListOf({ groups: [greeceGroupRow] }))
}

function noContent(): Response {
  return new Response(null, { status: 204 })
}

function mountHook<Variables>(
  useHook: () => { mutateAsync: (variables: Variables) => Promise<unknown> },
  variables: Variables,
  answer: () => Response,
): Mount {
  return async () => {
    const { result, queryClient } = await renderHookWithProviders(useHook, {
      seedCache: seedDashboardGroupsAndList,
    })
    return { queryClient, answer: answer(), run: () => act(() => result.current.mutateAsync(variables)) }
  }
}

const expenseChangesList: [string, Mount][] = [
  [
    'adding an expense',
    mountHook(() => useCreateExpense(testGroupId), { clientRequestId, ...expenseChanges }, () => Response.json(hotelDetail)),
  ],
  [
    'editing an expense',
    mountHook(() => useUpdateExpense(testGroupId, hotelExpenseId), expenseChanges, noContent),
  ],
  ['deleting an expense', mountHook(() => useDeleteExpense(testGroupId, hotelExpenseId), undefined, noContent)],
  ['restoring an expense', mountHook(() => useRestoreExpense(testGroupId), hotelExpenseId, noContent)],
  [
    'creating a One bill',
    mountHook(() => useCreateOneBill(), oneBillInput, () =>
      Response.json({ groupId: testGroupId, expenseId: hotelExpenseId }),
    ),
  ],
]

const settlementChanges: [string, Mount][] = [
  [
    'recording a payment',
    mountHook(
      () => useRecordSettlement(testGroupId),
      {
        clientRequestId,
        fromMemberId: markoMember.id,
        toMemberId: filipMember.id,
        amountMinor: 80000,
        currency: 'MKD' as const,
      },
      () => Response.json(markoPaidFilipPending),
    ),
  ],
  ['confirming a payment', mountHook(() => useAnswerSettlement(testGroupId, settlementId), 'confirm' as const, noContent)],
  ['rejecting a payment', mountHook(() => useAnswerSettlement(testGroupId, settlementId), 'reject' as const, noContent)],
  ['cancelling a payment', mountHook(() => useCancelSettlement(testGroupId, settlementId), undefined, noContent)],
  ['deleting a payment', mountHook(() => useDeleteSettlement(testGroupId, settlementId), undefined, noContent)],
  ['restoring a payment', mountHook(() => useRestoreSettlement(testGroupId), settlementId, noContent)],
]

const closingChanges: [string, Mount][] = [
  ['starting a closing', mountHook(() => useStartClosing(testGroupId), undefined, noContent)],
  ['confirming a closing', mountHook(() => useConfirmClosing(testGroupId), undefined, noContent)],
  ['objecting to a closing', mountHook(() => useObjectToClosing(testGroupId), 'Not yet', noContent)],
  ['objecting to a closing with no reason', mountHook(() => useObjectToClosing(testGroupId), undefined, noContent)],
  ['unlocking a group', mountHook(() => useUnlockGroup(testGroupId), undefined, noContent)],
]

const groupChanges: [string, Mount][] = [
  [
    'creating a group',
    mountHook(() => useCreateGroup(), { name: 'Greece trip', emoji: testGroup.emoji, currency: 'MKD' as const }, () =>
      Response.json(testGroup),
    ),
  ],
  [
    'updating a group',
    mountHook(
      () => useUpdateGroup(testGroupId),
      { name: 'Greece 2026', emoji: testGroup.emoji, currency: 'MKD' as const },
      noContent,
    ),
  ],
  ['deleting a group', mountHook(() => useDeleteGroup(testGroupId), undefined, noContent)],
  ['restoring a group', mountHook(() => useRestoreGroup(), testGroupId, noContent)],
  ['leaving a group', mountHook(() => useLeaveGroup(testGroupId), undefined, noContent)],
  ['joining a group', mountHook(() => useJoinGroup(), { token: 'q3Fz8-mXk2' }, () => Response.json({ groupId: testGroupId }))],
]

const memberChanges: [string, Mount][] = [
  ['adding a name', mountHook(() => useAddMember(testGroupId), 'Grandma', noContent)],
  ['removing a member', mountHook(() => useRemoveMember(testGroupId), markoMember.id, noContent)],
  ['letting a member back in', mountHook(() => useLetBackIn(testGroupId), markoMember.id, noContent)],
  ['making someone the owner', mountHook(() => useMakeOwner(testGroupId), anaMember.id, noContent)],
  ['claiming a name', mountHook(() => useClaimName(testGroupId), markoMember.id, noContent)],
  ['undoing a claim', mountHook(() => useUndoClaim(testGroupId), markoMember.id, noContent)],
  [
    'resetting the invite link',
    mountHook(() => useResetInviteLink(testGroupId), undefined, () => Response.json({ inviteToken: 'new-token' })),
  ],
  [
    'undoing the reset of the invite link',
    mountHook(() => useUndoResetInviteLink(testGroupId), undefined, () => Response.json({ inviteToken: 'old-token' })),
  ],
]

const everyChange: [string, Mount][] = [
  ...expenseChangesList,
  ...settlementChanges,
  ...closingChanges,
  ...groupChanges,
  ...memberChanges,
]

describe.each(everyChange)('the dashboard after a successful change: %s', (_name, mount) => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('is marked as out of date, because the totals, the groups, Needs you and the activity may have changed', async () => {
    const { run, queryClient, answer } = await mount()
    stubFetch(answer)

    await run()

    expect(queryClient.getQueryState(dashboardKey)?.isInvalidated).toBe(true)
  })

  it('is asked for again when the home screen is looking at it, so it shows without a reload', async () => {
    const { run, queryClient, answer } = await mount()
    stubFetch(answer)
    const loadDashboard = vi.fn(() => Promise.resolve(quietDashboard))
    const watcher = new QueryObserver(queryClient, {
      queryKey: dashboardKey,
      queryFn: loadDashboard,
      staleTime: Infinity,
    })
    const stopWatching = watcher.subscribe(() => {})

    await run()

    await waitFor(() => {
      expect(loadDashboard).toHaveBeenCalledTimes(1)
    })
    expect(queryClient.getQueryData(dashboardKey)).toEqual(quietDashboard)
    stopWatching()
  })

  it('is left as it is when the server refuses the change with a 403 that is not about the state of the group', async () => {
    const { run, queryClient } = await mount()
    stubFetch(problemResponse(403, 'GROUP_NOT_OWNER'))

    await captureError(run())

    expect(queryClient.getQueryState(dashboardKey)?.isInvalidated).toBe(false)
  })
})
