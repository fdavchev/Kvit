import { screen, waitFor } from '@testing-library/react'
import type { RouteObject } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { useOutboxItems, useOutboxSender } from '@/core/outbox/useOutbox'
import { hotelDetail } from '@/test/expenseTestData'
import { expensesPath } from '@/test/expenseTestHelpers'
import { testGroupId } from '@/test/groupTestData'
import { seedOutbox, storedIds } from '@/test/outboxTestHelpers'
import { testOutboxItem } from '@/test/outboxTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import {
  jsonAnswer,
  neverAnswer,
  noContentAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
  stubFetchByRequest,
} from '@/test/requestTestHelpers'
import { testMe } from '@/test/testMe'
import { RequireAuth } from './RequireAuth'
import { routes } from './routes'

const personX = { ...testMe, id: 'person-x' }

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

const waitingExpense = testOutboxItem('waiting-expense', {
  kind: 'expense',
  groupId: testGroupId,
  payload: expensePayload,
})

function OutboxPage() {
  const sender = useOutboxSender()
  const items = useOutboxItems()
  return (
    <p>
      {`home page with ${items.length} saved, sender ready: ${String(typeof sender.enqueue === 'function')}`}
    </p>
  )
}

const routeTable: RouteObject[] = [
  { path: routes.welcome, element: <p>welcome page</p> },
  {
    element: <RequireAuth />,
    children: [
      { path: routes.dashboard, element: <OutboxPage /> },
      { path: routes.changePassword, element: <p>change password page</p> },
    ],
  },
]

function answersFor(me: unknown, expenseAnswer = jsonAnswer(hotelDetail)) {
  return {
    'GET /api/me': jsonAnswer(me),
    'POST /api/me/active': noContentAnswer(),
    [`POST ${expensesPath}`]: expenseAnswer,
  }
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('RequireAuth with the outbox', () => {
  it('gives a signed-in screen an outbox that works', async () => {
    stubFetchByRequest(answersFor(personX))

    await renderRoutesWithProviders(routeTable, routes.dashboard)

    expect(await screen.findByText('home page with 0 saved, sender ready: true')).toBeTruthy()
  })

  it('loads the saved items of the signed-in person for the screens', async () => {
    stubFetchByRequest(answersFor(personX, neverAnswer()))
    seedOutbox(personX.id, [waitingExpense])

    await renderRoutesWithProviders(routeTable, routes.dashboard)

    expect(await screen.findByText('home page with 1 saved, sender ready: true')).toBeTruthy()
  })

  it('starts sending the saved items once when the signed-in screens open', async () => {
    const fetchMock = stubFetchByRequest(answersFor(personX, neverAnswer()))
    seedOutbox(personX.id, [waitingExpense])

    await renderRoutesWithProviders(routeTable, routes.dashboard)

    await waitFor(() => {
      expect(requestCount(fetchMock, 'POST', expensesPath)).toBe(1)
    })
    expect(requestsOf(fetchMock, 'POST', expensesPath)[0].body).toEqual(expensePayload)
    await new Promise((resolve) => setTimeout(resolve, 20))
    expect(requestCount(fetchMock, 'POST', expensesPath)).toBe(1)
  })

  it('takes a sent item out of the saved items of the person', async () => {
    stubFetchByRequest(answersFor(personX))
    seedOutbox(personX.id, [waitingExpense])

    await renderRoutesWithProviders(routeTable, routes.dashboard)

    await waitFor(() => {
      expect(storedIds(personX.id)).toEqual([])
    })
    expect(await screen.findByText('home page with 0 saved, sender ready: true')).toBeTruthy()
  })

  it('does not send the saved items of another person', async () => {
    const fetchMock = stubFetchByRequest(answersFor(personX))
    seedOutbox('person-other', [waitingExpense])

    await renderRoutesWithProviders(routeTable, routes.dashboard)
    await screen.findByText('home page with 0 saved, sender ready: true')
    await new Promise((resolve) => setTimeout(resolve, 20))

    expect(requestCount(fetchMock, 'POST', expensesPath)).toBe(0)
    expect(storedIds('person-other')).toEqual(['waiting-expense'])
  })

  it('sends nothing when nobody is signed in', async () => {
    const fetchMock = stubFetchByRequest({
      'GET /api/me': problemAnswer(401, 'AUTH_NOT_SIGNED_IN'),
      [`POST ${expensesPath}`]: jsonAnswer(hotelDetail),
    })
    seedOutbox(testMe.id, [waitingExpense])

    await renderRoutesWithProviders(routeTable, routes.dashboard)

    expect(await screen.findByText('welcome page')).toBeTruthy()
    await new Promise((resolve) => setTimeout(resolve, 20))
    expect(requestCount(fetchMock, 'POST', expensesPath)).toBe(0)
    expect(storedIds(testMe.id)).toEqual(['waiting-expense'])
  })

  it('does not start an outbox for a person who must change the password, and shows the page as before', async () => {
    const fetchMock = stubFetchByRequest(answersFor({ ...personX, mustChangePassword: true }))
    seedOutbox(personX.id, [waitingExpense])

    await renderRoutesWithProviders(routeTable, routes.dashboard)

    expect(await screen.findByText('change password page')).toBeTruthy()
    await new Promise((resolve) => setTimeout(resolve, 20))
    expect(requestCount(fetchMock, 'POST', expensesPath)).toBe(0)
    expect(storedIds(personX.id)).toEqual(['waiting-expense'])
  })

  it('keeps the saved items when the person is signed out later', async () => {
    stubFetchByRequest(answersFor(personX, neverAnswer()))
    seedOutbox(personX.id, [waitingExpense])
    const { queryClient } = await renderRoutesWithProviders(routeTable, routes.dashboard)
    await screen.findByText('home page with 1 saved, sender ready: true')

    queryClient.setQueryData(['me'], null)

    expect(await screen.findByText('welcome page')).toBeTruthy()
    expect(storedIds(personX.id)).toEqual(['waiting-expense'])
  })
})
