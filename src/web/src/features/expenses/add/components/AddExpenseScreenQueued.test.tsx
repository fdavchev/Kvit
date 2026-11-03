import { fireEvent, screen, waitFor } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { routes } from '@/core/router/routes'
import { hotelDetail, testCategories, testNow, testToday } from '@/test/expenseTestData'
import { isDisabled, saveButton, sentBody, typeAmount, typeTitle } from '@/test/expenseFormTestHelpers'
import {
  categoriesPath,
  expensesPath,
  freezeTime,
  groupPath,
  membersPath,
  unfreezeTime,
} from '@/test/expenseTestHelpers'
import { groupOf, testGroupId } from '@/test/groupTestData'
import { ownerViewMembers } from '@/test/memberTestData'
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
import { AddExpenseScreen } from './AddExpenseScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

async function renderReadyAdd(postAnswer: AnswerFactory = jsonAnswer(hotelDetail)) {
  const fetchMock = stubFetchByRequest({
    [`GET ${groupPath}`]: jsonAnswer(groupOf({ memberCount: 5 })),
    [`GET ${membersPath}`]: jsonAnswer(ownerViewMembers),
    [`GET ${categoriesPath}`]: jsonAnswer({ categories: testCategories }),
    [`POST ${expensesPath}`]: postAnswer,
  })
  const rendered = await renderRoutesWithProviders(
    [
      { path: '/groups/:groupId/expenses/new', element: <AddExpenseScreen /> },
      { path: '/groups/:groupId', element: <p>group page</p> },
      { path: routes.groups, element: <p>groups page</p> },
    ],
    routes.groupExpenseNew(testGroupId),
    { seedCache: seedMe(testMe) },
  )
  await screen.findByRole('button', { name: translated('en', 'groupSettings.save') })
  const run = holdOutboxSender(rendered.outboxSender)
  return { fetchMock, run, ...rendered }
}

function savedToastShown(): boolean {
  return shownToastTexts().includes(translated('en', 'expenses.saved'))
}

describe('AddExpenseScreen saving through the outbox', () => {
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
    it('shows the Expense saved toast, goes to the group and queues nothing', async () => {
      const { router, run } = await renderReadyAdd()
      typeAmount('1200')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })
      expect(savedToastShown()).toBe(true)
      expect(readSavedOutbox()).toEqual([])
      expect(run).not.toHaveBeenCalled()
    })

    it('goes to the group by replacing the history entry', async () => {
      const { router } = await renderReadyAdd()
      typeAmount('1200')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })
      expect(router.state.historyAction).toBe('REPLACE')
    })
  })

  describe('when the server cannot be reached', () => {
    it('goes to the group as if saved, by replacing the history entry', async () => {
      const { router } = await renderReadyAdd(networkFailureAnswer())
      typeAmount('1200')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })
      expect(router.state.historyAction).toBe('REPLACE')
      expect(await screen.findByText('group page')).toBeTruthy()
    })

    it('stores the expense in the outbox with the request id and the payload of the request that failed', async () => {
      const { fetchMock, router } = await renderReadyAdd(networkFailureAnswer())
      typeAmount('1200')
      typeTitle('Hotel')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })
      const body = sentBody(fetchMock, 'POST', expensesPath)
      expect(readSavedOutbox()).toHaveLength(1)
      expect(readSavedOutbox()[0]).toMatchObject({
        id: body.clientRequestId,
        kind: 'expense',
        groupId: testGroupId,
        state: 'waiting',
        failure: null,
      })
      expect(readSavedOutbox()[0].payload).toEqual(body)
    })

    it('stores what the waiting row will show: title, amount, currency, date, category and who paid', async () => {
      const { router } = await renderReadyAdd(networkFailureAnswer())
      typeAmount('1200')
      typeTitle('Hotel')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })
      expect(readSavedOutbox()[0].display).toEqual({
        title: 'Hotel',
        amountMinor: 120000,
        currency: 'MKD',
        date: testToday,
        categoryId: null,
        paidByName: 'Filip',
      })
    })

    it('shows the queued expense in the outbox of the screen, not only in the browser', async () => {
      const { router, outboxSender } = await renderReadyAdd(networkFailureAnswer())
      typeAmount('1200')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })
      expect(outboxSender.getSnapshot().items.map((item) => item.id)).toEqual(readSavedOutbox().map((item) => item.id))
      expect(outboxSender.getSnapshot().items).toHaveLength(1)
    })

    it('does not show the Expense saved toast or any error toast', async () => {
      const { router } = await renderReadyAdd(networkFailureAnswer())
      typeAmount('1200')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })
      expect(savedToastShown()).toBe(false)
      expect(vi.mocked(toast.error)).not.toHaveBeenCalled()
      expect(screen.queryByRole('alert')).toBeNull()
    })

    it('starts the sender once after the expense is queued', async () => {
      const { router, run } = await renderReadyAdd(networkFailureAnswer())
      typeAmount('1200')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })
      expect(run).toHaveBeenCalledTimes(1)
    })

    it.each([
      ['502', 502],
      ['503', 503],
      ['504', 504],
    ])('queues the expense on a %s from the server too', async (_name, status) => {
      const { router } = await renderReadyAdd(() => new Response(null, { status }))
      typeAmount('1200')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })
      expect(readSavedOutbox()).toHaveLength(1)
      expect(savedToastShown()).toBe(false)
    })

    it('sends only the one request of the screen itself, because the sender sends the rest later', async () => {
      const { fetchMock, router } = await renderReadyAdd(networkFailureAnswer())
      typeAmount('1200')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })
      expect(requestCount(fetchMock, 'POST', expensesPath)).toBe(1)
    })

    it('shows the generic error and stays on the screen when the expense could not be stored in the browser', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { router, outboxSender } = await renderReadyAdd(networkFailureAnswer())
      vi.spyOn(outboxSender, 'enqueue').mockRejectedValue(new Error('IndexedDB write failed'))
      typeAmount('1200')

      fireEvent.click(saveButton())

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.generic'))
      expect(router.state.location.pathname).toBe(routes.groupExpenseNew(testGroupId))
      expect(savedToastShown()).toBe(false)
    })
  })

  describe('when the server gives no answer for 3 seconds', () => {
    it('shows the pending state during the 3 seconds and queues nothing yet', async () => {
      const { router } = await renderReadyAdd(neverAnswer())
      typeAmount('1200')
      installFakeTimers(testNow)

      fireEvent.click(saveButton())
      await letTimeGoBy(2999)

      expect(isDisabled(saveButton())).toBe(true)
      expect(router.state.location.pathname).toBe(routes.groupExpenseNew(testGroupId))
      expect(readSavedOutbox()).toEqual([])
    })

    it('queues the expense, goes to the group and ends the pending state when the 3 seconds are over', async () => {
      const { fetchMock, router, run } = await renderReadyAdd(neverAnswer())
      typeAmount('1200')
      installFakeTimers(testNow)

      fireEvent.click(saveButton())
      await letQueueTimeGoBy()

      expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      expect(router.state.historyAction).toBe('REPLACE')
      expect(readSavedOutbox()).toHaveLength(1)
      expect(readSavedOutbox()[0].payload).toEqual(sentBody(fetchMock, 'POST', expensesPath))
      expect(savedToastShown()).toBe(false)
      expect(run).toHaveBeenCalledTimes(1)
    })
  })

  describe('when the original request answers after the expense was queued', () => {
    it('takes the expense out of the outbox and sends nothing a second time', async () => {
      const late = createDeferred<Response>()
      const { fetchMock, router } = await renderReadyAdd(() => late.promise)
      typeAmount('1200')
      installFakeTimers(testNow)
      fireEvent.click(saveButton())
      await letQueueTimeGoBy()
      expect(readSavedOutbox()).toHaveLength(1)

      late.resolve(Response.json(hotelDetail))
      await letTimeGoBy(0)

      expect(readSavedOutbox()).toEqual([])
      expect(requestCount(fetchMock, 'POST', expensesPath)).toBe(1)
      expect(router.state.location.pathname).toBe(routes.group(testGroupId))
    })

    it('keeps the expense in the outbox when the original request fails later with a network failure', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const late = createDeferred<Response>()
      const { router } = await renderReadyAdd(() => late.promise)
      typeAmount('1200')
      installFakeTimers(testNow)
      fireEvent.click(saveButton())
      await letQueueTimeGoBy()

      late.reject(new TypeError('Failed to fetch'))
      await letTimeGoBy(0)

      expect(readSavedOutbox()).toHaveLength(1)
      expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      expect(screen.queryByRole('alert')).toBeNull()
    })
  })

  describe('when the server refuses the expense', () => {
    it.each([
      [400, 'EXPENSE_AMOUNT_NOT_POSITIVE'],
      [400, 'EXPENSE_SPLIT_DOES_NOT_ADD_UP'],
      [400, 'GROUP_FINISHED'],
      [404, 'GROUP_NOT_FOUND'],
    ])('shows the translated text for %i %s, stays on the screen and queues nothing', async (status, code) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { router, run } = await renderReadyAdd(problemAnswer(status, code))
      typeAmount('1200')

      fireEvent.click(saveButton())

      const alert = await screen.findByRole('alert')
      expect(alert.textContent).toContain(translated('en', `errors.${code}`))
      expect(router.state.location.pathname).toBe(routes.groupExpenseNew(testGroupId))
      expect(savedToastShown()).toBe(false)
      expect(readSavedOutbox()).toEqual([])
      expect(run).not.toHaveBeenCalled()
    })

    it('keeps Save usable after the refusal', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderReadyAdd(problemAnswer(400, 'EXPENSE_AMOUNT_NOT_POSITIVE'))
      typeAmount('1200')

      fireEvent.click(saveButton())
      await screen.findByRole('alert')

      expect(isDisabled(saveButton())).toBe(false)
    })

    it('asks the group again when the refusal says the group changed its state', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { fetchMock } = await renderReadyAdd(problemAnswer(400, 'GROUP_FINISHED'))
      typeAmount('1200')
      expect(requestCount(fetchMock, 'GET', groupPath)).toBe(1)

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', groupPath)).toBe(2)
      })
    })
  })

  describe('while the request waits for an answer', () => {
    it('disables Save and queues nothing', async () => {
      await renderReadyAdd(neverAnswer())
      typeAmount('1200')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(isDisabled(saveButton())).toBe(true)
      })
      expect(readSavedOutbox()).toEqual([])
    })
  })

  describe('when something goes wrong after the save worked', () => {
    it('does not hide an error thrown while the saved expense is being finished: it reaches console.error', async () => {
      const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
      const thrown = new Error('the toast could not be shown')
      vi.mocked(toast.success).mockImplementationOnce(() => {
        throw thrown
      })
      await renderReadyAdd()
      typeAmount('1200')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(consoleError.mock.calls.some((call) => call.includes(thrown))).toBe(true)
      })
    })

    it('does not show the form error for an error thrown after the save worked', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      vi.mocked(toast.success).mockImplementationOnce(() => {
        throw new Error('the toast could not be shown')
      })
      await renderReadyAdd()
      typeAmount('1200')

      fireEvent.click(saveButton())
      await waitFor(() => {
        expect(vi.mocked(console.error)).toHaveBeenCalled()
      })

      expect(screen.queryByRole('alert')).toBeNull()
    })
  })
})
