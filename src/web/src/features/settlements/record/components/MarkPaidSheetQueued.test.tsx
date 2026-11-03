import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { GroupMember } from '@/core/services/groups/membersService'
import { settlementsPath } from '@/test/balancesTestData'
import { isDisabled, saveButton, sentBody, waitForSheetToClose } from '@/test/expenseFormTestHelpers'
import { personOf, expensePeople } from '@/features/expenses/shared/expensePeople'
import { testGroupId } from '@/test/groupTestData'
import { anaMember, filipMember, grandmaMember, markoMember } from '@/test/memberTestData'
import { createDeferred } from '@/test/outboxTestHelpers'
import {
  holdOutboxSender,
  letQueueTimeGoBy,
  letTimeGoBy,
  readSavedOutbox,
  installFakeTimers,
} from '@/test/queuedSaveTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import {
  jsonAnswer,
  networkFailureAnswer,
  neverAnswer,
  problemAnswer,
  requestCount,
  stubFetchByRequest,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { pendingPaymentOf } from '@/test/settlementActionsTestData'
import { seedMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { MarkPaidSheet } from './MarkPaidSheet'

const members = [filipMember, anaMember, markoMember, grandmaMember]

async function renderMarkPaid(
  from: GroupMember,
  to: GroupMember,
  amountMinor: number,
  postAnswer: AnswerFactory = jsonAnswer(pendingPaymentOf(from, to, amountMinor)),
) {
  const fetchMock = stubFetchByRequest({ [`POST ${settlementsPath}`]: postAnswer })
  const onClose = vi.fn<() => void>()
  const people = expensePeople(members)
  const rendered = await renderElementWithProviders(
    <MarkPaidSheet
      groupId={testGroupId}
      members={members}
      payment={{
        payer: personOf(people, from.id),
        receiver: personOf(people, to.id),
        amountMinor,
        currency: 'MKD',
      }}
      onClose={onClose}
    />,
    '/sheet',
    {
      seedCache: (queryClient) => {
        seedMe(testMe)(queryClient)
        queryClient.setQueryData(['groups', testGroupId], {})
      },
    },
  )
  const sheet = await screen.findByRole('dialog')
  const run = holdOutboxSender(rendered.outboxSender)
  return { ...rendered, sheet, fetchMock, onClose, run }
}

afterEach(() => {
  vi.useRealTimers()
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe('the Mark paid sheet saving through the outbox', () => {
  describe('when the server answers in time', () => {
    it('closes the sheet and queues nothing', async () => {
      const { onClose, run } = await renderMarkPaid(markoMember, filipMember, 80000)

      fireEvent.click(saveButton())

      await waitForSheetToClose()
      expect(onClose).toHaveBeenCalled()
      expect(readSavedOutbox()).toEqual([])
      expect(run).not.toHaveBeenCalled()
    })
  })

  describe('when the payment counts right away and the server cannot be reached', () => {
    it('closes the sheet as if saved, with no error', async () => {
      await renderMarkPaid(markoMember, filipMember, 80000, networkFailureAnswer())

      fireEvent.click(saveButton())

      await waitForSheetToClose()
      expect(screen.queryByRole('alert')).toBeNull()
    })

    it('stores the payment with the request id and the payload of the request that failed', async () => {
      const { fetchMock } = await renderMarkPaid(markoMember, filipMember, 80000, networkFailureAnswer())

      fireEvent.click(saveButton())

      await waitForSheetToClose()
      const body = sentBody(fetchMock, 'POST', settlementsPath)
      expect(readSavedOutbox()).toHaveLength(1)
      expect(readSavedOutbox()[0]).toMatchObject({
        id: body.clientRequestId,
        kind: 'settlement',
        groupId: testGroupId,
        state: 'waiting',
        failure: null,
      })
      expect(readSavedOutbox()[0].payload).toEqual(body)
      expect(body).toMatchObject({ fromMemberId: markoMember.id, toMemberId: filipMember.id, amountMinor: 80000, currency: 'MKD' })
    })

    it('stores the amount that was edited in the sheet, not the one the sheet opened with', async () => {
      const { fetchMock } = await renderMarkPaid(markoMember, filipMember, 80000, networkFailureAnswer())
      fireEvent.change(screen.getByLabelText(translated('en', 'expense.amount')), { target: { value: '500' } })

      fireEvent.click(saveButton())

      await waitForSheetToClose()
      expect(sentBody(fetchMock, 'POST', settlementsPath).amountMinor).toBe(50000)
      expect(readSavedOutbox()[0].display.amountMinor).toBe(50000)
    })

    it('stores what the waiting row will show: the amount, the currency, the payer and the receiver', async () => {
      await renderMarkPaid(markoMember, filipMember, 80000, networkFailureAnswer())

      fireEvent.click(saveButton())

      await waitForSheetToClose()
      expect(readSavedOutbox()[0].display).toEqual({
        amountMinor: 80000,
        currency: 'MKD',
        payerName: 'Marko',
        receiverName: 'Filip',
      })
    })

    it('queues the owner marking a payment between two names without an account too', async () => {
      await renderMarkPaid(markoMember, grandmaMember, 15000, networkFailureAnswer())

      fireEvent.click(saveButton())

      await waitForSheetToClose()
      expect(readSavedOutbox()).toHaveLength(1)
      expect(readSavedOutbox()[0].display).toMatchObject({ payerName: 'Marko', receiverName: 'Grandma' })
    })

    it('starts the sender once after the payment is queued', async () => {
      const { run } = await renderMarkPaid(markoMember, filipMember, 80000, networkFailureAnswer())

      fireEvent.click(saveButton())

      await waitForSheetToClose()
      expect(run).toHaveBeenCalledTimes(1)
    })

    it('shows the generic error and keeps the sheet open when the payment could not be stored in the browser', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { sheet, outboxSender, onClose } = await renderMarkPaid(markoMember, filipMember, 80000, networkFailureAnswer())
      vi.spyOn(outboxSender, 'enqueue').mockRejectedValue(new Error('IndexedDB write failed'))

      fireEvent.click(saveButton())

      expect((await within(sheet).findByRole('alert')).textContent).toContain(translated('en', 'errors.generic'))
      expect(onClose).not.toHaveBeenCalled()
    })
  })

  describe('when the payment waits for the other person and the server cannot be reached', () => {
    it.each([
      ['I paid a person with an account', filipMember, anaMember],
      ['a name pays a person with an account', markoMember, anaMember],
    ])('does not queue it when %s, shows the network message and keeps the sheet open', async (_name, from, to) => {
      const { sheet, run } = await renderMarkPaid(from, to, 45000, networkFailureAnswer())

      fireEvent.click(saveButton())

      expect((await within(sheet).findByRole('alert')).textContent).toContain(translated('en', 'errors.network'))
      expect(screen.getByRole('dialog')).toBe(sheet)
      expect(readSavedOutbox()).toEqual([])
      expect(run).not.toHaveBeenCalled()
    })

    it('does not queue it when the server gives no answer for 3 seconds, and keeps waiting', async () => {
      const { onClose } = await renderMarkPaid(filipMember, anaMember, 45000, neverAnswer())
      installFakeTimers()

      fireEvent.click(saveButton())
      await letTimeGoBy(10000)

      expect(isDisabled(saveButton())).toBe(true)
      expect(readSavedOutbox()).toEqual([])
      expect(onClose).not.toHaveBeenCalled()
    })
  })

  describe('when the server gives no answer for 3 seconds', () => {
    it('keeps the sheet open with Save disabled during the 3 seconds and queues nothing yet', async () => {
      const { onClose } = await renderMarkPaid(markoMember, filipMember, 80000, neverAnswer())
      installFakeTimers()

      fireEvent.click(saveButton())
      await letTimeGoBy(2999)

      expect(isDisabled(saveButton())).toBe(true)
      expect(readSavedOutbox()).toEqual([])
      expect(onClose).not.toHaveBeenCalled()
    })

    it('queues the payment, closes the sheet and starts the sender when the 3 seconds are over', async () => {
      const { fetchMock, run } = await renderMarkPaid(markoMember, filipMember, 80000, neverAnswer())
      installFakeTimers()

      fireEvent.click(saveButton())
      await letQueueTimeGoBy()
      await letTimeGoBy(1000)

      expect(screen.queryByRole('dialog')).toBeNull()
      expect(readSavedOutbox()).toHaveLength(1)
      expect(readSavedOutbox()[0].payload).toEqual(sentBody(fetchMock, 'POST', settlementsPath))
      expect(run).toHaveBeenCalledTimes(1)
    })
  })

  describe('when the original request answers after the payment was queued', () => {
    it('takes the payment out of the outbox and sends nothing a second time', async () => {
      const late = createDeferred<Response>()
      const { fetchMock } = await renderMarkPaid(markoMember, filipMember, 80000, () => late.promise)
      installFakeTimers()
      fireEvent.click(saveButton())
      await letQueueTimeGoBy()
      expect(readSavedOutbox()).toHaveLength(1)

      late.resolve(Response.json(pendingPaymentOf(markoMember, filipMember, 80000)))
      await letTimeGoBy(0)

      expect(readSavedOutbox()).toEqual([])
      expect(requestCount(fetchMock, 'POST', settlementsPath)).toBe(1)
    })
  })

  describe('when the server refuses the payment', () => {
    it.each([
      [400, 'SETTLEMENT_AMOUNT_TOO_LARGE'],
      [403, 'SETTLEMENT_NOT_ALLOWED'],
      [404, 'GROUP_NOT_FOUND'],
    ])('shows the translated text for %i %s inside the sheet, leaves it open and queues nothing', async (status, code) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { sheet, onClose, run } = await renderMarkPaid(markoMember, filipMember, 80000, problemAnswer(status, code))

      fireEvent.click(saveButton())

      const alert = await within(sheet).findByRole('alert')
      expect(alert.textContent).toContain(translated('en', `errors.${code}`))
      expect(onClose).not.toHaveBeenCalled()
      expect(readSavedOutbox()).toEqual([])
      expect(run).not.toHaveBeenCalled()
    })

    it('asks the groups again when the refusal says the group changed its state', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { sheet, queryClient } = await renderMarkPaid(markoMember, filipMember, 80000, problemAnswer(400, 'GROUP_CLOSING'))

      fireEvent.click(saveButton())

      await within(sheet).findByRole('alert')
      await waitFor(() => {
        expect(queryClient.getQueryState(['groups', testGroupId])?.isInvalidated).toBe(true)
      })
    })
  })
})
