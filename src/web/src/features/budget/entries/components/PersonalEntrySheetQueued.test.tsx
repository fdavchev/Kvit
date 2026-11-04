import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { amountField, isDisabled, saveButton, sentBody, typeAmount } from '@/test/expenseFormTestHelpers'
import { createDeferred } from '@/test/outboxTestHelpers'
import { coffeeCategory } from '@/test/personalCategoryTestData'
import { coffeeEntry, entryAnswerRow, salaryIncomeEntry } from '@/test/personalEntryTestData'
import {
  noteChip,
  personalEntriesPost,
  pickCategory,
  renderSpendingSheet,
  shownCurrency,
  waitForEveryDialogToClose,
  type SheetOptions,
} from '@/test/personalEntryTestHelpers'
import { findIncomeSheet, renderIncomeSheet } from '@/test/personalIncomeTestHelpers'
import {
  holdOutboxSender,
  letQueueTimeGoBy,
  letTimeGoBy,
  readSavedOutbox,
  installFakeTimers,
} from '@/test/queuedSaveTestHelpers'
import {
  jsonAnswer,
  networkFailureAnswer,
  neverAnswer,
  problemAnswer,
  requestCount,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { shownToastTexts } from '@/test/toastTestHelpers'
import { testMe } from '@/test/testMe'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const sheetNow = new Date('2026-10-19T10:00:00Z')
const today = '2026-10-19'
const lastCurrencyKey = `kvit.lastEntryCurrency.${testMe.id}`
const entriesUrl = '/api/personal/entries'

interface SheetKind {
  name: string
  entryKind: 'Spending' | 'Income'
  categoryId: string | null
  savedAnswer: Record<string, unknown>
  amountText: string
  amountMinor: number
  open: (answer: AnswerFactory, options?: Partial<SheetOptions>) => ReturnType<typeof renderSpendingSheet>
}

async function openSpending(answer: AnswerFactory, options: Partial<SheetOptions> = {}) {
  const rendered = await renderSpendingSheet({ answers: { [personalEntriesPost]: answer }, ...options })
  await pickCategory(coffeeCategory)
  return rendered
}

async function openIncome(answer: AnswerFactory, options: Partial<SheetOptions> = {}) {
  const rendered = await renderIncomeSheet({ answers: { [personalEntriesPost]: answer }, ...options })
  await findIncomeSheet()
  return rendered
}

const sheetKinds: SheetKind[] = [
  {
    name: 'PersonalSpendingSheet',
    entryKind: 'Spending',
    categoryId: coffeeCategory.id,
    savedAnswer: entryAnswerRow(coffeeEntry),
    amountText: '120',
    amountMinor: 12000,
    open: openSpending,
  },
  {
    name: 'PersonalIncomeSheet',
    entryKind: 'Income',
    categoryId: null,
    savedAnswer: entryAnswerRow(salaryIncomeEntry),
    amountText: '65000',
    amountMinor: 6500000,
    open: openIncome,
  },
]

function savedToastShown(): boolean {
  return shownToastTexts().includes(translated('en', 'groupSettings.saved'))
}

function typeNote(text: string): void {
  const sheet = screen.getByRole('dialog')
  fireEvent.click(noteChip(sheet))
  fireEvent.change(within(sheet).getByLabelText(translated('en', 'expense.note')), { target: { value: text } })
}

describe.each(sheetKinds)('$name adding an entry through the outbox', (kind) => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.clearAllMocks()
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(sheetNow)
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('when the server answers in time', () => {
    it('shows the Saved toast, closes the sheet, remembers the currency and queues nothing', async () => {
      const { onClose, outboxSender } = await kind.open(jsonAnswer(kind.savedAnswer))
      const run = holdOutboxSender(outboxSender)
      fireEvent.click(shownCurrency(screen.getByRole('dialog'), 'MKD'))
      typeAmount('12.50')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(onClose).toHaveBeenCalledOnce()
      })
      expect(savedToastShown()).toBe(true)
      expect(window.localStorage.getItem(lastCurrencyKey)).toBe('EUR')
      expect(readSavedOutbox()).toEqual([])
      expect(run).not.toHaveBeenCalled()
    })
  })

  describe('when the server cannot be reached', () => {
    it('closes the sheet as if saved, with no Saved toast, no error toast and no error text', async () => {
      const { onClose, outboxSender } = await kind.open(networkFailureAnswer())
      holdOutboxSender(outboxSender)
      typeAmount(kind.amountText)

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(onClose).toHaveBeenCalledOnce()
      })
      await waitForEveryDialogToClose()
      expect(savedToastShown()).toBe(false)
      expect(vi.mocked(toast.error)).not.toHaveBeenCalled()
      expect(screen.queryByRole('alert')).toBeNull()
    })

    it('still remembers the currency of the entry', async () => {
      const { onClose, outboxSender } = await kind.open(networkFailureAnswer())
      holdOutboxSender(outboxSender)
      fireEvent.click(shownCurrency(screen.getByRole('dialog'), 'MKD'))
      typeAmount('12.50')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(onClose).toHaveBeenCalledOnce()
      })
      expect(window.localStorage.getItem(lastCurrencyKey)).toBe('EUR')
    })

    it('stores the entry in the outbox with no group, the request id and the payload of the request that failed', async () => {
      const { fetchMock, onClose, outboxSender } = await kind.open(networkFailureAnswer())
      holdOutboxSender(outboxSender)
      typeAmount(kind.amountText)

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(onClose).toHaveBeenCalledOnce()
      })
      const body = sentBody(fetchMock, 'POST', entriesUrl)
      expect(readSavedOutbox()).toHaveLength(1)
      expect(readSavedOutbox()[0]).toMatchObject({
        id: body.clientRequestId,
        kind: 'personalEntry',
        groupId: null,
        state: 'waiting',
        failure: null,
      })
      expect(readSavedOutbox()[0].payload).toEqual(body)
      expect(body).toMatchObject({ kind: kind.entryKind, amountMinor: kind.amountMinor, currency: 'MKD', entryDate: today })
    })

    it('stores what the waiting row will show: the note as the title, amount, currency, date, category and kind', async () => {
      const { onClose, outboxSender } = await kind.open(networkFailureAnswer())
      holdOutboxSender(outboxSender)
      typeAmount(kind.amountText)
      typeNote('Latte')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(onClose).toHaveBeenCalledOnce()
      })
      expect(readSavedOutbox()[0].display).toEqual({
        title: 'Latte',
        amountMinor: kind.amountMinor,
        currency: 'MKD',
        date: today,
        categoryId: kind.categoryId,
        entryKind: kind.entryKind,
      })
    })

    it('starts the sender once after the entry is queued', async () => {
      const { onClose, outboxSender } = await kind.open(networkFailureAnswer())
      const run = holdOutboxSender(outboxSender)
      typeAmount(kind.amountText)

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(onClose).toHaveBeenCalledOnce()
      })
      expect(run).toHaveBeenCalledTimes(1)
    })

    it.each([
      ['502', 502],
      ['503', 503],
      ['504', 504],
    ])('queues the entry on a %s from the server too', async (_name, status) => {
      const { onClose, outboxSender } = await kind.open(() => new Response(null, { status }))
      holdOutboxSender(outboxSender)
      typeAmount(kind.amountText)

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(onClose).toHaveBeenCalledOnce()
      })
      expect(readSavedOutbox()).toHaveLength(1)
    })

    it('sends one request and queues one entry when the form is submitted twice quickly', async () => {
      const { fetchMock, onClose, outboxSender } = await kind.open(networkFailureAnswer())
      holdOutboxSender(outboxSender)
      typeAmount(kind.amountText)
      const form = amountField().closest('form')
      if (form === null) {
        throw new Error('The amount is not inside a form')
      }

      fireEvent.submit(form)
      fireEvent.submit(form)

      await waitFor(() => {
        expect(onClose).toHaveBeenCalled()
      })
      expect(requestCount(fetchMock, 'POST', entriesUrl)).toBe(1)
      expect(readSavedOutbox()).toHaveLength(1)
    })

    it('shows the generic error and keeps the sheet open when the entry could not be stored in the browser', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { onClose, outboxSender } = await kind.open(networkFailureAnswer())
      vi.spyOn(outboxSender, 'enqueue').mockRejectedValue(new Error('IndexedDB write failed'))
      typeAmount(kind.amountText)

      fireEvent.click(saveButton())

      expect((await screen.findByRole('alert')).textContent).toBe(translated('en', 'errors.generic'))
      expect(onClose).not.toHaveBeenCalled()
      expect(window.localStorage.length).toBe(0)
    })
  })

  describe('when the server gives no answer for 3 seconds', () => {
    it('keeps the sheet open with Save disabled during the 3 seconds and queues nothing yet', async () => {
      const { onClose } = await kind.open(neverAnswer())
      typeAmount(kind.amountText)
      installFakeTimers(sheetNow)

      fireEvent.click(saveButton())
      await letTimeGoBy(2999)

      expect(isDisabled(saveButton())).toBe(true)
      expect(onClose).not.toHaveBeenCalled()
      expect(readSavedOutbox()).toEqual([])
    })

    it('queues the entry, closes the sheet without a toast and starts the sender when the 3 seconds are over', async () => {
      const { fetchMock, onClose, outboxSender } = await kind.open(neverAnswer())
      const run = holdOutboxSender(outboxSender)
      typeAmount(kind.amountText)
      installFakeTimers(sheetNow)

      fireEvent.click(saveButton())
      await letQueueTimeGoBy()

      expect(onClose).toHaveBeenCalledOnce()
      expect(readSavedOutbox()).toHaveLength(1)
      expect(readSavedOutbox()[0].payload).toEqual(sentBody(fetchMock, 'POST', entriesUrl))
      expect(savedToastShown()).toBe(false)
      expect(window.localStorage.getItem(lastCurrencyKey)).toBe('MKD')
      expect(run).toHaveBeenCalledTimes(1)
    })
  })

  describe('when the original request answers after the entry was queued', () => {
    it('takes the entry out of the outbox and sends nothing a second time', async () => {
      const late = createDeferred<Response>()
      const { fetchMock, outboxSender } = await kind.open(() => late.promise)
      holdOutboxSender(outboxSender)
      typeAmount(kind.amountText)
      installFakeTimers(sheetNow)
      fireEvent.click(saveButton())
      await letQueueTimeGoBy()
      expect(readSavedOutbox()).toHaveLength(1)

      late.resolve(Response.json(kind.savedAnswer))
      await letTimeGoBy(0)

      expect(readSavedOutbox()).toEqual([])
      expect(requestCount(fetchMock, 'POST', entriesUrl)).toBe(1)
    })
  })

  describe('when the server refuses the entry', () => {
    it('shows the translated error in the sheet, keeps it open, queues nothing and forgets the currency', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { onClose, outboxSender } = await kind.open(problemAnswer(400, 'PERSONAL_ENTRY_AMOUNT_TOO_LARGE'))
      const run = holdOutboxSender(outboxSender)
      fireEvent.click(shownCurrency(screen.getByRole('dialog'), 'MKD'))
      typeAmount('12')

      fireEvent.click(saveButton())

      expect((await screen.findByRole('alert')).textContent).toBe(
        translated('en', 'errors.PERSONAL_ENTRY_AMOUNT_TOO_LARGE'),
      )
      expect(onClose).not.toHaveBeenCalled()
      expect(screen.getByRole('dialog')).toBeTruthy()
      expect(readSavedOutbox()).toEqual([])
      expect(run).not.toHaveBeenCalled()
      expect(window.localStorage.length).toBe(0)
      expect(savedToastShown()).toBe(false)
    })

    it('lets Save be tapped again after the refusal', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await kind.open(problemAnswer(400, 'PERSONAL_ENTRY_AMOUNT_TOO_LARGE'))
      typeAmount(kind.amountText)
      fireEvent.click(saveButton())
      await screen.findByRole('alert')

      expect(isDisabled(saveButton())).toBe(false)
    })
  })
})
