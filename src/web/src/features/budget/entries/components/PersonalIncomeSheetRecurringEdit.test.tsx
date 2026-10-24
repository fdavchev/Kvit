import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { amountField, dateField, isDisabled, saveButton, sentBody, typeAmount, yesterdayChip } from '@/test/expenseFormTestHelpers'
import { describedText, isEntriesCacheRefreshed, noteChip, shownCurrency, waitForEveryDialogToClose } from '@/test/personalEntryTestHelpers'
import { findIncomeEditSheet, renderIncomeSheet } from '@/test/personalIncomeTestHelpers'
import {
  anyEndsChip,
  anyStartChip,
  chooseEnds,
  endsChip,
  findDateSheet,
  repeatsChipIn,
  sentWritesToRecurringIncomes,
  sheetNow,
  startChip,
  tapRepeats,
} from '@/test/recurringIncomeTestHelpers'
import {
  catchUpAnswer,
  catchUpPost,
  flatRentRecurringIncome,
  recurringIncomeAnswerRow,
  recurringIncomePut,
  recurringIncomeRestartPost,
  recurringIncomeStopPost,
  recurringIncomeUrl,
  salaryRecurringIncome,
  tutoringRecurringIncome,
  untitledRecurringIncome,
} from '@/test/recurringIncomeTestData'
import {
  jsonAnswer,
  networkFailureAnswer,
  neverAnswer,
  noContentAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { expectSaveOutsideScrollingFields, expectTitleOutsideScrollingBody } from '@/test/sheetScrollTestHelpers'
import { testMe } from '@/test/testMe'
import { pressToastAction, toastShownWithText } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const salary = salaryRecurringIncome
const salaryUrl = recurringIncomeUrl(salary.id)
const catchUpUrl = '/api/personal/recurring-incomes/catch-up'
const saveAnswers = {
  [recurringIncomePut(salary.id)]: jsonAnswer(recurringIncomeAnswerRow(salary)),
  [catchUpPost]: catchUpAnswer(2),
}
const stopAnswers = {
  [recurringIncomeStopPost(salary.id)]: noContentAnswer(),
  [recurringIncomeRestartPost(salary.id)]: noContentAnswer(),
  [catchUpPost]: catchUpAnswer(1),
}
const putAndCatchUp = [`PUT ${salaryUrl}`, `POST ${catchUpUrl}`]

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
  { errorCode: 'RECURRING_INCOME_TITLE_INVALID', status: 400, field: 'note' },
  { errorCode: 'RECURRING_INCOME_START_DATE_INVALID', status: 400, field: 'date' },
  { errorCode: 'RECURRING_INCOME_START_CANNOT_CHANGE', status: 400, field: 'date' },
  { errorCode: 'RECURRING_INCOME_DAY_INVALID', status: 400, field: 'date' },
  { errorCode: 'RECURRING_INCOME_END_DATE_INVALID', status: 400, field: 'ends' },
  { errorCode: 'RECURRING_INCOME_FREQUENCY_INVALID', status: 400, field: null },
  { errorCode: 'RECURRING_INCOME_LIMIT_REACHED', status: 400, field: null },
  { errorCode: 'GUEST_NOT_ALLOWED', status: 403, field: null },
]

function stopButton(sheet: HTMLElement, language: 'en' | 'mk' = 'en'): HTMLElement {
  return within(sheet).getByRole('button', { name: translated(language, 'budget.recurring.stop') })
}

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

async function openSalary(answers: Record<string, AnswerFactory> = saveAnswers) {
  const rendered = await renderIncomeSheet({ recurringIncome: salary, answers })
  const sheet = await findIncomeEditSheet()
  return { ...rendered, sheet }
}

async function saveRefusedEdit(errorCode: string, status: number) {
  const rendered = await openSalary({ ...saveAnswers, [recurringIncomePut(salary.id)]: problemAnswer(status, errorCode) })
  fireEvent.click(saveButton())
  await screen.findByRole('alert')
  return rendered
}

describe('PersonalIncomeSheet, editing a repeating income', () => {
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

  describe('opening', () => {
    it.each(languages)('is filled in with the amount, currency, name, start, repeat rhythm and end of the income (%s)', async (language) => {
      await renderIncomeSheet({ recurringIncome: salary, language })

      const sheet = await findIncomeEditSheet(language)

      expect(amountField(language).value).toBe('30000')
      expect(shownCurrency(sheet, 'MKD', language)).toBeTruthy()
      expect(within(sheet).getByLabelText<HTMLTextAreaElement>(translated(language, 'expense.note')).value).toBe('Salary')
      expect(startChip(sheet, language === 'en' ? '1 Aug' : '1 авг', language)).toBeTruthy()
      expect(repeatsChipIn(sheet, 'Monthly', language)).toBeTruthy()
      expect(endsChip(sheet, null, language)).toBeTruthy()
    })

    it.each(languages)('is titled with the edit title, not the title of a new income (%s)', async (language) => {
      await renderIncomeSheet({ recurringIncome: salary, language })

      const sheet = await findIncomeEditSheet(language)

      expect(within(sheet).getByRole('heading', { name: translated(language, 'budget.income.editTitle') })).toBeTruthy()
      expect(screen.queryByRole('heading', { name: translated(language, 'budget.income.title') })).toBeNull()
    })

    it.each(languages)('shows the start date on a disabled chip when the income already has entries (%s)', async (language) => {
      await renderIncomeSheet({ recurringIncome: { ...salary, hasEntries: true }, language })

      const sheet = await findIncomeEditSheet(language)
      const chip = startChip(sheet, language === 'en' ? '1 Aug' : '1 авг', language)

      expect(isDisabled(chip)).toBe(true)
      fireEvent.click(chip)
      expect(screen.queryByRole('dialog', { name: translated(language, 'expense.pickDate') })).toBeNull()
    })

    it('keeps the start date chip enabled when the income has no entries yet', async () => {
      const { sheet } = await openSalary()

      expect(isDisabled(anyStartChip(sheet))).toBe(false)
    })

    it.each(languages)('says why the start date is locked, under the chips and linked to the chip (%s)', async (language) => {
      await renderIncomeSheet({ recurringIncome: { ...salary, hasEntries: true }, language })

      const sheet = await findIncomeEditSheet(language)
      const chip = startChip(sheet, language === 'en' ? '1 Aug' : '1 авг', language)

      expect(chip.getAttribute('aria-disabled')).toBe('true')
      expect(within(sheet).getByText(translated(language, 'budget.income.startLockedReason'))).toBeTruthy()
      expect(describedText(chip)).toBe(translated(language, 'budget.income.startLockedReason'))
    })

    it('keeps the locked start chip in the Tab order, so a keyboard or screen reader still reaches the start date', async () => {
      await renderIncomeSheet({ recurringIncome: { ...salary, hasEntries: true } })

      const chip = anyStartChip(await findIncomeEditSheet())

      expect(chip.matches(':disabled')).toBe(false)
      expect(chip.tabIndex).toBeGreaterThanOrEqual(0)
    })

    it.each([
      ['has no entries yet', false],
      ['comes from an older API that does not say whether it has entries', null],
    ] as const)('shows no reason and leaves the start open when the income %s', async (_name, hasEntries) => {
      await renderIncomeSheet({ recurringIncome: { ...salary, hasEntries } })

      const sheet = await findIncomeEditSheet()

      expect(isDisabled(anyStartChip(sheet))).toBe(false)
      expect(anyStartChip(sheet).hasAttribute('aria-describedby')).toBe(false)
      expect(within(sheet).queryByText(translated('en', 'budget.income.startLockedReason'))).toBeNull()
    })

    it('keeps the other chips and Save enabled when the start date is locked', async () => {
      await renderIncomeSheet({ recurringIncome: { ...salary, hasEntries: true } })

      const sheet = await findIncomeEditSheet()

      expect(isDisabled(repeatsChipIn(sheet, 'Monthly'))).toBe(false)
      expect(isDisabled(anyEndsChip(sheet))).toBe(false)
      expect(isDisabled(saveButton())).toBe(false)
    })

    it('shows the rhythm hint of the start: the 1st for the salary', async () => {
      const { sheet } = await openSalary()

      expect(within(sheet).getByText('Every month on the 1st')).toBeTruthy()
    })

    it('fills in a weekly income with its end date and the weekday of its start', async () => {
      await renderIncomeSheet({ recurringIncome: tutoringRecurringIncome })
      const sheet = await findIncomeEditSheet()

      expect(amountField().value).toBe('1500')
      expect(repeatsChipIn(sheet, 'Weekly')).toBeTruthy()
      expect(startChip(sheet, '1 Sep')).toBeTruthy()
      expect(endsChip(sheet, '20 Jan 2027')).toBeTruthy()
      expect(within(sheet).getByText('Every week on Tuesday')).toBeTruthy()
    })

    it('fills in a EUR income with cents and keeps the note chip closed when the income has no name', async () => {
      await renderIncomeSheet({ recurringIncome: untitledRecurringIncome })

      const sheet = await findIncomeEditSheet()

      expect(amountField().value).toBe('40')
      expect(shownCurrency(sheet, 'EUR')).toBeTruthy()
      expect(noteChip(sheet)).toBeTruthy()
      expect(startChip(sheet, '15 Jan')).toBeTruthy()
      expect(endsChip(sheet, '15 Oct')).toBeTruthy()
    })

    it('uses the currency of the income, not the remembered one', async () => {
      window.localStorage.setItem(`kvit.lastEntryCurrency.${testMe.id}`, 'EUR')

      await renderIncomeSheet({ recurringIncome: salary })

      expect(shownCurrency(await findIncomeEditSheet(), 'MKD')).toBeTruthy()
    })

    it('offers a red Stop 44 px tall and no Delete', async () => {
      const { sheet } = await openSalary()

      expect(Array.from(stopButton(sheet).classList)).toEqual(expect.arrayContaining(['text-danger', 'min-h-13']))
      expect(within(sheet).queryByRole('button', { name: translated('en', 'expense.delete') })).toBeNull()
    })

    it('keeps the title, Save and Stop in view while the amount, chips and note scroll on a short window', async () => {
      const { sheet } = await openSalary()

      expectTitleOutsideScrollingBody(sheet, translated('en', 'budget.income.editTitle'))
      expectSaveOutsideScrollingFields(sheet, amountField(), saveButton())
      expectSaveOutsideScrollingFields(sheet, amountField(), stopButton(sheet))
    })

    it('swaps Monthly and Weekly with the Repeats chip and never offers Never', async () => {
      const { sheet } = await openSalary()

      tapRepeats(sheet, 1)
      expect(repeatsChipIn(sheet, 'Weekly')).toBeTruthy()
      tapRepeats(sheet, 1)
      expect(repeatsChipIn(sheet, 'Monthly')).toBeTruthy()
      expect(within(sheet).queryByRole('button', { name: translated('en', 'budget.income.repeatsNever') })).toBeNull()
    })

    it('shows the hint of the new rhythm after the Repeats chip was tapped', async () => {
      const { sheet } = await openSalary()

      tapRepeats(sheet, 1)

      expect(within(sheet).getByText('Every week on Saturday')).toBeTruthy()
      expect(within(sheet).queryByText('Every month on the 1st')).toBeNull()
    })

    it('limits the start date box from the oldest Budget month to one year after today', async () => {
      const { sheet } = await openSalary()

      fireEvent.click(anyStartChip(sheet))
      const dateSheet = await findDateSheet()

      expect(dateField(dateSheet).min).toBe('2024-11-01')
      expect(dateField(dateSheet).max).toBe('2027-10-20')
    })
  })

  describe('saving', () => {
    it('sends PUT with the fields of the income, no request id and no entry', async () => {
      const { fetchMock } = await openSalary()

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', salaryUrl)).toHaveLength(1)
      })
      const body = sentBody(fetchMock, 'PUT', salaryUrl)
      expect(body).toEqual({
        amountMinor: 3000000,
        currency: 'MKD',
        title: 'Salary',
        frequency: 'Monthly',
        dayOfMonth: 1,
        dayOfWeek: null,
        startsOn: '2026-08-01',
        endsOn: null,
      })
      expect(Object.keys(body)).not.toContain('clientRequestId')
      expect(requestCount(fetchMock, 'POST', '/api/personal/recurring-incomes')).toBe(0)
      expect(requestCount(fetchMock, 'POST', '/api/personal/entries')).toBe(0)
    })

    it('sends the PUT, then the catch-up, and nothing else', async () => {
      const { fetchMock } = await openSalary()

      fireEvent.click(saveButton())

      await waitForEveryDialogToClose()
      expect(sentWritesToRecurringIncomes(fetchMock)).toEqual(putAndCatchUp)
    })

    it('sends the changed amount', async () => {
      const { fetchMock } = await openSalary()

      typeAmount('32000')
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', salaryUrl)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'PUT', salaryUrl)).toMatchObject({ amountMinor: 3200000 })
    })

    it('sends Weekly with the weekday of the start and no day of the month after the Repeats chip was tapped', async () => {
      const { fetchMock, sheet } = await openSalary()

      tapRepeats(sheet, 1)
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', salaryUrl)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'PUT', salaryUrl)).toMatchObject({
        frequency: 'Weekly',
        dayOfMonth: null,
        dayOfWeek: 6,
        startsOn: '2026-08-01',
      })
    })

    it('sends Monthly with the day of the month after one tap on a weekly income', async () => {
      const { fetchMock } = await renderIncomeSheet({
        recurringIncome: tutoringRecurringIncome,
        answers: { [recurringIncomePut(tutoringRecurringIncome.id)]: jsonAnswer(recurringIncomeAnswerRow(tutoringRecurringIncome)), [catchUpPost]: catchUpAnswer(0) },
      })
      const sheet = await findIncomeEditSheet()

      tapRepeats(sheet, 1)
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', recurringIncomeUrl(tutoringRecurringIncome.id))).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'PUT', recurringIncomeUrl(tutoringRecurringIncome.id))).toMatchObject({
        frequency: 'Monthly',
        dayOfMonth: 1,
        dayOfWeek: null,
      })
    })

    it('sends a chosen end counted from the start of the income', async () => {
      const { fetchMock, sheet } = await openSalary()

      await chooseEnds(sheet, 'endsIn3Months')
      expect(endsChip(sheet, '31 Oct')).toBeTruthy()
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', salaryUrl)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'PUT', salaryUrl)).toMatchObject({ endsOn: '2026-10-31' })
    })

    it('sends no end after Never was chosen on an income that had an end', async () => {
      const { fetchMock } = await renderIncomeSheet({
        recurringIncome: tutoringRecurringIncome,
        answers: { [recurringIncomePut(tutoringRecurringIncome.id)]: jsonAnswer(recurringIncomeAnswerRow(tutoringRecurringIncome)), [catchUpPost]: catchUpAnswer(0) },
      })
      const sheet = await findIncomeEditSheet()

      await chooseEnds(sheet, 'endsOptionNever')
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', recurringIncomeUrl(tutoringRecurringIncome.id))).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'PUT', recurringIncomeUrl(tutoringRecurringIncome.id))).toMatchObject({ endsOn: null })
    })

    it('sends the start of Yesterday when it is changed', async () => {
      const { fetchMock, sheet } = await openSalary()

      fireEvent.click(anyStartChip(sheet))
      fireEvent.click(yesterdayChip(await findDateSheet()))
      await waitFor(() => {
        expect(screen.queryByRole('dialog', { name: translated('en', 'expense.pickDate') })).toBeNull()
      })
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', salaryUrl)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'PUT', salaryUrl)).toMatchObject({ startsOn: '2026-10-19', dayOfMonth: 19 })
    })

    it('sends no title when the name was emptied', async () => {
      const { fetchMock, sheet } = await openSalary()

      fireEvent.change(within(sheet).getByLabelText(translated('en', 'expense.note')), { target: { value: '' } })
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', salaryUrl)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'PUT', salaryUrl)).toMatchObject({ title: null })
    })

    it('keeps a start older than the Budget strip when it is saved without changing it', async () => {
      const oldIncome = { ...salary, startsOn: '2023-05-04', dayOfMonth: 4 }
      const { fetchMock } = await renderIncomeSheet({
        recurringIncome: oldIncome,
        answers: { [recurringIncomePut(salary.id)]: jsonAnswer(recurringIncomeAnswerRow(oldIncome)), [catchUpPost]: catchUpAnswer(0) },
      })
      await findIncomeEditSheet()

      typeAmount('32000')
      fireEvent.click(saveButton())

      await waitForEveryDialogToClose()
      expect(sentBody(fetchMock, 'PUT', salaryUrl)).toMatchObject({ startsOn: '2023-05-04', dayOfMonth: 4 })
    })

    it('closes, says Saved, refreshes the entries and remembers the currency after the catch-up', async () => {
      const { onClose, queryClient } = await renderIncomeSheet({
        recurringIncome: flatRentRecurringIncome,
        answers: {
          [recurringIncomePut(flatRentRecurringIncome.id)]: jsonAnswer(recurringIncomeAnswerRow(flatRentRecurringIncome)),
          [catchUpPost]: catchUpAnswer(1),
        },
      })
      await findIncomeEditSheet()

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(onClose).toHaveBeenCalledOnce()
      })
      expect(vi.mocked(toast.success)).toHaveBeenCalledWith(translated('en', 'groupSettings.saved'))
      expect(isEntriesCacheRefreshed(queryClient)).toBe(true)
      expect(window.localStorage.getItem(`kvit.lastEntryCurrency.${testMe.id}`)).toBe('EUR')
    })

    it('stays open until the catch-up has answered', async () => {
      const { fetchMock, onClose } = await openSalary({ ...saveAnswers, [catchUpPost]: neverAnswer() })

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'POST', catchUpUrl)).toHaveLength(1)
      })
      expect(onClose).not.toHaveBeenCalled()
    })

    it('still closes with Saved when the catch-up fails after the change was saved, and logs the failure', async () => {
      const logged = vi.spyOn(console, 'error').mockImplementation(() => undefined)
      const { onClose } = await openSalary({ ...saveAnswers, [catchUpPost]: networkFailureAnswer() })

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(onClose).toHaveBeenCalledOnce()
      })
      expect(vi.mocked(toast.success)).toHaveBeenCalledWith(translated('en', 'groupSettings.saved'))
      expect(screen.queryByRole('alert')).toBeNull()
      expect(logged).toHaveBeenCalled()
    })

    it('says Saved first and then shows an error toast when the catch-up fails after the change was saved', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => undefined)
      const { onClose } = await openSalary({ ...saveAnswers, [catchUpPost]: networkFailureAnswer() })

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(vi.mocked(toast.error)).toHaveBeenCalledWith(translated('en', 'budget.recurring.catchUpFailed'))
      })
      expect(onClose).toHaveBeenCalledOnce()
      expect(vi.mocked(toast.error)).toHaveBeenCalledOnce()
      expect(vi.mocked(toast.success).mock.invocationCallOrder[0]).toBeLessThan(vi.mocked(toast.error).mock.invocationCallOrder[0])
    })

    it('sends one PUT and one catch-up when Save is tapped twice quickly', async () => {
      const { fetchMock } = await openSalary()

      fireEvent.click(saveButton())
      fireEvent.click(saveButton())

      await waitForEveryDialogToClose()
      expect(sentWritesToRecurringIncomes(fetchMock)).toEqual(putAndCatchUp)
    })

    it('keeps Save disabled while the amount is not a positive amount', async () => {
      await openSalary()

      typeAmount('0')

      expect(isDisabled(saveButton())).toBe(true)
    })
  })

  describe('refused saves', () => {
    it.each(refusalCases)('shows the translated message for $errorCode above Save', async ({ errorCode, status }) => {
      await saveRefusedEdit(errorCode, status)

      const alert = screen.getByRole('alert')
      expect(alert.textContent).toBe(translated('en', `errors.${errorCode}`))
      expect(alert.compareDocumentPosition(saveButton()) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
    })

    it.each(refusalCases)('puts the red edge on the field $errorCode is about ($field) and on no other', async ({ errorCode, status, field }) => {
      const { sheet } = await saveRefusedEdit(errorCode, status)

      expect(refusedFields(sheet)).toEqual(field === null ? [] : [field])
    })

    it.each(refusalCases)('keeps the sheet open and sends no catch-up after $errorCode', async ({ errorCode, status }) => {
      const { onClose, fetchMock } = await saveRefusedEdit(errorCode, status)

      expect(onClose).not.toHaveBeenCalled()
      expect(sentWritesToRecurringIncomes(fetchMock)).toEqual([`PUT ${salaryUrl}`])
    })

    it('takes the red edge off the start once another start is picked', async () => {
      const { sheet } = await saveRefusedEdit('RECURRING_INCOME_START_CANNOT_CHANGE', 400)

      fireEvent.click(anyStartChip(sheet))
      fireEvent.click(yesterdayChip(await findDateSheet()))
      await waitFor(() => {
        expect(screen.queryByRole('dialog', { name: translated('en', 'expense.pickDate') })).toBeNull()
      })

      expect(anyStartChip(sheet).getAttribute('aria-invalid')).not.toBe('true')
    })

    it('takes the red edge off the amount once the amount is changed', async () => {
      await saveRefusedEdit('RECURRING_INCOME_AMOUNT_TOO_LARGE', 400)

      typeAmount('130')

      expect(amountField().getAttribute('aria-invalid')).not.toBe('true')
    })

    it('closes the sheet, shows the error as a toast and refreshes the entries when the income no longer exists', async () => {
      const { onClose, queryClient, fetchMock } = await openSalary({
        ...saveAnswers,
        [recurringIncomePut(salary.id)]: problemAnswer(404, 'RECURRING_INCOME_NOT_FOUND'),
      })

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(onClose).toHaveBeenCalledOnce()
      })
      expect(vi.mocked(toast.error)).toHaveBeenCalledWith(translated('en', 'errors.RECURRING_INCOME_NOT_FOUND'))
      expect(isEntriesCacheRefreshed(queryClient)).toBe(true)
      expect(screen.queryByRole('alert')).toBeNull()
      expect(sentWritesToRecurringIncomes(fetchMock)).toEqual([`PUT ${salaryUrl}`])
    })
  })

  describe('stopping', () => {
    it.each(languages)('writes Stop in %s and shows no Delete', async (language) => {
      await renderIncomeSheet({ recurringIncome: salary, language, answers: saveAnswers })

      const sheet = await findIncomeEditSheet(language)

      expect(stopButton(sheet, language)).toBeTruthy()
      expect(within(sheet).queryByRole('button', { name: translated(language, 'expense.delete') })).toBeNull()
    })

    it('stops at once with one tap, with no question, and closes the sheet', async () => {
      const { fetchMock, onClose, sheet } = await openSalary(stopAnswers)

      fireEvent.click(stopButton(sheet))

      await waitFor(() => {
        expect(onClose).toHaveBeenCalledOnce()
      })
      expect(requestCount(fetchMock, 'POST', `${salaryUrl}/stop`)).toBe(1)
      expect(sentWritesToRecurringIncomes(fetchMock)).toEqual([`POST ${salaryUrl}/stop`])
      expect(screen.queryByRole('dialog')).toBeNull()
    })

    it.each(languages)('shows a "Stopped" toast with Undo (%s)', async (language) => {
      await renderIncomeSheet({ recurringIncome: salary, language, answers: stopAnswers })
      const sheet = await findIncomeEditSheet(language)

      fireEvent.click(stopButton(sheet, language))

      await waitFor(() => {
        expect(toastShownWithText(translated(language, 'budget.recurring.stopped'))).toBeTruthy()
      })
      expect(toastShownWithText(translated(language, 'budget.recurring.stopped')).options.action).toMatchObject({
        label: translated(language, 'common.undo'),
      })
    })

    it('starts it again with Undo, makes the entries that are due, and says Restored', async () => {
      const { fetchMock, sheet } = await openSalary(stopAnswers)
      fireEvent.click(stopButton(sheet))
      await waitForEveryDialogToClose()

      pressToastAction(toastShownWithText(translated('en', 'budget.recurring.stopped')))

      await waitFor(() => {
        expect(vi.mocked(toast.success)).toHaveBeenCalledWith(translated('en', 'budget.entries.restored'))
      })
      expect(sentWritesToRecurringIncomes(fetchMock)).toEqual([`POST ${salaryUrl}/stop`, `POST ${salaryUrl}/restart`, `POST ${catchUpUrl}`])
    })

    it.each([
      ['RECURRING_INCOME_RESTART_EXPIRED', 400],
      ['RECURRING_INCOME_NOT_STOPPED', 400],
      ['RECURRING_INCOME_NOT_FOUND', 404],
    ])('shows the translated error when Undo is refused with %s', async (errorCode, status) => {
      const { sheet } = await openSalary({ ...stopAnswers, [recurringIncomeRestartPost(salary.id)]: problemAnswer(status, errorCode) })
      fireEvent.click(stopButton(sheet))
      await waitForEveryDialogToClose()

      pressToastAction(toastShownWithText(translated('en', 'budget.recurring.stopped')))

      await waitFor(() => {
        expect(vi.mocked(toast.error)).toHaveBeenCalledWith(translated('en', `errors.${errorCode}`))
      })
    })

    it('sends one stop when Stop is tapped twice quickly', async () => {
      const { fetchMock, sheet } = await openSalary(stopAnswers)

      fireEvent.click(stopButton(sheet))
      fireEvent.click(stopButton(sheet))

      await waitForEveryDialogToClose()
      expect(requestCount(fetchMock, 'POST', `${salaryUrl}/stop`)).toBe(1)
    })

    it('closes the sheet, shows the error as a toast and refreshes the entries when the income no longer exists', async () => {
      const { onClose, queryClient, sheet } = await openSalary({
        ...stopAnswers,
        [recurringIncomeStopPost(salary.id)]: problemAnswer(404, 'RECURRING_INCOME_NOT_FOUND'),
      })

      fireEvent.click(stopButton(sheet))

      await waitFor(() => {
        expect(onClose).toHaveBeenCalledOnce()
      })
      expect(vi.mocked(toast.error)).toHaveBeenCalledWith(translated('en', 'errors.RECURRING_INCOME_NOT_FOUND'))
      expect(isEntriesCacheRefreshed(queryClient)).toBe(true)
    })

    it('keeps the sheet open and shows the error as a toast when the stop fails for another reason', async () => {
      const { onClose, sheet } = await openSalary({
        ...stopAnswers,
        [recurringIncomeStopPost(salary.id)]: problemAnswer(403, 'GUEST_NOT_ALLOWED'),
      })

      fireEvent.click(stopButton(sheet))

      await waitFor(() => {
        expect(vi.mocked(toast.error)).toHaveBeenCalledWith(translated('en', 'errors.GUEST_NOT_ALLOWED'))
      })
      expect(onClose).not.toHaveBeenCalled()
      expect(screen.getByRole('dialog')).toBeTruthy()
    })
  })

  describe('one action at a time', () => {
    it('disables Stop, the chips and Save while a save is on its way, and sends no stop', async () => {
      const { fetchMock, sheet } = await openSalary({ ...saveAnswers, [recurringIncomePut(salary.id)]: neverAnswer(), ...stopAnswers })

      fireEvent.click(saveButton())
      await waitFor(() => {
        expect(isDisabled(stopButton(sheet))).toBe(true)
      })
      fireEvent.click(stopButton(sheet))

      expect(isDisabled(saveButton())).toBe(true)
      expect(isDisabled(anyStartChip(sheet))).toBe(true)
      expect(requestCount(fetchMock, 'POST', `${salaryUrl}/stop`)).toBe(0)
    })

    it('disables Save and the chips while a stop is on its way, and sends no PUT', async () => {
      const { fetchMock, sheet } = await openSalary({ ...saveAnswers, ...stopAnswers, [recurringIncomeStopPost(salary.id)]: neverAnswer() })

      fireEvent.click(stopButton(sheet))
      await waitFor(() => {
        expect(isDisabled(saveButton())).toBe(true)
      })

      expect(isDisabled(anyStartChip(sheet))).toBe(true)
      expect(requestCount(fetchMock, 'PUT', salaryUrl)).toBe(0)
    })
  })
})
