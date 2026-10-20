import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { amountField, doneButton, isDisabled, saveButton, sentBody, typeAmount, typeDate, yesterdayChip } from '@/test/expenseFormTestHelpers'
import { entryAnswerRow, salaryIncomeEntry } from '@/test/personalEntryTestData'
import {
  isEntriesCacheRefreshed,
  noteChip,
  personalEntriesPost,
  shownCurrency,
  waitForEveryDialogToClose,
} from '@/test/personalEntryTestHelpers'
import { findIncomeSheet, renderIncomeSheet } from '@/test/personalIncomeTestHelpers'
import {
  anyEndsChip,
  anyStartChip,
  chooseEnds,
  findDateSheet,
  sentRecurringRequests,
  sentWritesToRecurringIncomes,
  sheetNow,
  tapRepeats,
} from '@/test/recurringIncomeTestHelpers'
import {
  catchUpAnswer,
  catchUpPost,
  recurringIncomeAnswerRow,
  recurringIncomesPost,
  salaryRecurringIncome,
} from '@/test/recurringIncomeTestData'
import {
  jsonAnswer,
  networkFailureAnswer,
  neverAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { testMe } from '@/test/testMe'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/
const createdAnswers = {
  [recurringIncomesPost]: jsonAnswer(recurringIncomeAnswerRow(salaryRecurringIncome)),
  [catchUpPost]: catchUpAnswer(1),
}
const createUrl = '/api/personal/recurring-incomes'
const catchUpUrl = '/api/personal/recurring-incomes/catch-up'
const lastCurrencyKey = `kvit.lastEntryCurrency.${testMe.id}`
const createAndCatchUp = [`POST ${createUrl}`, `POST ${catchUpUrl}`]

type RefusedField = 'amount' | 'date' | 'note' | 'ends' | null

interface RefusalCase {
  errorCode: string
  status: number
  field: RefusedField
}

const refusalCases: RefusalCase[] = [
  { errorCode: 'RECURRING_INCOME_AMOUNT_NOT_POSITIVE', status: 400, field: 'amount' },
  { errorCode: 'RECURRING_INCOME_AMOUNT_TOO_LARGE', status: 400, field: 'amount' },
  { errorCode: 'RECURRING_INCOME_CURRENCY_INVALID', status: 400, field: 'amount' },
  { errorCode: 'MONEY_NOT_ON_CURRENCY_STEP', status: 400, field: 'amount' },
  { errorCode: 'RECURRING_INCOME_TITLE_INVALID', status: 400, field: 'note' },
  { errorCode: 'RECURRING_INCOME_START_DATE_INVALID', status: 400, field: 'date' },
  { errorCode: 'RECURRING_INCOME_DAY_INVALID', status: 400, field: 'date' },
  { errorCode: 'RECURRING_INCOME_END_DATE_INVALID', status: 400, field: 'ends' },
  { errorCode: 'RECURRING_INCOME_FREQUENCY_INVALID', status: 400, field: null },
  { errorCode: 'RECURRING_INCOME_CLIENT_REQUEST_ID_USED', status: 400, field: null },
  { errorCode: 'RECURRING_INCOME_LIMIT_REACHED', status: 400, field: null },
  { errorCode: 'GUEST_NOT_ALLOWED', status: 403, field: null },
]

function refusedFields(sheet: HTMLElement): RefusedField[] {
  const note = within(sheet).getByLabelText(translated('en', 'expense.note'))
  const marks: [RefusedField, boolean][] = [
    ['amount', amountField().getAttribute('aria-invalid') === 'true'],
    ['date', anyStartChip(sheet).getAttribute('aria-invalid') === 'true'],
    ['note', note.getAttribute('aria-invalid') === 'true'],
    ['ends', anyEndsChip(sheet).getAttribute('aria-invalid') === 'true'],
  ]
  return marks.filter(([, isMarked]) => isMarked).map(([field]) => field)
}

async function openRepeatingSheet(taps: 1 | 2, answers: Record<string, AnswerFactory> = createdAnswers) {
  const rendered = await renderIncomeSheet({ answers })
  const sheet = await findIncomeSheet()
  tapRepeats(sheet, taps)
  return { ...rendered, sheet }
}

async function saveRefusedRepeatingIncome(errorCode: string, status: number) {
  const rendered = await openRepeatingSheet(1, { ...createdAnswers, [recurringIncomesPost]: problemAnswer(status, errorCode) })
  typeAmount('65000')
  fireEvent.click(noteChip(rendered.sheet))
  fireEvent.change(within(rendered.sheet).getByLabelText(translated('en', 'expense.note')), { target: { value: 'Salary' } })
  fireEvent.click(saveButton())
  await screen.findByRole('alert')
  return rendered
}

describe('PersonalIncomeSheet, saving a repeating income', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(sheetNow)
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.clearAllMocks()
    vi.restoreAllMocks()
  })

  describe('what is sent', () => {
    it('sends ONE POST to the repeating incomes with a request id and a monthly income on the 20th, and no entry', async () => {
      const { fetchMock } = await openRepeatingSheet(1)

      typeAmount('65000')
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'POST', createUrl)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'POST', createUrl)).toEqual({
        clientRequestId: expect.stringMatching(uuidPattern),
        amountMinor: 6500000,
        currency: 'MKD',
        title: null,
        frequency: 'Monthly',
        dayOfMonth: 20,
        dayOfWeek: null,
        startsOn: '2026-10-20',
        endsOn: null,
      })
      expect(requestCount(fetchMock, 'POST', '/api/personal/entries')).toBe(0)
    })

    it('sends a weekly income on Tuesday (2) with no day of the month', async () => {
      const { fetchMock } = await openRepeatingSheet(2)

      typeAmount('1500')
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'POST', createUrl)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'POST', createUrl)).toMatchObject({
        frequency: 'Weekly',
        dayOfMonth: null,
        dayOfWeek: 2,
        startsOn: '2026-10-20',
        amountMinor: 150000,
      })
    })

    it.each([
      ['2026-10-18', 0],
      ['2026-10-24', 6],
    ])('sends Sunday as 0 and Saturday as 6: the start %s gives the weekday %i', async (start, dayOfWeek) => {
      vi.setSystemTime(new Date(`${start}T10:00:00Z`))
      const { fetchMock } = await openRepeatingSheet(2)

      typeAmount('1500')
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'POST', createUrl)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'POST', createUrl)).toMatchObject({ dayOfWeek, dayOfMonth: null })
    })

    it('sends the trimmed note as the title', async () => {
      const { fetchMock, sheet } = await openRepeatingSheet(1)
      typeAmount('65000')

      fireEvent.click(noteChip(sheet))
      fireEvent.change(within(sheet).getByLabelText(translated('en', 'expense.note')), { target: { value: '  Salary  ' } })
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'POST', createUrl)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'POST', createUrl)).toMatchObject({ title: 'Salary' })
    })

    it('sends a note of only spaces as no title', async () => {
      const { fetchMock, sheet } = await openRepeatingSheet(1)
      typeAmount('65000')

      fireEvent.click(noteChip(sheet))
      fireEvent.change(within(sheet).getByLabelText(translated('en', 'expense.note')), { target: { value: '   ' } })
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'POST', createUrl)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'POST', createUrl)).toMatchObject({ title: null })
    })

    it('sends euros as cents', async () => {
      const { fetchMock, sheet } = await openRepeatingSheet(1)

      fireEvent.click(shownCurrency(sheet, 'MKD'))
      typeAmount('250.5')
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'POST', createUrl)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'POST', createUrl)).toMatchObject({ amountMinor: 25050, currency: 'EUR' })
    })

    it.each([
      ['endsIn3Months', '2027-01-20'],
      ['endsIn6Months', '2027-04-20'],
      ['endsIn1Year', '2027-10-20'],
    ] as const)('sends the end %s as %s', async (key, endsOn) => {
      const { fetchMock, sheet } = await openRepeatingSheet(1)
      typeAmount('65000')

      await chooseEnds(sheet, key)
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'POST', createUrl)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'POST', createUrl)).toMatchObject({ endsOn })
    })

    it('sends a picked end date', async () => {
      const { fetchMock, sheet } = await openRepeatingSheet(1)
      typeAmount('65000')
      fireEvent.click(anyEndsChip(sheet))
      fireEvent.click(await screen.findByRole('button', { name: translated('en', 'budget.income.endsPickDate') }))
      const dateSheet = await findDateSheet()

      typeDate(dateSheet, '2027-02-10')
      fireEvent.click(doneButton(dateSheet))
      await waitFor(() => {
        expect(screen.getAllByRole('dialog')).toHaveLength(1)
      })
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'POST', createUrl)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'POST', createUrl)).toMatchObject({ endsOn: '2027-02-10' })
    })

    it('sends no end after Never was chosen again', async () => {
      const { fetchMock, sheet } = await openRepeatingSheet(1)
      typeAmount('65000')
      await chooseEnds(sheet, 'endsIn3Months')
      await chooseEnds(sheet, 'endsOptionNever')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'POST', createUrl)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'POST', createUrl)).toMatchObject({ endsOn: null })
    })

    it('sends the start of Yesterday with the 19th of the month', async () => {
      const { fetchMock, sheet } = await openRepeatingSheet(1)
      typeAmount('65000')

      fireEvent.click(anyStartChip(sheet))
      fireEvent.click(yesterdayChip(await findDateSheet()))
      await waitFor(() => {
        expect(screen.queryByRole('dialog', { name: translated('en', 'expense.pickDate') })).toBeNull()
      })
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'POST', createUrl)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'POST', createUrl)).toMatchObject({ startsOn: '2026-10-19', dayOfMonth: 19, dayOfWeek: null })
    })

    it.each([
      ['2026-12-31', 31],
      ['2026-12-29', 29],
    ])('sends the day of the month of a typed start: %s gives day %i', async (start, dayOfMonth) => {
      const { fetchMock, sheet } = await openRepeatingSheet(1)
      typeAmount('65000')

      fireEvent.click(anyStartChip(sheet))
      const dateSheet = await findDateSheet()
      typeDate(dateSheet, start)
      fireEvent.click(doneButton(dateSheet))
      await waitFor(() => {
        expect(screen.queryByRole('dialog', { name: translated('en', 'expense.pickDate') })).toBeNull()
      })
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'POST', createUrl)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'POST', createUrl)).toMatchObject({ startsOn: start, dayOfMonth })
    })
  })

  describe('the order of the requests and what happens after', () => {
    it('sends the create, then the catch-up, and nothing else to the repeating incomes', async () => {
      const { fetchMock } = await openRepeatingSheet(1)
      typeAmount('65000')

      fireEvent.click(saveButton())

      await waitForEveryDialogToClose()
      expect(sentRecurringRequests(fetchMock)).toEqual(createAndCatchUp)
    })

    it('saves a repeating income in 1 tap and 1 field after the chip: Repeats, amount, Save', async () => {
      const { fetchMock } = await renderIncomeSheet({ answers: createdAnswers })
      const sheet = await findIncomeSheet()
      let taps = 0

      tapRepeats(sheet, 1)
      taps += 1
      typeAmount('65000')
      fireEvent.click(saveButton())
      taps += 1

      await waitForEveryDialogToClose()
      expect(taps).toBe(2)
      expect(sentRecurringRequests(fetchMock)).toEqual(createAndCatchUp)
    })

    it('closes, says Saved, refreshes the entries and remembers the currency, after the catch-up', async () => {
      const { onClose, queryClient, sheet } = await openRepeatingSheet(1)

      fireEvent.click(shownCurrency(sheet, 'MKD'))
      typeAmount('250')
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(onClose).toHaveBeenCalledOnce()
      })
      await waitForEveryDialogToClose()
      expect(vi.mocked(toast.success)).toHaveBeenCalledWith(translated('en', 'groupSettings.saved'))
      expect(isEntriesCacheRefreshed(queryClient)).toBe(true)
      expect(window.localStorage.getItem(lastCurrencyKey)).toBe('EUR')
    })

    it('stays open until the catch-up has answered', async () => {
      const { fetchMock, onClose } = await openRepeatingSheet(1, { ...createdAnswers, [catchUpPost]: neverAnswer() })
      typeAmount('65000')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'POST', catchUpUrl)).toHaveLength(1)
      })
      expect(onClose).not.toHaveBeenCalled()
      expect(vi.mocked(toast.success)).not.toHaveBeenCalled()
    })

    it('still closes with Saved when the catch-up fails after the income was saved, and logs the failure', async () => {
      const logged = vi.spyOn(console, 'error').mockImplementation(() => undefined)
      const { onClose, queryClient } = await openRepeatingSheet(1, { ...createdAnswers, [catchUpPost]: networkFailureAnswer() })
      typeAmount('65000')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(onClose).toHaveBeenCalledOnce()
      })
      expect(vi.mocked(toast.success)).toHaveBeenCalledWith(translated('en', 'groupSettings.saved'))
      expect(isEntriesCacheRefreshed(queryClient)).toBe(true)
      expect(screen.queryByRole('alert')).toBeNull()
      expect(logged).toHaveBeenCalled()
    })

    it('keeps Save disabled until the amount is a positive amount', async () => {
      await openRepeatingSheet(1)

      expect(isDisabled(saveButton())).toBe(true)
      typeAmount('0')
      expect(isDisabled(saveButton())).toBe(true)
      typeAmount('5')
      expect(isDisabled(saveButton())).toBe(false)
    })
  })

  describe('saving once', () => {
    it('sends one create and one catch-up when Save is tapped twice quickly', async () => {
      const { fetchMock } = await openRepeatingSheet(1)
      typeAmount('65000')

      fireEvent.click(saveButton())
      fireEvent.click(saveButton())

      await waitForEveryDialogToClose()
      expect(sentRecurringRequests(fetchMock)).toEqual(createAndCatchUp)
    })

    it('sends one create when Enter is pressed twice in the amount', async () => {
      const { fetchMock } = await openRepeatingSheet(1)
      typeAmount('65000')

      const form = amountField().closest('form')
      if (form === null) {
        throw new Error('The amount is not inside a form')
      }
      fireEvent.submit(form)
      fireEvent.submit(form)

      await waitForEveryDialogToClose()
      expect(sentRecurringRequests(fetchMock)).toEqual(createAndCatchUp)
    })

    it('sends the same request id again after a network failure, and the catch-up only after the create worked', async () => {
      let answer = networkFailureAnswer()
      const { fetchMock } = await openRepeatingSheet(1, { ...createdAnswers, [recurringIncomesPost]: () => answer() })
      typeAmount('65000')
      fireEvent.click(saveButton())
      await screen.findByRole('alert')
      expect(requestCount(fetchMock, 'POST', catchUpUrl)).toBe(0)

      answer = jsonAnswer(recurringIncomeAnswerRow(salaryRecurringIncome))
      fireEvent.click(saveButton())

      await waitForEveryDialogToClose()
      const [first, second] = requestsOf(fetchMock, 'POST', createUrl).map((request) => (request.body as { clientRequestId: string }).clientRequestId)
      expect(second).toBe(first)
      expect(requestCount(fetchMock, 'POST', catchUpUrl)).toBe(1)
    })

    it('makes a new request id every time the sheet opens', async () => {
      const first = await openRepeatingSheet(1)
      typeAmount('65000')
      fireEvent.click(saveButton())
      await waitForEveryDialogToClose()
      const firstId = (requestsOf(first.fetchMock, 'POST', createUrl)[0].body as { clientRequestId: string }).clientRequestId

      const second = await openRepeatingSheet(1)
      typeAmount('70000')
      fireEvent.click(saveButton())
      await waitForEveryDialogToClose()

      expect((requestsOf(second.fetchMock, 'POST', createUrl)[0].body as { clientRequestId: string }).clientRequestId).not.toBe(firstId)
    })
  })

  describe('with Never', () => {
    it('still sends ONE POST to the entries and nothing to the repeating incomes', async () => {
      const { fetchMock } = await renderIncomeSheet({
        answers: { [personalEntriesPost]: jsonAnswer(entryAnswerRow(salaryIncomeEntry)) },
      })
      await findIncomeSheet()

      typeAmount('65000')
      fireEvent.click(saveButton())

      await waitForEveryDialogToClose()
      expect(requestCount(fetchMock, 'POST', '/api/personal/entries')).toBe(1)
      expect(sentRecurringRequests(fetchMock)).toEqual([])
    })

    it('sends the entry, and nothing to the repeating incomes, after the chip was tapped round to Never again', async () => {
      const { fetchMock } = await renderIncomeSheet({
        answers: { [personalEntriesPost]: jsonAnswer(entryAnswerRow(salaryIncomeEntry)) },
      })
      const sheet = await findIncomeSheet()
      typeAmount('65000')

      tapRepeats(sheet, 3)
      fireEvent.click(saveButton())

      await waitForEveryDialogToClose()
      expect(requestCount(fetchMock, 'POST', '/api/personal/entries')).toBe(1)
      expect(sentRecurringRequests(fetchMock)).toEqual([])
      expect(sentBody(fetchMock, 'POST', '/api/personal/entries')).toMatchObject({ kind: 'Income', entryDate: '2026-10-20' })
    })
  })

  describe('refused creates', () => {
    it.each(refusalCases)('shows the translated message for $errorCode above Save', async ({ errorCode, status }) => {
      await saveRefusedRepeatingIncome(errorCode, status)

      const alert = screen.getByRole('alert')
      expect(alert.textContent).toBe(translated('en', `errors.${errorCode}`))
      expect(alert.compareDocumentPosition(saveButton()) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
    })

    it.each(refusalCases)('puts the red edge on the field $errorCode is about ($field) and on no other', async ({ errorCode, status, field }) => {
      const { sheet } = await saveRefusedRepeatingIncome(errorCode, status)

      expect(refusedFields(sheet)).toEqual(field === null ? [] : [field])
    })

    it.each(refusalCases)('keeps the typed amount, the note and the sheet open after $errorCode, with no catch-up', async ({ errorCode, status }) => {
      const { sheet, onClose, fetchMock } = await saveRefusedRepeatingIncome(errorCode, status)

      expect(amountField().value).toBe('65000')
      expect(within(sheet).getByLabelText<HTMLTextAreaElement>(translated('en', 'expense.note')).value).toBe('Salary')
      expect(onClose).not.toHaveBeenCalled()
      expect(sentWritesToRecurringIncomes(fetchMock)).toEqual([`POST ${createUrl}`])
    })

    it('takes the red edge off the amount once the amount is changed', async () => {
      await saveRefusedRepeatingIncome('RECURRING_INCOME_AMOUNT_TOO_LARGE', 400)

      typeAmount('130')

      expect(amountField().getAttribute('aria-invalid')).not.toBe('true')
    })

    it('takes the red edge off the start once another start is picked', async () => {
      const { sheet } = await saveRefusedRepeatingIncome('RECURRING_INCOME_START_DATE_INVALID', 400)

      fireEvent.click(anyStartChip(sheet))
      fireEvent.click(yesterdayChip(await findDateSheet()))
      await waitFor(() => {
        expect(screen.queryByRole('dialog', { name: translated('en', 'expense.pickDate') })).toBeNull()
      })

      expect(anyStartChip(sheet).getAttribute('aria-invalid')).not.toBe('true')
    })

    it('takes the red edge off the Ends chip once another end is chosen', async () => {
      const { sheet } = await saveRefusedRepeatingIncome('RECURRING_INCOME_END_DATE_INVALID', 400)

      await chooseEnds(sheet, 'endsIn6Months')

      expect(anyEndsChip(sheet).getAttribute('aria-invalid')).not.toBe('true')
    })

    it('takes the red edge off the note once the note is changed', async () => {
      const { sheet } = await saveRefusedRepeatingIncome('RECURRING_INCOME_TITLE_INVALID', 400)

      const note = within(sheet).getByLabelText(translated('en', 'expense.note'))
      fireEvent.change(note, { target: { value: 'shorter' } })

      expect(note.getAttribute('aria-invalid')).not.toBe('true')
    })

    it('shows the network message when the server cannot be reached', async () => {
      await openRepeatingSheet(1, { ...createdAnswers, [recurringIncomesPost]: networkFailureAnswer() })
      typeAmount('65000')

      fireEvent.click(saveButton())

      expect((await screen.findByRole('alert')).textContent).toBe(translated('en', 'errors.network'))
    })

    it('lets Save be tapped again after a refusal and sends a second create', async () => {
      const { fetchMock } = await saveRefusedRepeatingIncome('RECURRING_INCOME_AMOUNT_TOO_LARGE', 400)

      typeAmount('130')
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'POST', createUrl)).toHaveLength(2)
      })
    })

    it('does not change the remembered currency after a refused save', async () => {
      await saveRefusedRepeatingIncome('RECURRING_INCOME_LIMIT_REACHED', 400)

      expect(window.localStorage.length).toBe(0)
    })
  })
})
