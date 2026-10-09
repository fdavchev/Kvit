import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest'
import { activityPath } from '@/test/activityTestData'
import {
  activityTab,
  expensesTab,
  renderGroupApp,
} from '@/test/activityTestHelpers'
import { hotelExpenseId } from '@/test/expenseTestData'
import { categoriesPath } from '@/test/expenseTestHelpers'
import { typeAmount } from '@/test/expenseFormTestHelpers'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { emptyGroupList, testGroup } from '@/test/groupTestData'
import { jsonAnswer, requestCount } from '@/test/requestTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { routes } from './routes'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const oneBillPath = '/api/groups/one-bill'
const groupId = testGroup.id

function urlsAsked(fetchMock: Mock<typeof fetch>): string[] {
  return fetchMock.mock.calls.map(([url]) => String(url))
}

describe('the New group, One bill and Activity routes followed through the real route table', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    stubDeviceColorScheme('light')
    stubGoogleSignIn()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  describe('the order of the routes', () => {
    it('shows the choice at /groups/new, with the rows One bill and Group, and never asks for a group called new', async () => {
      const { fetchMock, router } = await renderGroupApp(routes.newGroup)

      expect(await screen.findByRole('heading', { level: 1, name: translated('en', 'newGroup.title') })).toBeTruthy()
      expect(screen.getByRole('link', { name: /^One bill/ }).getAttribute('href')).toBe(routes.newBill)
      expect(screen.getByRole('link', { name: /^Group/ }).getAttribute('href')).toBe(routes.newGroupGroup)
      expect(router.state.location.pathname).toBe(routes.newGroup)
      expect(urlsAsked(fetchMock)).not.toContain('/api/groups/new')
    })

    it('shows the group form at /groups/new/group and never asks for a group called new', async () => {
      const { fetchMock } = await renderGroupApp(routes.newGroupGroup)

      expect(await screen.findByLabelText(translated('en', 'groupFields.name'))).toBeTruthy()
      expect(screen.getByRole('heading', { level: 1, name: translated('en', 'newGroup.title') })).toBeTruthy()
      expect(urlsAsked(fetchMock).filter((url) => url.startsWith('/api/groups'))).toEqual([])
    })

    it('does not take "new" for a group id at /groups/new/bill: it shows the One bill form and asks for no group, members or expenses', async () => {
      const { fetchMock } = await renderGroupApp(routes.newBill)

      expect(
        await screen.findByRole('heading', { level: 1, name: translated('en', 'newGroup.oneBill') }),
      ).toBeTruthy()
      expect(await screen.findByLabelText(translated('en', 'expense.amount'))).toBeTruthy()
      expect(urlsAsked(fetchMock).sort()).toEqual(['/api/me', categoriesPath].sort())
    })

    it('shows the group screen on its Activity tab at /groups/{id}/activity and asks for the activity of that group', async () => {
      const { fetchMock, router } = await renderGroupApp(routes.groupActivity(groupId))

      expect(await screen.findByRole('heading', { level: 1, name: testGroup.name })).toBeTruthy()
      expect(await screen.findByText(translated('en', 'activity.empty'))).toBeTruthy()
      expect(activityTab().getAttribute('aria-current')).toBe('page')
      expect(router.state.location.pathname).toBe(routes.groupActivity(groupId))
      expect(urlsAsked(fetchMock)).toContain(activityPath)
    })

    it('keeps /groups/{id} on the Expenses tab', async () => {
      await renderGroupApp(routes.group(groupId))

      await screen.findByRole('link', { name: /Dinner/ })

      expect(expensesTab().getAttribute('aria-current')).toBe('page')
    })
  })

  describe('moving around', () => {
    it('takes a person from the choice to the One bill form and back to the choice', async () => {
      const { router } = await renderGroupApp(routes.newGroup)
      fireEvent.click(await screen.findByRole('link', { name: /^One bill/ }))
      await screen.findByRole('heading', { level: 1, name: translated('en', 'newGroup.oneBill') })

      fireEvent.click(screen.getByRole('link', { name: translated('en', 'common.back') }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.newGroup)
      })
    })

    it('takes a person from the choice to the group form', async () => {
      const { router } = await renderGroupApp(routes.newGroup)

      fireEvent.click(await screen.findByRole('link', { name: /^Group/ }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.newGroupGroup)
      })
      expect(await screen.findByLabelText(translated('en', 'groupFields.name'))).toBeTruthy()
    })

    it('takes a person from the One bill form to the new group after Save bill, and the group asks for its own data', async () => {
      const { fetchMock, router } = await renderGroupApp(routes.newBill, {
        answers: { [`POST ${oneBillPath}`]: jsonAnswer({ groupId, expenseId: hotelExpenseId }) },
      })
      await screen.findByRole('heading', { level: 1, name: translated('en', 'newGroup.oneBill') })
      await screen.findByLabelText(translated('en', 'expense.amount'))
      typeAmount('1800')

      fireEvent.click(screen.getByRole('button', { name: translated('en', 'oneBill.save') }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(groupId))
      })
      expect(await screen.findByRole('heading', { level: 1, name: testGroup.name })).toBeTruthy()
      expect(requestCount(fetchMock, 'POST', oneBillPath)).toBe(1)
      expect(urlsAsked(fetchMock)).toContain(`/api/groups/${groupId}`)
    })

    it('takes a person from the Expenses tab to the Activity tab and back through the pill switch, keeping one group screen', async () => {
      const { router } = await renderGroupApp(routes.group(groupId))
      await screen.findByRole('heading', { level: 1, name: testGroup.name })

      fireEvent.click(activityTab())
      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groupActivity(groupId))
      })
      await screen.findByText(translated('en', 'activity.empty'))
      fireEvent.click(expensesTab())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(groupId))
      })
      expect(screen.getAllByRole('heading', { level: 1 })).toHaveLength(1)
    })

    it('goes back to the groups list from the group screen on the Activity tab', async () => {
      const { router } = await renderGroupApp(routes.groupActivity(groupId), {
        answers: { 'GET /api/groups': jsonAnswer(emptyGroupList) },
      })
      await screen.findByRole('heading', { level: 1, name: testGroup.name })

      fireEvent.click(screen.getByRole('link', { name: translated('en', 'common.back') }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groups)
      })
    })
  })
})
