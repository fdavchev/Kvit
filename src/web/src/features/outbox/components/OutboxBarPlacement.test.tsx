import { screen, waitFor } from '@testing-library/react'
import type { RouteObject } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { RequireAuth } from '@/core/router/RequireAuth'
import { routes } from '@/core/router/routes'
import { hotelDetail } from '@/test/expenseTestData'
import { expensesPath, isBefore } from '@/test/expenseTestHelpers'
import { queuedExpense } from '@/test/outboxScreenTestData'
import { outboxText } from '@/test/outboxTextsTestData'
import { seedOutbox } from '@/test/outboxTestHelpers'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import {
  jsonAnswer,
  neverAnswer,
  noContentAnswer,
  problemAnswer,
  stubFetchByRequest,
} from '@/test/requestTestHelpers'
import { testMe } from '@/test/testMe'

const routeTable: RouteObject[] = [
  { path: routes.welcome, element: <p>welcome page</p> },
  { path: routes.logIn, element: <p>log in page</p> },
  {
    element: <RequireAuth />,
    children: [
      { path: routes.dashboard, element: <p>home page</p> },
      { path: routes.settings, element: <p>settings page</p> },
      { path: routes.group(':groupId'), element: <p>group page</p> },
      { path: routes.changePassword, element: <p>change password page</p> },
    ],
  },
]

const failedItem = queuedExpense('refused', { state: 'failed' })

function signedInAnswers(me: unknown = testMe, expenseAnswer = neverAnswer()) {
  return {
    'GET /api/me': jsonAnswer(me),
    'POST /api/me/active': noContentAnswer(),
    [`POST ${expensesPath}`]: expenseAnswer,
  }
}

function bar(): HTMLElement | null {
  return document.querySelector<HTMLElement>('[data-outbox-bar]')
}

function barText(): string {
  return bar()?.textContent ?? ''
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('the outbox bar on the signed-in screens', () => {
  it.each([
    ['the home screen', routes.dashboard, 'home page'],
    ['the settings screen', routes.settings, 'settings page'],
    ['a group screen, which has no bottom tab bar', routes.group('7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e'), 'group page'],
  ])('is shown on %s when a change failed', async (_name, path, pageText) => {
    stubFetchByRequest(signedInAnswers())
    seedOutbox(testMe.id, [failedItem])

    await renderRoutesWithProviders(routeTable, path)

    expect(await screen.findByText(pageText)).toBeTruthy()
    await waitFor(() => {
      expect(bar()).not.toBeNull()
    })
    expect(barText()).toContain(outboxText('en', 'outbox.failed', 1))
  })

  it('shows the number of changes waiting while they are sent', async () => {
    stubFetchByRequest(signedInAnswers())
    seedOutbox(testMe.id, [queuedExpense('a'), queuedExpense('b')])

    await renderRoutesWithProviders(routeTable, routes.dashboard)

    await waitFor(() => {
      expect(barText()).toContain('2 changes waiting')
    })
  })

  it('says everything is up to date after the saved changes were sent', async () => {
    stubFetchByRequest(signedInAnswers(testMe, jsonAnswer(hotelDetail)))
    seedOutbox(testMe.id, [queuedExpense('a')])

    await renderRoutesWithProviders(routeTable, routes.dashboard)

    expect(await screen.findByText(outboxText('en', 'outbox.upToDate'))).toBeTruthy()
  })

  it('is placed after the screen, so it is drawn over it', async () => {
    stubFetchByRequest(signedInAnswers())
    seedOutbox(testMe.id, [failedItem])

    await renderRoutesWithProviders(routeTable, routes.dashboard)

    const page = await screen.findByText('home page')
    await waitFor(() => {
      expect(bar()).not.toBeNull()
    })
    expect(isBefore(page, bar() as HTMLElement)).toBe(true)
  })

  it('is not there when there is nothing to tell', async () => {
    stubFetchByRequest(signedInAnswers())

    await renderRoutesWithProviders(routeTable, routes.dashboard)
    await screen.findByText('home page')
    await new Promise((resolve) => setTimeout(resolve, 30))

    expect(bar()).toBeNull()
  })

  it('shows only the changes of the signed-in person', async () => {
    stubFetchByRequest(signedInAnswers())
    seedOutbox('someone-else', [failedItem])

    await renderRoutesWithProviders(routeTable, routes.dashboard)
    await screen.findByText('home page')
    await new Promise((resolve) => setTimeout(resolve, 30))

    expect(bar()).toBeNull()
  })
})

describe('the outbox bar on the screens where nobody is signed in', () => {
  it.each([
    ['the welcome screen', routes.welcome, 'welcome page'],
    ['the log-in screen', routes.logIn, 'log in page'],
  ])('is not on %s, even when changes of a person are saved in this browser', async (_name, path, pageText) => {
    stubFetchByRequest({ 'GET /api/me': problemAnswer(401, 'AUTH_NOT_SIGNED_IN') })
    seedOutbox(testMe.id, [failedItem, queuedExpense('waiting')])

    await renderRoutesWithProviders(routeTable, path)

    expect(await screen.findByText(pageText)).toBeTruthy()
    await new Promise((resolve) => setTimeout(resolve, 30))
    expect(bar()).toBeNull()
  })

  it('is not shown after the person signs out', async () => {
    stubFetchByRequest(signedInAnswers())
    seedOutbox(testMe.id, [failedItem])
    const { queryClient } = await renderRoutesWithProviders(routeTable, routes.dashboard)
    await waitFor(() => {
      expect(bar()).not.toBeNull()
    })

    queryClient.setQueryData(['me'], null)

    expect(await screen.findByText('welcome page')).toBeTruthy()
    expect(bar()).toBeNull()
  })

  it('is not on the change-password screen, which a person must pass before the app opens', async () => {
    stubFetchByRequest(signedInAnswers({ ...testMe, mustChangePassword: true }))
    seedOutbox(testMe.id, [failedItem])

    await renderRoutesWithProviders(routeTable, routes.dashboard)

    expect(await screen.findByText('change password page')).toBeTruthy()
    await new Promise((resolve) => setTimeout(resolve, 30))
    expect(bar()).toBeNull()
  })
})
