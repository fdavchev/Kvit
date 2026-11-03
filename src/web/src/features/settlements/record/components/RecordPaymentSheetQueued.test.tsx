import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { settlementsPath } from '@/test/balancesTestData'
import { isDisabled, saveButton, sentBody, typeAmount, waitForSheetToClose } from '@/test/expenseFormTestHelpers'
import { testGroupId } from '@/test/groupTestData'
import { anaMember, filipMember, markoMember } from '@/test/memberTestData'
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
import { sheetButton } from '@/test/settlementActionsTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { RecordPaymentSheet } from './RecordPaymentSheet'

const recordAnotherPayment = 'Record another payment'
const iPaid = 'I paid'
const someonePaidMe = 'Someone paid me'
const members = [filipMember, anaMember, markoMember]

async function renderOpenSheet(postAnswer: AnswerFactory = jsonAnswer(pendingPaymentOf(anaMember, filipMember, 20000))) {
  const fetchMock = stubFetchByRequest({ [`POST ${settlementsPath}`]: postAnswer })
  const onClose = vi.fn<() => void>()
  const rendered = await renderElementWithProviders(
    <RecordPaymentSheet groupId={testGroupId} me={filipMember} members={members} defaultCurrency="MKD" onClose={onClose} />,
    '/sheet',
    {
      seedCache: (queryClient) => {
        seedMe(testMe)(queryClient)
        queryClient.setQueryData(['groups', testGroupId], {})
      },
    },
  )
  const sheet = await screen.findByRole('dialog', { name: recordAnotherPayment })
  const run = holdOutboxSender(rendered.outboxSender)
  return { ...rendered, sheet, fetchMock, onClose, run }
}

function choosePayment(sheet: HTMLElement, direction: string, person: string, amount: string): void {
  fireEvent.click(sheetButton(sheet, direction))
  fireEvent.click(within(sheet).getByRole('button', { name: person }))
  typeAmount(amount)
}

function choosePaymentAndSave(sheet: HTMLElement, direction: string, person: string, amount: string): void {
  choosePayment(sheet, direction, person, amount)
  fireEvent.click(saveButton())
}

afterEach(() => {
  vi.useRealTimers()
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe('the Record another payment sheet saving through the outbox', () => {
  describe('when the server answers in time', () => {
    it('closes the sheet and queues nothing', async () => {
      const { sheet, onClose, run } = await renderOpenSheet()

      choosePaymentAndSave(sheet, someonePaidMe, 'Ana', '200')

      await waitForSheetToClose()
      expect(onClose).toHaveBeenCalled()
      expect(readSavedOutbox()).toEqual([])
      expect(run).not.toHaveBeenCalled()
    })
  })

  describe('when the payment counts right away and the server cannot be reached', () => {
    it('closes the sheet as if saved, with no error', async () => {
      const { sheet } = await renderOpenSheet(networkFailureAnswer())

      choosePaymentAndSave(sheet, someonePaidMe, 'Ana', '200')

      await waitForSheetToClose()
      expect(screen.queryByRole('alert')).toBeNull()
    })

    it('stores the payment of a person with an account to me with the request id and the payload of the request that failed', async () => {
      const { sheet, fetchMock } = await renderOpenSheet(networkFailureAnswer())

      choosePaymentAndSave(sheet, someonePaidMe, 'Ana', '200')

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
      expect(body).toMatchObject({ fromMemberId: anaMember.id, toMemberId: filipMember.id, amountMinor: 20000, currency: 'MKD' })
    })

    it('stores what the waiting row will show: the amount, the currency, the payer and the receiver', async () => {
      const { sheet } = await renderOpenSheet(networkFailureAnswer())

      choosePaymentAndSave(sheet, someonePaidMe, 'Ana', '200')

      await waitForSheetToClose()
      expect(readSavedOutbox()[0].display).toEqual({
        amountMinor: 20000,
        currency: 'MKD',
        payerName: 'Ana',
        receiverName: 'Filip',
      })
    })

    it('queues the owner paying a name without an account too, because that counts right away', async () => {
      const { sheet } = await renderOpenSheet(networkFailureAnswer())

      choosePaymentAndSave(sheet, iPaid, 'Marko', '300')

      await waitForSheetToClose()
      expect(readSavedOutbox()).toHaveLength(1)
      expect(readSavedOutbox()[0].display).toEqual({
        amountMinor: 30000,
        currency: 'MKD',
        payerName: 'Filip',
        receiverName: 'Marko',
      })
    })

    it('starts the sender once after the payment is queued', async () => {
      const { sheet, run } = await renderOpenSheet(networkFailureAnswer())

      choosePaymentAndSave(sheet, someonePaidMe, 'Ana', '200')

      await waitForSheetToClose()
      expect(run).toHaveBeenCalledTimes(1)
    })

    it.each([
      ['502', 502],
      ['503', 503],
      ['504', 504],
    ])('queues the payment on a %s from the server too', async (_name, status) => {
      const { sheet } = await renderOpenSheet(() => new Response(null, { status }))

      choosePaymentAndSave(sheet, someonePaidMe, 'Ana', '200')

      await waitForSheetToClose()
      expect(readSavedOutbox()).toHaveLength(1)
    })

    it('shows the generic error and keeps the sheet open when the payment could not be stored in the browser', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { sheet, outboxSender, onClose } = await renderOpenSheet(networkFailureAnswer())
      vi.spyOn(outboxSender, 'enqueue').mockRejectedValue(new Error('IndexedDB write failed'))

      choosePaymentAndSave(sheet, someonePaidMe, 'Ana', '200')

      expect((await within(sheet).findByRole('alert')).textContent).toContain(translated('en', 'errors.generic'))
      expect(screen.getByRole('dialog', { name: recordAnotherPayment })).toBe(sheet)
      expect(onClose).not.toHaveBeenCalled()
    })
  })

  describe('when the payment waits for the other person and the server cannot be reached', () => {
    it('does not queue I paid a person with an account, shows the network message and keeps the sheet open', async () => {
      const { sheet, run } = await renderOpenSheet(networkFailureAnswer())

      choosePaymentAndSave(sheet, iPaid, 'Ana', '500')

      expect((await within(sheet).findByRole('alert')).textContent).toContain(translated('en', 'errors.network'))
      expect(screen.getByRole('dialog', { name: recordAnotherPayment })).toBe(sheet)
      expect(readSavedOutbox()).toEqual([])
      expect(run).not.toHaveBeenCalled()
    })

    it('does not queue it on a 503 from the server either', async () => {
      const { sheet } = await renderOpenSheet(() => new Response(null, { status: 503 }))

      choosePaymentAndSave(sheet, iPaid, 'Ana', '500')

      expect((await within(sheet).findByRole('alert')).textContent).toContain(translated('en', 'errors.network'))
      expect(readSavedOutbox()).toEqual([])
    })

    it('does not queue it when the server gives no answer for 3 seconds, and keeps waiting', async () => {
      const { sheet, onClose } = await renderOpenSheet(neverAnswer())
      choosePayment(sheet, iPaid, 'Ana', '500')
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
      const { sheet, onClose } = await renderOpenSheet(neverAnswer())
      choosePayment(sheet, someonePaidMe, 'Ana', '200')
      installFakeTimers()

      fireEvent.click(saveButton())
      await letTimeGoBy(2999)

      expect(isDisabled(saveButton())).toBe(true)
      expect(readSavedOutbox()).toEqual([])
      expect(onClose).not.toHaveBeenCalled()
    })

    it('queues the payment, closes the sheet and starts the sender when the 3 seconds are over', async () => {
      const { sheet, fetchMock, run } = await renderOpenSheet(neverAnswer())
      choosePayment(sheet, someonePaidMe, 'Ana', '200')
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
      const { sheet, fetchMock } = await renderOpenSheet(() => late.promise)
      choosePayment(sheet, someonePaidMe, 'Ana', '200')
      installFakeTimers()
      fireEvent.click(saveButton())
      await letQueueTimeGoBy()
      expect(readSavedOutbox()).toHaveLength(1)

      late.resolve(Response.json(pendingPaymentOf(anaMember, filipMember, 20000)))
      await letTimeGoBy(0)

      expect(readSavedOutbox()).toEqual([])
      expect(requestCount(fetchMock, 'POST', settlementsPath)).toBe(1)
    })
  })

  describe('when the server refuses the payment', () => {
    it.each([
      [400, 'SETTLEMENT_AMOUNT_TOO_LARGE'],
      [400, 'SETTLEMENT_MEMBER_NOT_CURRENT'],
      [403, 'SETTLEMENT_NOT_ALLOWED'],
    ])('shows the translated text for %i %s inside the sheet, leaves it open and queues nothing', async (status, code) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { sheet, onClose, run } = await renderOpenSheet(problemAnswer(status, code))

      choosePaymentAndSave(sheet, someonePaidMe, 'Ana', '200')

      const alert = await within(sheet).findByRole('alert')
      expect(alert.textContent).toContain(translated('en', `errors.${code}`))
      expect(screen.getByRole('dialog', { name: recordAnotherPayment })).toBe(sheet)
      expect(onClose).not.toHaveBeenCalled()
      expect(readSavedOutbox()).toEqual([])
      expect(run).not.toHaveBeenCalled()
    })

    it('asks the groups again when the refusal says the group changed its state', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { sheet, queryClient } = await renderOpenSheet(problemAnswer(400, 'GROUP_FINISHED'))

      choosePaymentAndSave(sheet, someonePaidMe, 'Ana', '200')

      await within(sheet).findByRole('alert')
      await waitFor(() => {
        expect(queryClient.getQueryState(['groups', testGroupId])?.isInvalidated).toBe(true)
      })
      expect(readSavedOutbox()).toEqual([])
    })
  })
})
