import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useApiHealth } from '@/features/auth/welcome/hooks/useApiHealth'
import {
  dinnerRow,
  expenseRows,
  hotelDetail,
  hotelExpenseId,
  hotelRow,
  deletedRows,
  testCategories,
} from '@/test/expenseTestData'
import {
  categoriesPath,
  deletedExpensesPath,
  expensePathOf,
  expensesPath,
  groupPath,
  membersPath,
} from '@/test/expenseTestHelpers'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { testGroup } from '@/test/groupTestData'
import { ownerViewMembers } from '@/test/memberTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import {
  jsonAnswer,
  noContentAnswer,
  problemAnswer,
  requestCount,
  stubFetchByRequest,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { testMe } from '@/test/testMe'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { routeObjects } from './router'
import { routes } from './routes'

vi.mock('@/features/auth/welcome/hooks/useApiHealth', () => ({
  useApiHealth: vi.fn(),
}))

const groupId = testGroup.id
const mustChangePasswordMe = { ...testMe, mustChangePassword: true }

const expenseRoutePaths = [
  routes.groupExpenseNew(groupId),
  routes.groupExpensesDeleted(groupId),
  routes.groupExpense(groupId, hotelExpenseId),
  routes.groupExpenseEdit(groupId, hotelExpenseId),
]

const screenHeadings: [string, string, string][] = [
  [routes.groupExpenseNew(groupId), 'the Add expense screen', translated('en', 'expense.addTitle')],
  [routes.groupExpensesDeleted(groupId), 'the Recently deleted expenses screen', translated('en', 'recentlyDeleted.title')],
  [routes.groupExpense(groupId, hotelExpenseId), 'the expense detail screen', 'Hotel'],
  [routes.groupExpenseEdit(groupId, hotelExpenseId), 'the Edit expense screen', translated('en', 'expense.editTitle')],
]

function stubSignedInPerson(answers: Record<string, AnswerFactory> = {}) {
  return stubFetchByRequest({
    'GET /api/me': jsonAnswer(testMe),
    [`GET ${groupPath}`]: jsonAnswer(testGroup),
    [`GET ${membersPath}`]: jsonAnswer(ownerViewMembers),
    [`GET ${categoriesPath}`]: jsonAnswer({ categories: testCategories }),
    [`GET ${expensesPath}`]: jsonAnswer({ expenses: expenseRows }),
    [`GET ${deletedExpensesPath}`]: jsonAnswer({ expenses: deletedRows }),
    [`GET ${expensePathOf(hotelExpenseId)}`]: jsonAnswer(hotelDetail),
    ...answers,
  })
}

describe('expense routes followed through the real route table', () => {
  beforeEach(() => {
    vi.mocked(useApiHealth).mockReset()
    stubDeviceColorScheme('light')
    stubGoogleSignIn()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(expenseRoutePaths)('sends a signed-out visitor who opens %s to the welcome screen', async (path) => {
    stubFetchByRequest({ 'GET /api/me': problemAnswer(401, 'AUTH_NOT_SIGNED_IN') })

    const { router } = await renderRoutesWithProviders(routeObjects, path)

    expect(
      await screen.findByRole('link', { name: translated('en', 'welcome.haveAccount') }),
    ).toBeTruthy()
    expect(router.state.location.pathname).toBe(routes.welcome)
  })

  it.each(expenseRoutePaths)('sends a person who must change the password from %s to the change-password screen', async (path) => {
    stubFetchByRequest({ 'GET /api/me': jsonAnswer(mustChangePasswordMe) })

    const { router } = await renderRoutesWithProviders(routeObjects, path)

    expect(
      await screen.findByRole('heading', { name: translated('en', 'auth.changePassword.title') }),
    ).toBeTruthy()
    expect(router.state.location.pathname).toBe(routes.changePassword)
  })

  it.each(screenHeadings)('shows the right screen at %s: %s', async (path, _name, heading) => {
    stubSignedInPerson()

    await renderRoutesWithProviders(routeObjects, path)

    expect(await screen.findByRole('heading', { level: 1, name: heading })).toBeTruthy()
  })

  it.each(expenseRoutePaths)('shows no bottom bar at %s', async (path) => {
    stubSignedInPerson()

    await renderRoutesWithProviders(routeObjects, path)

    await screen.findByRole('heading', { level: 1 })
    expect(screen.queryByRole('navigation')).toBeNull()
  })

  describe('the order of the routes', () => {
    it('does not read "deleted" as an expense id: it shows the Recently deleted screen, not the detail of an expense', async () => {
      stubSignedInPerson()

      await renderRoutesWithProviders(routeObjects, routes.groupExpensesDeleted(groupId))

      expect(
        await screen.findByRole('heading', { level: 1, name: translated('en', 'recentlyDeleted.title') }),
      ).toBeTruthy()
      expect(screen.queryByRole('link', { name: translated('en', 'expense.edit') })).toBeNull()
      expect(screen.queryByRole('button', { name: translated('en', 'expense.delete') })).toBeNull()
    })

    it('does not read "new" as an expense id: it shows the Add screen and never asks for an expense called new', async () => {
      const fetchMock = stubSignedInPerson()

      await renderRoutesWithProviders(routeObjects, routes.groupExpenseNew(groupId))

      await screen.findByRole('heading', { level: 1, name: translated('en', 'expense.addTitle') })
      expect(fetchMock.mock.calls.map(([url]) => String(url))).not.toContain(`${expensesPath}/new`)
    })

    it('asks for the expense with its own id on the Edit screen, never for an expense called edit', async () => {
      const fetchMock = stubSignedInPerson()

      await renderRoutesWithProviders(routeObjects, routes.groupExpenseEdit(groupId, hotelExpenseId))

      await screen.findByRole('heading', { level: 1, name: translated('en', 'expense.editTitle') })
      const urls = fetchMock.mock.calls.map(([url]) => String(url))
      expect(urls).toContain(expensePathOf(hotelExpenseId))
      expect(urls).not.toContain(`${expensesPath}/edit`)
    })

    it('shows the not-found state with a Go to Groups link for an expense id that is not a guid', async () => {
      stubSignedInPerson({
        [`GET ${expensesPath}/not-a-guid`]: () => new Response(null, { status: 404 }),
      })

      await renderRoutesWithProviders(routeObjects, `${routes.group(groupId)}/expenses/not-a-guid`)

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated('en', 'errors.EXPENSE_NOT_FOUND'),
      )
      expect(screen.getByRole('link', { name: translated('en', 'groups.goToGroups') })).toBeTruthy()
      expect(screen.queryByRole('button', { name: translated('en', 'common.retry') })).toBeNull()
    })
  })

  describe('moving around', () => {
    it('takes a person from the group to the detail of an expense, then to Edit, and back to the detail', async () => {
      stubSignedInPerson()
      const { router } = await renderRoutesWithProviders(routeObjects, routes.group(groupId))

      fireEvent.click(await screen.findByRole('link', { name: /Hotel/ }))
      await screen.findByRole('heading', { level: 1, name: 'Hotel' })
      fireEvent.click(screen.getByRole('link', { name: translated('en', 'expense.edit') }))
      await screen.findByRole('heading', { level: 1, name: translated('en', 'expense.editTitle') })
      fireEvent.click(screen.getByRole('link', { name: translated('en', 'common.back') }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groupExpense(groupId, hotelExpenseId))
      })
    })

    it('takes a person from the group to the Add screen and back to the group', async () => {
      stubSignedInPerson()
      const { router } = await renderRoutesWithProviders(routeObjects, routes.group(groupId))

      fireEvent.click(await screen.findByRole('link', { name: translated('en', 'expense.addTitle') }))
      await screen.findByRole('heading', { level: 1, name: translated('en', 'expense.addTitle') })
      fireEvent.click(screen.getByRole('link', { name: translated('en', 'common.back') }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(groupId))
      })
    })

    it('takes a person from the group to the Recently deleted expenses and back to the group', async () => {
      stubSignedInPerson()
      const { router } = await renderRoutesWithProviders(routeObjects, routes.group(groupId))

      fireEvent.click(await screen.findByRole('link', { name: /^Recently deleted/ }))
      await screen.findByRole('heading', { level: 1, name: translated('en', 'recentlyDeleted.title') })
      fireEvent.click(screen.getByRole('link', { name: translated('en', 'common.back') }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(groupId))
      })
    })

    it('shows the new expense in the list of the group after it is saved on the Add screen', async () => {
      let isSaved = false
      stubSignedInPerson({
        [`GET ${expensesPath}`]: () => Response.json({ expenses: isSaved ? [dinnerRow] : [] }),
        [`POST ${expensesPath}`]: () => {
          isSaved = true
          return Response.json(hotelDetail)
        },
      })
      const { router } = await renderRoutesWithProviders(routeObjects, routes.group(groupId))
      await screen.findByText(translated('en', 'expenses.emptyTitle'))

      fireEvent.click(screen.getByRole('link', { name: translated('en', 'expense.addTitle') }))
      const amount = await screen.findByLabelText(translated('en', 'expense.amount'))
      fireEvent.change(amount, { target: { value: '2400' } })
      fireEvent.click(screen.getByRole('button', { name: translated('en', 'groupSettings.save') }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(groupId))
      })
      expect(await screen.findByRole('link', { name: /Dinner/ })).toBeTruthy()
    })

    it('takes the deleted expense off the list of the group after it is deleted on its detail screen', async () => {
      let isDeleted = false
      stubSignedInPerson({
        [`GET ${expensesPath}`]: () =>
          Response.json({ expenses: isDeleted ? [dinnerRow] : [dinnerRow, hotelRow] }),
        [`DELETE ${expensePathOf(hotelExpenseId)}`]: () => {
          isDeleted = true
          return noContentAnswer()()
        },
      })
      const { router } = await renderRoutesWithProviders(routeObjects, routes.group(groupId))

      fireEvent.click(await screen.findByRole('link', { name: /Hotel/ }))
      fireEvent.click(await screen.findByRole('button', { name: translated('en', 'expense.delete') }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(groupId))
      })
      await screen.findByRole('link', { name: /Dinner/ })
      await waitFor(() => {
        expect(screen.queryByRole('link', { name: /Hotel/ })).toBeNull()
      })
    })

    it('asks for the categories only once while a person moves between the expense screens', async () => {
      const fetchMock = stubSignedInPerson()
      await renderRoutesWithProviders(routeObjects, routes.group(groupId))

      fireEvent.click(await screen.findByRole('link', { name: translated('en', 'expense.addTitle') }))
      await screen.findByRole('heading', { level: 1, name: translated('en', 'expense.addTitle') })
      fireEvent.click(screen.getByRole('link', { name: translated('en', 'common.back') }))
      await screen.findByRole('link', { name: /Hotel/ })

      expect(requestCount(fetchMock, 'GET', categoriesPath)).toBe(1)
    })
  })
})
