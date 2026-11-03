import { QueryObserver, type QueryClient } from '@tanstack/react-query'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { createQueryClient } from '@/core/api/queryClient'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { captureError, problemResponse } from '@/test/apiTestHelpers'
import { settlementsPath } from '@/test/balancesTestData'
import { hotelDetail } from '@/test/expenseTestData'
import { expensesPath } from '@/test/expenseTestHelpers'
import { testGroupId } from '@/test/groupTestData'
import { anaMember, filipMember } from '@/test/memberTestData'
import { createDeferred } from '@/test/outboxTestHelpers'
import { testOutboxItem } from '@/test/outboxTestData'
import { coffeeEntry, entryAnswerRow } from '@/test/personalEntryTestData'
import { jsonAnswer, requestCount, requestsOf, stubFetchByRequest } from '@/test/requestTestHelpers'
import { pendingPaymentOf } from '@/test/settlementActionsTestData'
import { createTestPersister } from '@/test/testPersister'
import { personalEntriesQueryKey } from '@/features/budget/entries/personalEntryCache'
import { personalSummaryQueryKey } from '@/features/budget/summary/personalSummaryCache'
import { refreshAfterQueuedItem, sendQueuedItem } from './outboxDispatch'

const oneBillPath = '/api/groups/one-bill'
const personalEntriesPath = '/api/personal/entries'
const createdBill = { groupId: testGroupId, expenseId: 'expense-1' }

const expensePayload = {
  clientRequestId: 'expense-request',
  title: 'Hotel',
  note: null,
  amountMinor: 300000,
  currency: 'MKD',
  expenseDate: '2026-10-19',
  categoryId: null,
  paidByMemberId: filipMember.id,
  splitType: 'Equal',
  shares: [{ memberId: filipMember.id, inputValue: 1 }],
}

const oneBillPayload = {
  clientRequestId: 'one-bill-request',
  title: 'Dinner',
  groupName: null,
  names: ['Marko'],
  note: null,
  amountMinor: 180000,
  currency: 'MKD',
  expenseDate: '2026-10-20',
  categoryId: null,
  paidByPersonIndex: 0,
  splitType: 'Equal',
  shares: [{ personIndex: 0, inputValue: 1 }],
}

const personalPayload = {
  clientRequestId: 'personal-request',
  kind: 'Income',
  amountMinor: 6500000,
  currency: 'MKD',
  entryDate: '2026-10-22',
  note: null,
}

const settlementPayload = {
  clientRequestId: 'settlement-request',
  fromMemberId: anaMember.id,
  toMemberId: filipMember.id,
  amountMinor: 80000,
  currency: 'MKD',
}

const expenseItem = testOutboxItem('expense-request', {
  kind: 'expense',
  groupId: testGroupId,
  payload: expensePayload,
})
const oneBillItem = testOutboxItem('one-bill-request', {
  kind: 'oneBill',
  groupId: null,
  payload: oneBillPayload,
})
const personalItem = testOutboxItem('personal-request', {
  kind: 'personalEntry',
  groupId: null,
  payload: personalPayload,
})
const settlementItem = testOutboxItem('settlement-request', {
  kind: 'settlement',
  groupId: testGroupId,
  payload: settlementPayload,
})

function allAnswers() {
  return {
    [`POST ${expensesPath}`]: jsonAnswer(hotelDetail),
    [`POST ${oneBillPath}`]: jsonAnswer(createdBill),
    [`POST ${personalEntriesPath}`]: jsonAnswer(entryAnswerRow(coffeeEntry)),
    [`POST ${settlementsPath}`]: jsonAnswer(pendingPaymentOf(anaMember, filipMember, 80000)),
  }
}

function createClient(): QueryClient {
  return createQueryClient(createTestPersister())
}

function isInvalidated(queryClient: QueryClient, key: readonly unknown[]): boolean {
  return queryClient.getQueryState(key)?.isInvalidated === true
}

afterEach(() => {
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe('sendQueuedItem', () => {
  it('posts an expense item to the expenses of its group with the payload as the body', async () => {
    const fetchMock = stubFetchByRequest(allAnswers())

    await sendQueuedItem(expenseItem)

    expect(requestsOf(fetchMock, 'POST', expensesPath)).toHaveLength(1)
    expect(requestsOf(fetchMock, 'POST', expensesPath)[0].body).toEqual(expensePayload)
    expect(requestsOf(fetchMock, 'POST', expensesPath)[0].contentType).toBe('application/json')
  })

  it('posts a One bill item to the One bill endpoint with the payload as the body', async () => {
    const fetchMock = stubFetchByRequest(allAnswers())

    await sendQueuedItem(oneBillItem)

    expect(requestsOf(fetchMock, 'POST', oneBillPath)).toHaveLength(1)
    expect(requestsOf(fetchMock, 'POST', oneBillPath)[0].body).toEqual(oneBillPayload)
  })

  it('posts a personal entry item to the personal entries with the payload as the body', async () => {
    const fetchMock = stubFetchByRequest(allAnswers())

    await sendQueuedItem(personalItem)

    expect(requestsOf(fetchMock, 'POST', personalEntriesPath)).toHaveLength(1)
    expect(requestsOf(fetchMock, 'POST', personalEntriesPath)[0].body).toEqual(personalPayload)
  })

  it('posts a settlement item to the payments of its group with the payload as the body', async () => {
    const fetchMock = stubFetchByRequest(allAnswers())

    await sendQueuedItem(settlementItem)

    expect(requestsOf(fetchMock, 'POST', settlementsPath)).toHaveLength(1)
    expect(requestsOf(fetchMock, 'POST', settlementsPath)[0].body).toEqual(settlementPayload)
  })

  it('sends each item once and no other request', async () => {
    const fetchMock = stubFetchByRequest(allAnswers())

    await sendQueuedItem(expenseItem)
    await sendQueuedItem(oneBillItem)

    expect(fetchMock).toHaveBeenCalledTimes(2)
  })

  it('sends the group of the item, not another one', async () => {
    const otherGroupPath = '/api/groups/other-group/expenses'
    const fetchMock = stubFetchByRequest({ [`POST ${otherGroupPath}`]: jsonAnswer(hotelDetail) })

    await sendQueuedItem({ ...expenseItem, groupId: 'other-group' })

    expect(requestCount(fetchMock, 'POST', otherGroupPath)).toBe(1)
  })

  it('answers with what the server answered', async () => {
    stubFetchByRequest(allAnswers())

    const created = await sendQueuedItem(oneBillItem)

    expect(created).toEqual(createdBill)
  })

  it('fails with the error of the server for the sender to judge', async () => {
    stubFetchByRequest({
      [`POST ${expensesPath}`]: () => problemResponse(400, 'GROUP_FINISHED'),
    })

    const error = await captureError(sendQueuedItem(expenseItem))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 400, errorCode: 'GROUP_FINISHED' })
  })

  it('fails with a network error as an ApiError without a status', async () => {
    stubFetchByRequest({
      [`POST ${expensesPath}`]: () => new TypeError('Failed to fetch'),
    })

    const error = await captureError(sendQueuedItem(expenseItem))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })
})

describe('sendQueuedItem with a payload that is not usable', () => {
  const malformedItems: [string, OutboxItem][] = [
    ['a payload that is null', { ...expenseItem, payload: null }],
    ['a payload that is a text', { ...expenseItem, payload: 'Hotel' }],
    ['a payload that is a number', { ...oneBillItem, payload: 42 }],
    ['a payload that is a list', { ...personalItem, payload: [expensePayload] }],
    ['a payload without clientRequestId', { ...expenseItem, payload: { ...expensePayload, clientRequestId: undefined } }],
    ['a payload with a clientRequestId that is not a text', { ...oneBillItem, payload: { ...oneBillPayload, clientRequestId: 7 } }],
    ['a payload with another clientRequestId than the id of the item', { ...settlementItem, payload: { ...settlementPayload, clientRequestId: 'someone-else' } }],
    ['an expense item without a group', { ...expenseItem, groupId: null }],
    ['a settlement item without a group', { ...settlementItem, groupId: null }],
  ]

  it.each(malformedItems)('fails with a clear error and sends nothing for %s', async (_name, item) => {
    const fetchMock = stubFetchByRequest(allAnswers())

    const error = await captureError((async () => sendQueuedItem(item))())

    expect(error).toBeInstanceOf(Error)
    expect(error).not.toBeInstanceOf(ApiError)
    expect((error as Error).message).toContain(item.kind)
    expect((error as Error).message).toContain(item.id)
    expect(fetchMock).not.toHaveBeenCalled()
  })
})

describe('sendQueuedItem without a group', () => {
  it('sends a One bill item and a personal item that have no group', async () => {
    const fetchMock = stubFetchByRequest(allAnswers())

    await sendQueuedItem(oneBillItem)
    await sendQueuedItem(personalItem)

    expect(fetchMock).toHaveBeenCalledTimes(2)
  })
})

describe('refreshAfterQueuedItem', () => {
  const groupKeys = {
    expenses: ['groups', testGroupId, 'expenses'],
    expenseDetail: ['groups', testGroupId, 'expenses', 'expense-1'],
    activity: ['groups', testGroupId, 'activity'],
    balances: ['groups', testGroupId, 'balances'],
    spending: ['groups', testGroupId, 'spending'],
    settlements: ['groups', testGroupId, 'settlements'],
    members: ['groups', testGroupId, 'members'],
    group: ['groups', testGroupId],
    list: ['groups'],
  } as const

  function clientWithGroupCached(): QueryClient {
    const queryClient = createClient()
    for (const key of Object.values(groupKeys)) {
      queryClient.setQueryData(key, [])
    }
    queryClient.setQueryData(['groups', 'other-group'], [])
    return queryClient
  }

  it('marks what an added expense changes in its group as out of date', async () => {
    const queryClient = clientWithGroupCached()

    await refreshAfterQueuedItem(queryClient, expenseItem)

    for (const key of [
      groupKeys.expenses,
      groupKeys.expenseDetail,
      groupKeys.activity,
      groupKeys.balances,
      groupKeys.spending,
      groupKeys.group,
      groupKeys.list,
    ]) {
      expect(isInvalidated(queryClient, key), JSON.stringify(key)).toBe(true)
    }
  })

  it('leaves the other groups alone after an expense', async () => {
    const queryClient = clientWithGroupCached()

    await refreshAfterQueuedItem(queryClient, expenseItem)

    expect(isInvalidated(queryClient, ['groups', 'other-group'])).toBe(false)
  })

  it('marks the list of groups and the home screen as out of date after a One bill', async () => {
    const queryClient = clientWithGroupCached()
    queryClient.setQueryData(['dashboard'], {})

    await refreshAfterQueuedItem(queryClient, oneBillItem)

    expect(isInvalidated(queryClient, groupKeys.list)).toBe(true)
    expect(isInvalidated(queryClient, ['dashboard'])).toBe(true)
  })

  it('does not mark a single group as out of date after a One bill, because none is known yet', async () => {
    const queryClient = clientWithGroupCached()

    await refreshAfterQueuedItem(queryClient, oneBillItem)

    expect(isInvalidated(queryClient, groupKeys.group)).toBe(false)
    expect(isInvalidated(queryClient, groupKeys.expenses)).toBe(false)
  })

  it('marks the personal entries and every summary as out of date after a personal entry', async () => {
    const queryClient = createClient()
    queryClient.setQueryData(personalEntriesQueryKey('2026-10'), 'the October answer')
    queryClient.setQueryData(personalSummaryQueryKey('2026-10'), 'the October summary')
    queryClient.setQueryData(personalSummaryQueryKey('2026-09'), 'the September summary')

    await refreshAfterQueuedItem(queryClient, personalItem)

    expect(isInvalidated(queryClient, personalEntriesQueryKey('2026-10'))).toBe(true)
    expect(isInvalidated(queryClient, personalSummaryQueryKey('2026-10'))).toBe(true)
    expect(isInvalidated(queryClient, personalSummaryQueryKey('2026-09'))).toBe(true)
  })

  it('leaves the groups alone after a personal entry', async () => {
    const queryClient = clientWithGroupCached()

    await refreshAfterQueuedItem(queryClient, personalItem)

    expect(isInvalidated(queryClient, groupKeys.group)).toBe(false)
    expect(isInvalidated(queryClient, groupKeys.balances)).toBe(false)
  })

  it('marks what a recorded payment changes in its group as out of date', async () => {
    const queryClient = clientWithGroupCached()

    await refreshAfterQueuedItem(queryClient, settlementItem)

    for (const key of [
      groupKeys.balances,
      groupKeys.settlements,
      groupKeys.activity,
      groupKeys.expenses,
      groupKeys.members,
      groupKeys.group,
      groupKeys.list,
    ]) {
      expect(isInvalidated(queryClient, key), JSON.stringify(key)).toBe(true)
    }
  })

  it('leaves the other groups alone after a payment', async () => {
    const queryClient = clientWithGroupCached()

    await refreshAfterQueuedItem(queryClient, settlementItem)

    expect(isInvalidated(queryClient, ['groups', 'other-group'])).toBe(false)
  })

  it('waits until the screens that are open have asked again before it is done', async () => {
    const queryClient = createClient()
    const refetch = createDeferred<string>()
    const queryFn = vi.fn<() => Promise<string>>().mockResolvedValueOnce('first answer')
    queryClient.mount()
    const observer = new QueryObserver(queryClient, { queryKey: [...groupKeys.balances], queryFn })
    const unsubscribe = observer.subscribe(() => {})
    await vi.waitFor(() => {
      expect(observer.getCurrentResult().isSuccess).toBe(true)
    })
    queryFn.mockImplementationOnce(() => refetch.promise)
    const onDone = vi.fn<() => void>()

    const refreshing = refreshAfterQueuedItem(queryClient, settlementItem).then(onDone)
    await new Promise((resolve) => setTimeout(resolve, 20))

    expect(queryFn).toHaveBeenCalledTimes(2)
    expect(onDone).not.toHaveBeenCalled()
    refetch.resolve('second answer')
    await refreshing
    expect(onDone).toHaveBeenCalledOnce()
    unsubscribe()
    queryClient.unmount()
  })

  it('answers with nothing', async () => {
    const queryClient = clientWithGroupCached()

    await expect(refreshAfterQueuedItem(queryClient, expenseItem)).resolves.toBeUndefined()
  })

  it('does not send any request', async () => {
    const fetchMock = stubFetchByRequest({})

    await refreshAfterQueuedItem(clientWithGroupCached(), expenseItem)

    expect(fetchMock).not.toHaveBeenCalled()
  })
})
