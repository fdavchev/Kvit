import type { QueryClient } from '@tanstack/react-query'
import { act, cleanup, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { reactToFailedRequest } from '@/core/api/queryClient'
import { meQueryKey } from '@/core/auth/useMe'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import type { OutboxSender } from '@/core/outbox/outboxSender'
import { useOutboxItems, useOutboxSender } from '@/core/outbox/useOutbox'
import { hotelDetail } from '@/test/expenseTestData'
import { expensesPath } from '@/test/expenseTestHelpers'
import { testGroupId } from '@/test/groupTestData'
import { fakeIdbKeyval, seedOutbox, storedIds, storedItems } from '@/test/outboxTestHelpers'
import { testOutboxItem } from '@/test/outboxTestData'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { jsonAnswer, neverAnswer, problemAnswer, requestCount, requestsOf, stubFetchByRequest } from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { SignedInOutbox } from './SignedInOutbox'

vi.mock('@/core/api/queryClient', async (importOriginal) => {
  const original = await importOriginal<typeof import('@/core/api/queryClient')>()
  return { ...original, reactToFailedRequest: vi.fn(original.reactToFailedRequest) }
})

const personX = { ...testMe, id: 'person-x' }
const personY = { ...testMe, id: 'person-y' }

const expensePayload = {
  clientRequestId: 'waiting-expense',
  title: 'Hotel',
  note: null,
  amountMinor: 300000,
  currency: 'MKD',
  expenseDate: '2026-10-19',
  categoryId: null,
  paidByMemberId: 'member-1',
  splitType: 'Equal',
  shares: [{ memberId: 'member-1', inputValue: 1 }],
}

function waitingExpense(id = 'waiting-expense', overrides: Partial<OutboxItem> = {}): OutboxItem {
  return testOutboxItem(id, {
    kind: 'expense',
    groupId: testGroupId,
    payload: { ...expensePayload, clientRequestId: id },
    ...overrides,
  })
}

function itemsText(...ids: string[]): string {
  return `items: [${ids.join(',')}]`
}

function Probe({ onSender }: { onSender: (sender: OutboxSender) => void }) {
  const sender = useOutboxSender()
  const items = useOutboxItems()
  onSender(sender)
  return <p>{itemsText(...items.map((item) => item.id))}</p>
}

async function renderSignedInOutbox(options: { seedQueryCache?: (queryClient: QueryClient) => void } = {}) {
  const senders: OutboxSender[] = []
  const rendered = await renderElementWithProviders(
    <SignedInOutbox>
      <Probe onSender={(sender) => senders.push(sender)} />
    </SignedInOutbox>,
    '/outbox',
    {
      seedCache: (queryClient) => {
        seedMe(personX)(queryClient)
        options.seedQueryCache?.(queryClient)
      },
    },
  )
  return { ...rendered, senders }
}

beforeEach(() => {
  vi.clearAllMocks()
})

afterEach(() => {
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe('SignedInOutbox', () => {
  it('gives its children a sender of their own, not the one outside it', async () => {
    stubFetchByRequest({})

    const { senders, outboxSender } = await renderSignedInOutbox()

    expect(senders.length).toBeGreaterThan(0)
    expect(senders[0]).not.toBe(outboxSender)
  })

  it('shows the saved items of the signed-in person and no item of another person', async () => {
    stubFetchByRequest({ [`POST ${expensesPath}`]: neverAnswer() })
    seedOutbox(personX.id, [waitingExpense('mine')])
    seedOutbox(personY.id, [waitingExpense('theirs')])

    await renderSignedInOutbox()

    expect(await screen.findByText(itemsText('mine'))).toBeTruthy()
    expect(screen.queryByText(itemsText('theirs'))).toBeNull()
  })

  it('starts sending the saved items as soon as it is mounted, with the payload of the item as the body', async () => {
    const fetchMock = stubFetchByRequest({ [`POST ${expensesPath}`]: jsonAnswer(hotelDetail) })
    seedOutbox(personX.id, [waitingExpense()])

    await renderSignedInOutbox()

    await waitFor(() => {
      expect(requestCount(fetchMock, 'POST', expensesPath)).toBe(1)
    })
    expect(requestsOf(fetchMock, 'POST', expensesPath)[0].body).toEqual(expensePayload)
  })

  it('removes a saved item from the browser once the server has it', async () => {
    stubFetchByRequest({ [`POST ${expensesPath}`]: jsonAnswer(hotelDetail) })
    seedOutbox(personX.id, [waitingExpense()])

    await renderSignedInOutbox()

    await waitFor(() => {
      expect(storedIds(personX.id)).toEqual([])
    })
  })

  it('marks the screens of the saved group as out of date after the item was sent', async () => {
    stubFetchByRequest({ [`POST ${expensesPath}`]: jsonAnswer(hotelDetail) })
    seedOutbox(personX.id, [waitingExpense()])
    const activityKey = ['groups', testGroupId, 'activity']

    const { queryClient } = await renderSignedInOutbox({
      seedQueryCache: (queryClient) => {
        queryClient.setQueryData(activityKey, [])
      },
    })

    await waitFor(() => {
      expect(queryClient.getQueryState(activityKey)?.isInvalidated).toBe(true)
    })
  })

  it('sends the item only once when it renders again', async () => {
    const fetchMock = stubFetchByRequest({ [`POST ${expensesPath}`]: neverAnswer() })
    seedOutbox(personX.id, [waitingExpense()])
    const { queryClient } = await renderSignedInOutbox()
    await waitFor(() => {
      expect(requestCount(fetchMock, 'POST', expensesPath)).toBe(1)
    })

    act(() => {
      queryClient.setQueryData(meQueryKey, { ...personX, displayName: 'Filip D.' })
    })
    await new Promise((resolve) => setTimeout(resolve, 20))

    expect(requestCount(fetchMock, 'POST', expensesPath)).toBe(1)
  })

  it('keeps the same sender while the signed-in person stays the same', async () => {
    stubFetchByRequest({})
    const { queryClient, senders } = await renderSignedInOutbox()

    act(() => {
      queryClient.setQueryData(meQueryKey, { ...personX, displayName: 'Filip D.' })
    })
    await new Promise((resolve) => setTimeout(resolve, 20))

    expect(new Set(senders).size).toBe(1)
  })

  it('makes a new sender with the saved items of the other person when the signed-in person changes', async () => {
    stubFetchByRequest({ [`POST ${expensesPath}`]: neverAnswer() })
    seedOutbox(personX.id, [waitingExpense('mine')])
    seedOutbox(personY.id, [waitingExpense('theirs')])
    const { queryClient, senders } = await renderSignedInOutbox()
    await screen.findByText(itemsText('mine'))

    act(() => {
      queryClient.setQueryData(meQueryKey, personY)
    })

    expect(await screen.findByText(itemsText('theirs'))).toBeTruthy()
    expect(new Set(senders).size).toBe(2)
  })

  it('does not delete the saved items of the first person when the signed-in person changes', async () => {
    stubFetchByRequest({ [`POST ${expensesPath}`]: neverAnswer() })
    seedOutbox(personX.id, [waitingExpense('mine')])
    const { queryClient } = await renderSignedInOutbox()
    await screen.findByText(itemsText('mine'))

    act(() => {
      queryClient.setQueryData(meQueryKey, personY)
    })
    await screen.findByText(itemsText())

    expect(storedIds(personX.id)).toEqual(['mine'])
  })

  it('runs the sender once each time the browser comes back online, also after it rendered again', async () => {
    stubFetchByRequest({})
    const { queryClient, senders } = await renderSignedInOutbox()
    const run = vi.spyOn(senders[0], 'run')
    act(() => {
      queryClient.setQueryData(meQueryKey, { ...personX, displayName: 'Filip D.' })
    })
    await new Promise((resolve) => setTimeout(resolve, 20))

    window.dispatchEvent(new Event('online'))

    expect(run).toHaveBeenCalledTimes(1)
  })

  it('stops running the sender on the online event when it goes away', async () => {
    stubFetchByRequest({})
    const { senders } = await renderSignedInOutbox()
    const run = vi.spyOn(senders[0], 'run')

    cleanup()
    window.dispatchEvent(new Event('online'))

    expect(run).not.toHaveBeenCalled()
  })

  it('runs only the sender of the new person on the online event after the signed-in person changed', async () => {
    stubFetchByRequest({})
    const { queryClient, senders } = await renderSignedInOutbox()
    const firstRun = vi.spyOn(senders[0], 'run')
    act(() => {
      queryClient.setQueryData(meQueryKey, personY)
    })
    await waitFor(() => {
      expect(new Set(senders).size).toBe(2)
    })
    const secondRun = vi.spyOn(senders[senders.length - 1], 'run')

    window.dispatchEvent(new Event('online'))

    expect(senders[senders.length - 1]).not.toBe(senders[0])
    expect(firstRun).not.toHaveBeenCalled()
    expect(secondRun).toHaveBeenCalledTimes(1)
  })

  it('keeps the saved items in the browser when it goes away', async () => {
    stubFetchByRequest({ [`POST ${expensesPath}`]: neverAnswer() })
    seedOutbox(personX.id, [waitingExpense('mine')])
    await renderSignedInOutbox()
    await screen.findByText(itemsText('mine'))

    cleanup()

    expect(storedIds(personX.id)).toEqual(['mine'])
    expect(fakeIdbKeyval.del).not.toHaveBeenCalled()
  })
})

describe('SignedInOutbox when an item cannot be sent', () => {
  it('marks an item that the server refused as failed with the code and the status of the refusal', async () => {
    stubFetchByRequest({ [`POST ${expensesPath}`]: problemAnswer(400, 'EXPENSE_AMOUNT_NOT_POSITIVE') })
    seedOutbox(personX.id, [waitingExpense()])

    await renderSignedInOutbox()

    await waitFor(() => {
      expect(storedItems(personX.id)[0]).toMatchObject({
        state: 'failed',
        failure: { errorCode: 'EXPENSE_AMOUNT_NOT_POSITIVE', httpStatus: 400 },
      })
    })
  })

  it('lets the rest of the app react to the refusal of an API error before the sender judges it', async () => {
    stubFetchByRequest({ [`POST ${expensesPath}`]: problemAnswer(400, 'GROUP_FINISHED') })
    seedOutbox(personX.id, [waitingExpense()])

    const { queryClient } = await renderSignedInOutbox()

    await waitFor(() => {
      expect(storedItems(personX.id)[0]?.state).toBe('failed')
    })
    expect(reactToFailedRequest).toHaveBeenCalledTimes(1)
    expect(reactToFailedRequest).toHaveBeenCalledWith(
      queryClient,
      expect.objectContaining({ httpStatus: 400, errorCode: 'GROUP_FINISHED' }),
    )
  })

  it('refreshes every group after a refusal because the group changed its state', async () => {
    stubFetchByRequest({ [`POST ${expensesPath}`]: problemAnswer(400, 'GROUP_FINISHED') })
    seedOutbox(personX.id, [waitingExpense()])

    const { queryClient } = await renderSignedInOutbox({
      seedQueryCache: (queryClient) => {
        queryClient.setQueryData(['groups', testGroupId], {})
      },
    })

    await waitFor(() => {
      expect(queryClient.getQueryState(['groups', testGroupId])?.isInvalidated).toBe(true)
    })
  })

  it('passes the same error on to the sender, so a 401 pauses the item instead of failing it', async () => {
    vi.mocked(reactToFailedRequest).mockImplementationOnce(() => undefined)
    stubFetchByRequest({ [`POST ${expensesPath}`]: problemAnswer(401, 'AUTH_NOT_SIGNED_IN') })
    seedOutbox(personX.id, [waitingExpense()])

    await renderSignedInOutbox()

    await waitFor(() => {
      expect(reactToFailedRequest).toHaveBeenCalledTimes(1)
    })
    await new Promise((resolve) => setTimeout(resolve, 20))
    expect(storedItems(personX.id)[0]).toMatchObject({ id: 'waiting-expense', state: 'waiting', failure: null })
  })

  it('lets the rest of the app react to a network failure too, which it ignores', async () => {
    stubFetchByRequest({ [`POST ${expensesPath}`]: () => new TypeError('Failed to fetch') })
    seedOutbox(personX.id, [waitingExpense()])

    const { senders } = await renderSignedInOutbox()

    await waitFor(() => {
      expect(reactToFailedRequest).toHaveBeenCalledWith(
        expect.anything(),
        expect.objectContaining({ httpStatus: null }),
      )
    })
    senders[0].pause()
    expect(storedItems(personX.id)[0].state).toBe('waiting')
  })

  it('does not call reactToFailedRequest for an error that is not an API error, and still fails the item', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    stubFetchByRequest({})
    seedOutbox(personX.id, [waitingExpense('broken', { payload: null })])

    await renderSignedInOutbox()

    await waitFor(() => {
      expect(storedItems(personX.id)[0]).toMatchObject({ state: 'failed', failure: { errorCode: null, httpStatus: null } })
    })
    expect(reactToFailedRequest).not.toHaveBeenCalled()
  })
})
