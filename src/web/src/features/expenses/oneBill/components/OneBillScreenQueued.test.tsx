import { fireEvent, screen, waitFor } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { routes } from '@/core/router/routes'
import { testCategories, testNow, testToday } from '@/test/expenseTestData'
import { isDisabled, sentBody, typeAmount, typeTitle } from '@/test/expenseFormTestHelpers'
import { categoriesPath, freezeTime, unfreezeTime } from '@/test/expenseTestHelpers'
import { testGroupId } from '@/test/groupTestData'
import { createDeferred } from '@/test/outboxTestHelpers'
import {
  holdOutboxSender,
  letQueueTimeGoBy,
  letTimeGoBy,
  readSavedOutbox,
  installFakeTimers,
} from '@/test/queuedSaveTestHelpers'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import {
  jsonAnswer,
  networkFailureAnswer,
  neverAnswer,
  problemAnswer,
  requestCount,
  stubFetchByRequest,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { shownToastTexts } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'
import { OneBillScreen } from './OneBillScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const oneBillPath = '/api/groups/one-bill'
const createdBill = { groupId: testGroupId, expenseId: 'expense-1' }

function saveBillButton(): HTMLElement {
  return screen.getByRole('button', { name: translated('en', 'oneBill.save') })
}

async function renderReadyBill(postAnswer: AnswerFactory = jsonAnswer(createdBill)) {
  const fetchMock = stubFetchByRequest({
    [`GET ${categoriesPath}`]: jsonAnswer({ categories: testCategories }),
    [`POST ${oneBillPath}`]: postAnswer,
  })
  const rendered = await renderRoutesWithProviders(
    [
      { path: routes.newBill, element: <OneBillScreen /> },
      { path: '/groups/:groupId', element: <p>group page</p> },
      { path: routes.newGroup, element: <p>new group page</p> },
      { path: routes.groups, element: <p>groups page</p> },
    ],
    routes.newBill,
    { seedCache: seedMe(testMe) },
  )
  await screen.findByRole('button', { name: translated('en', 'oneBill.save') })
  const run = holdOutboxSender(rendered.outboxSender)
  return { fetchMock, run, ...rendered }
}

function savedToastShown(): boolean {
  return shownToastTexts().includes(translated('en', 'expenses.saved'))
}

describe('OneBillScreen saving through the outbox', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    freezeTime()
  })

  afterEach(() => {
    unfreezeTime()
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('when the server answers in time', () => {
    it('shows the Expense saved toast, opens the new group and queues nothing', async () => {
      const { router, run } = await renderReadyBill()
      typeAmount('1800')

      fireEvent.click(saveBillButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })
      expect(router.state.historyAction).toBe('REPLACE')
      expect(savedToastShown()).toBe(true)
      expect(readSavedOutbox()).toEqual([])
      expect(run).not.toHaveBeenCalled()
    })
  })

  describe('when the server cannot be reached', () => {
    it('goes to the list of groups as if saved, by replacing the history entry, because no group exists yet', async () => {
      const { router } = await renderReadyBill(networkFailureAnswer())
      typeAmount('1800')

      fireEvent.click(saveBillButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groups)
      })
      expect(router.state.historyAction).toBe('REPLACE')
      expect(screen.getByText('groups page')).toBeTruthy()
    })

    it('stores the One bill with no group, the request id and the payload of the request that failed', async () => {
      const { fetchMock, router } = await renderReadyBill(networkFailureAnswer())
      typeAmount('1800')

      fireEvent.click(saveBillButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groups)
      })
      const body = sentBody(fetchMock, 'POST', oneBillPath)
      expect(readSavedOutbox()).toHaveLength(1)
      expect(readSavedOutbox()[0]).toMatchObject({
        id: body.clientRequestId,
        kind: 'oneBill',
        groupId: null,
        state: 'waiting',
        failure: null,
      })
      expect(readSavedOutbox()[0].payload).toEqual(body)
    })

    it('stores the typed title as the title and as the group name of the waiting row', async () => {
      const { router } = await renderReadyBill(networkFailureAnswer())
      typeAmount('1800')
      typeTitle('Dinner')

      fireEvent.click(saveBillButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groups)
      })
      expect(readSavedOutbox()[0].display).toEqual({
        title: 'Dinner',
        groupName: 'Dinner',
        amountMinor: 180000,
        currency: 'MKD',
        date: testToday,
        categoryId: null,
      })
    })

    it('stores the group name that the phone sent as the title when no title was typed', async () => {
      const { fetchMock, router } = await renderReadyBill(networkFailureAnswer())
      typeAmount('1800')

      fireEvent.click(saveBillButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groups)
      })
      const sentGroupName = sentBody(fetchMock, 'POST', oneBillPath).groupName
      expect(typeof sentGroupName).toBe('string')
      expect(readSavedOutbox()[0].display.title).toBe(sentGroupName)
      expect(readSavedOutbox()[0].display.groupName).toBe(sentGroupName)
    })

    it('does not show the Expense saved toast, any error toast or any error text', async () => {
      const { router } = await renderReadyBill(networkFailureAnswer())
      typeAmount('1800')

      fireEvent.click(saveBillButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groups)
      })
      expect(savedToastShown()).toBe(false)
      expect(vi.mocked(toast.error)).not.toHaveBeenCalled()
      expect(screen.queryByRole('alert')).toBeNull()
    })

    it('starts the sender once after the One bill is queued', async () => {
      const { router, run } = await renderReadyBill(networkFailureAnswer())
      typeAmount('1800')

      fireEvent.click(saveBillButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groups)
      })
      expect(run).toHaveBeenCalledTimes(1)
    })

    it.each([
      ['502', 502],
      ['503', 503],
      ['504', 504],
    ])('queues the One bill on a %s from the server too', async (_name, status) => {
      const { router } = await renderReadyBill(() => new Response(null, { status }))
      typeAmount('1800')

      fireEvent.click(saveBillButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groups)
      })
      expect(readSavedOutbox()).toHaveLength(1)
    })

    it('shows the generic error and stays on the screen when the One bill could not be stored in the browser', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { router, outboxSender } = await renderReadyBill(networkFailureAnswer())
      vi.spyOn(outboxSender, 'enqueue').mockRejectedValue(new Error('IndexedDB write failed'))
      typeAmount('1800')

      fireEvent.click(saveBillButton())

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.generic'))
      expect(router.state.location.pathname).toBe(routes.newBill)
    })
  })

  describe('when the server gives no answer for 3 seconds', () => {
    it('shows the pending state during the 3 seconds and queues nothing yet', async () => {
      const { router } = await renderReadyBill(neverAnswer())
      typeAmount('1800')
      installFakeTimers(testNow)

      fireEvent.click(saveBillButton())
      await letTimeGoBy(2999)

      expect(isDisabled(saveBillButton())).toBe(true)
      expect(router.state.location.pathname).toBe(routes.newBill)
      expect(readSavedOutbox()).toEqual([])
    })

    it('queues the One bill, goes to the list of groups and ends the pending state when the 3 seconds are over', async () => {
      const { fetchMock, router, run } = await renderReadyBill(neverAnswer())
      typeAmount('1800')
      installFakeTimers(testNow)

      fireEvent.click(saveBillButton())
      await letQueueTimeGoBy()

      expect(router.state.location.pathname).toBe(routes.groups)
      expect(router.state.historyAction).toBe('REPLACE')
      expect(readSavedOutbox()).toHaveLength(1)
      expect(readSavedOutbox()[0].payload).toEqual(sentBody(fetchMock, 'POST', oneBillPath))
      expect(savedToastShown()).toBe(false)
      expect(run).toHaveBeenCalledTimes(1)
    })
  })

  describe('when the original request answers after the One bill was queued', () => {
    it('takes the One bill out of the outbox and sends nothing a second time', async () => {
      const late = createDeferred<Response>()
      const { fetchMock } = await renderReadyBill(() => late.promise)
      typeAmount('1800')
      installFakeTimers(testNow)
      fireEvent.click(saveBillButton())
      await letQueueTimeGoBy()
      expect(readSavedOutbox()).toHaveLength(1)

      late.resolve(Response.json(createdBill))
      await letTimeGoBy(0)

      expect(readSavedOutbox()).toEqual([])
      expect(requestCount(fetchMock, 'POST', oneBillPath)).toBe(1)
    })
  })

  describe('when the server refuses the One bill', () => {
    it.each([
      [400, 'EXPENSE_AMOUNT_TOO_LARGE'],
      [400, 'EXPENSE_TITLE_INVALID'],
      [404, 'MEMBER_NOT_FOUND'],
    ])('shows the translated text for %i %s inline, stays on the screen and queues nothing', async (status, code) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { router, run } = await renderReadyBill(problemAnswer(status, code))
      typeAmount('1800')

      fireEvent.click(saveBillButton())

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', `errors.${code}`))
      expect(router.state.location.pathname).toBe(routes.newBill)
      expect(savedToastShown()).toBe(false)
      expect(readSavedOutbox()).toEqual([])
      expect(run).not.toHaveBeenCalled()
    })
  })
})
