import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { kvitGlassPanelLook } from '@/shared/components/kvitGlassLooks'
import {
  addDays,
  amountField,
  dateField,
  doneButton,
  invalidAmountTextCases,
  isDisabled,
  saveButton,
  sentBody,
  todayChip,
  typeAmount,
  typeDate,
  yesterdayChip,
} from '@/test/expenseFormTestHelpers'
import { entryAnswerRow, salaryIncomeEntry } from '@/test/personalEntryTestData'
import {
  dateChip,
  isEntriesCacheRefreshed,
  noteChip,
  personalEntriesPost,
  repeatsChips,
  shownCurrency,
  waitForEveryDialogToClose,
} from '@/test/personalEntryTestHelpers'
import { findIncomeSheet, renderIncomeSheet } from '@/test/personalIncomeTestHelpers'
import { jsonAnswer, networkFailureAnswer, requestCount, requestsOf } from '@/test/requestTestHelpers'
import { expectSaveOutsideScrollingFields, expectTitleOutsideScrollingBody } from '@/test/sheetScrollTestHelpers'
import { testMe } from '@/test/testMe'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/
const savedAnswer = { [personalEntriesPost]: jsonAnswer(entryAnswerRow(salaryIncomeEntry)) }
const sentPosts = (fetchMock: Parameters<typeof requestsOf>[0]) => requestsOf(fetchMock, 'POST', '/api/personal/entries')
const lastCurrencyKey = `kvit.lastEntryCurrency.${testMe.id}`

describe('PersonalIncomeSheet, adding', () => {
  beforeEach(() => {
    window.localStorage.clear()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.clearAllMocks()
    vi.restoreAllMocks()
  })

  describe('opening', () => {
    it.each(languages)('opens straight on the amount in a glass sheet titled "New income" (%s)', async (language) => {
      await renderIncomeSheet({ language })

      const sheet = await findIncomeSheet(language)

      expect(sheet.className.split(/\s+/)).toEqual(expect.arrayContaining(kvitGlassPanelLook.split(/\s+/)))
      expect(within(sheet).getByRole('heading', { name: translated(language, 'budget.income.title') })).toBeTruthy()
      expect(amountField(language)).toBeTruthy()
    })

    it('focuses the amount and opens the number keypad', async () => {
      await renderIncomeSheet()
      await findIncomeSheet()

      await waitFor(() => {
        expect(document.activeElement).toBe(amountField())
      })
      expect(amountField().inputMode).toBe('numeric')
    })

    it('shows no category grid and no category chip, but one Repeats chip saying Never', async () => {
      await renderIncomeSheet()

      const sheet = await findIncomeSheet()

      expect(screen.queryByRole('dialog', { name: translated('en', 'budget.entries.pickCategory') })).toBeNull()
      expect(within(sheet).queryByRole('button', { name: translated('en', 'expense.category') })).toBeNull()
      expect(repeatsChips(sheet)).toHaveLength(1)
      expect(within(sheet).getByRole('button', { name: translated('en', 'budget.income.repeatsNever') })).toBe(repeatsChips(sheet)[0])
      expect(within(sheet).queryByRole('heading', { name: 'Coffee' })).toBeNull()
    })

    it('never asks for the categories', async () => {
      const { fetchMock } = await renderIncomeSheet()
      await findIncomeSheet()

      expect(requestCount(fetchMock, 'GET', '/api/personal/categories')).toBe(0)
    })

    it('shows no Delete while adding', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()

      expect(within(sheet).queryByRole('button', { name: translated('en', 'expense.delete') })).toBeNull()
    })

    it('keeps the title and Save in view while the amount, chips and note scroll on a short window', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()

      expectTitleOutsideScrollingBody(sheet, translated('en', 'budget.income.title'))
      expectSaveOutsideScrollingFields(sheet, amountField(), saveButton())
    })
  })

  describe('saving with the fewest taps', () => {
    it('saves with the typed amount and Save: 1 tap and 1 field', async () => {
      const { fetchMock } = await renderIncomeSheet({ answers: savedAnswer })
      let taps = 0

      await findIncomeSheet()
      typeAmount('65000')
      fireEvent.click(saveButton())
      taps += 1

      await waitFor(() => {
        expect(sentPosts(fetchMock)).toHaveLength(1)
      })
      expect(taps).toBe(1)
    })

    it('sends a request id, kind Income, no categoryId, the amount in deni, MKD, today and no note', async () => {
      vi.useFakeTimers({ toFake: ['Date'] })
      vi.setSystemTime(new Date('2026-10-19T10:00:00Z'))
      const { fetchMock } = await renderIncomeSheet({ answers: savedAnswer })
      await findIncomeSheet()

      typeAmount('65000')
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(sentPosts(fetchMock)).toHaveLength(1)
      })
      const body = sentBody(fetchMock, 'POST', '/api/personal/entries')
      expect(body).toEqual({
        clientRequestId: expect.stringMatching(uuidPattern),
        kind: 'Income',
        amountMinor: 6500000,
        currency: 'MKD',
        entryDate: '2026-10-19',
        note: null,
      })
      expect(Object.keys(body)).not.toContain('categoryId')
    })

    it('closes, shows "Saved" and refreshes the entries after a save', async () => {
      const { onClose, queryClient } = await renderIncomeSheet({ answers: savedAnswer })
      await findIncomeSheet()

      typeAmount('65000')
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(onClose).toHaveBeenCalledOnce()
      })
      await waitForEveryDialogToClose()
      expect(vi.mocked(toast.success)).toHaveBeenCalledWith(translated('en', 'groupSettings.saved'))
      expect(isEntriesCacheRefreshed(queryClient)).toBe(true)
    })

    it('keeps Save disabled until the amount is a positive amount', async () => {
      await renderIncomeSheet()
      await findIncomeSheet()

      expect(isDisabled(saveButton())).toBe(true)
      typeAmount('0')
      expect(isDisabled(saveButton())).toBe(true)
      typeAmount('5')
      expect(isDisabled(saveButton())).toBe(false)
    })

    it('sends one request when Save is tapped twice quickly', async () => {
      const { fetchMock } = await renderIncomeSheet({ answers: savedAnswer })
      await findIncomeSheet()
      typeAmount('65000')

      fireEvent.click(saveButton())
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(sentPosts(fetchMock)).toHaveLength(1)
      })
      await waitForEveryDialogToClose()
      expect(sentPosts(fetchMock)).toHaveLength(1)
    })

    it('sends one request when Enter is pressed twice in the amount', async () => {
      const { fetchMock } = await renderIncomeSheet({ answers: savedAnswer })
      await findIncomeSheet()
      typeAmount('65000')

      const form = amountField().closest('form')
      if (form === null) {
        throw new Error('The amount is not inside a form')
      }
      fireEvent.submit(form)
      fireEvent.submit(form)

      await waitForEveryDialogToClose()
      expect(sentPosts(fetchMock)).toHaveLength(1)
    })

    it('sends the same request id again when Save is tapped after a network failure, so a retry cannot save twice', async () => {
      let answer = networkFailureAnswer()
      const { fetchMock } = await renderIncomeSheet({ answers: { [personalEntriesPost]: () => answer() } })
      await findIncomeSheet()
      typeAmount('65000')
      fireEvent.click(saveButton())
      await screen.findByRole('alert')

      answer = jsonAnswer(entryAnswerRow(salaryIncomeEntry))
      fireEvent.click(saveButton())

      await waitForEveryDialogToClose()
      const [first, second] = sentPosts(fetchMock).map((request) => (request.body as { clientRequestId: string }).clientRequestId)
      expect(second).toBe(first)
    })

    it('makes a new request id for every time the sheet opens', async () => {
      const first = await renderIncomeSheet({ answers: savedAnswer })
      await findIncomeSheet()
      typeAmount('65000')
      fireEvent.click(saveButton())
      await waitForEveryDialogToClose()
      const firstId = (sentPosts(first.fetchMock)[0].body as { clientRequestId: string }).clientRequestId

      const second = await renderIncomeSheet({ answers: savedAnswer })
      await findIncomeSheet()
      typeAmount('70000')
      fireEvent.click(saveButton())
      await waitForEveryDialogToClose()

      expect((sentPosts(second.fetchMock)[0].body as { clientRequestId: string }).clientRequestId).not.toBe(firstId)
    })
  })

  describe('the amount and the currency', () => {
    it.each([
      ['1200', 120000],
      [' 75 ', 7500],
    ])('turns "%s" denars into %i deni', async (text, amountMinor) => {
      const { fetchMock } = await renderIncomeSheet({ answers: savedAnswer })
      await findIncomeSheet()

      typeAmount(text)
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(sentPosts(fetchMock)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'POST', '/api/personal/entries')).toMatchObject({ amountMinor, currency: 'MKD' })
    })

    it.each([
      ['12.5', 1250],
      ['12,50', 1250],
      ['12', 1200],
    ])('turns "%s" euros into %i cents after switching to EUR', async (text, amountMinor) => {
      const { fetchMock } = await renderIncomeSheet({ answers: savedAnswer })
      const sheet = await findIncomeSheet()

      fireEvent.click(shownCurrency(sheet, 'MKD'))
      typeAmount(text)
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(sentPosts(fetchMock)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'POST', '/api/personal/entries')).toMatchObject({ amountMinor, currency: 'EUR' })
    })

    it('opens the decimal keypad in EUR', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()

      fireEvent.click(shownCurrency(sheet, 'MKD'))

      expect(amountField().inputMode).toBe('decimal')
    })

    it.each(invalidAmountTextCases)('refuses $name with a red edge and Save disabled', async ({ currency, text }) => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()
      if (currency === 'EUR') {
        fireEvent.click(shownCurrency(sheet, 'MKD'))
      }

      typeAmount(text)

      expect(isDisabled(saveButton())).toBe(true)
      expect(amountField().getAttribute('aria-invalid')).toBe('true')
    })

    it('uses a big amount font, so phones do not zoom the page', async () => {
      await renderIncomeSheet()
      await findIncomeSheet()

      expect(Array.from(amountField().classList)).toContain('text-5xl')
    })
  })

  describe('the default currency', () => {
    async function saveOneIncome(currencyToPick: 'MKD' | 'EUR'): Promise<void> {
      await renderIncomeSheet({ answers: savedAnswer })
      const sheet = await findIncomeSheet()
      if (currencyToPick === 'EUR') {
        fireEvent.click(shownCurrency(sheet, 'MKD'))
      }
      typeAmount('12')
      fireEvent.click(saveButton())
      await waitForEveryDialogToClose()
    }

    it('is MKD for the first entry ever', async () => {
      await renderIncomeSheet()

      expect(shownCurrency(await findIncomeSheet(), 'MKD')).toBeTruthy()
    })

    it('is the currency of the last saved entry, shared with spending', async () => {
      window.localStorage.setItem(lastCurrencyKey, 'EUR')

      await renderIncomeSheet()

      expect(shownCurrency(await findIncomeSheet(), 'EUR')).toBeTruthy()
    })

    it('is EUR on the next sheet after an income in EUR was saved, and the key is updated', async () => {
      await saveOneIncome('EUR')
      expect(window.localStorage.getItem(lastCurrencyKey)).toBe('EUR')

      await renderIncomeSheet()

      expect(shownCurrency(await findIncomeSheet(), 'EUR')).toBeTruthy()
    })

    it('goes back to MKD after the next income is saved in MKD', async () => {
      window.localStorage.setItem(lastCurrencyKey, 'EUR')
      await renderIncomeSheet({ answers: savedAnswer })
      fireEvent.click(shownCurrency(await findIncomeSheet(), 'EUR'))
      typeAmount('120')
      fireEvent.click(saveButton())
      await waitForEveryDialogToClose()

      expect(window.localStorage.getItem(lastCurrencyKey)).toBe('MKD')
    })

    it('is MKD for another person on the same browser', async () => {
      await saveOneIncome('EUR')

      await renderIncomeSheet({ me: { ...testMe, id: '9e8d7c6b-5a49-4382-9170-6f5e4d3c2b1a' } })

      expect(shownCurrency(await findIncomeSheet(), 'MKD')).toBeTruthy()
    })

    it('is not changed by a save that the server refused', async () => {
      await renderIncomeSheet({ answers: { [personalEntriesPost]: networkFailureAnswer() } })
      const sheet = await findIncomeSheet()
      fireEvent.click(shownCurrency(sheet, 'MKD'))
      typeAmount('12')
      fireEvent.click(saveButton())
      await screen.findByRole('alert')

      expect(window.localStorage.length).toBe(0)
    })
  })

  describe('the date', () => {
    it.each([
      ['Europe/Skopje', '2026-10-20'],
      ['America/New_York', '2026-10-19'],
    ])('is today in the time zone of the person (%s), not of the device', async (timeZone, today) => {
      vi.useFakeTimers({ toFake: ['Date'] })
      vi.setSystemTime(new Date('2026-10-19T22:30:00Z'))
      const { fetchMock } = await renderIncomeSheet({ me: { ...testMe, timeZone }, answers: savedAnswer })
      const sheet = await findIncomeSheet()

      expect(dateChip(sheet, translated('en', 'expenses.today'))).toBeTruthy()
      typeAmount('65000')
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(sentPosts(fetchMock)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'POST', '/api/personal/entries')).toMatchObject({ entryDate: today })
    })

    it('changes to Yesterday through the date sheet and sends that day', async () => {
      vi.useFakeTimers({ toFake: ['Date'] })
      vi.setSystemTime(new Date('2026-10-19T10:00:00Z'))
      const { fetchMock } = await renderIncomeSheet({ answers: savedAnswer })
      const sheet = await findIncomeSheet()
      typeAmount('65000')

      fireEvent.click(dateChip(sheet, translated('en', 'expenses.today')))
      const dateSheet = await screen.findByRole('dialog', { name: translated('en', 'expense.pickDate') })
      expect(todayChip(dateSheet)).toBeTruthy()
      fireEvent.click(yesterdayChip(dateSheet))
      await waitFor(() => {
        expect(screen.queryByRole('dialog', { name: translated('en', 'expense.pickDate') })).toBeNull()
      })

      expect(dateChip(sheet, translated('en', 'expenses.yesterday'))).toBeTruthy()
      expect(amountField().value).toBe('65000')
      fireEvent.click(saveButton())
      await waitFor(() => {
        expect(sentPosts(fetchMock)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'POST', '/api/personal/entries')).toMatchObject({ entryDate: addDays('2026-10-19', -1) })
    })

    it.each([
      ['Europe/Skopje', '2026-10-19T10:00:00Z', '2024-11-01', '2026-10-19'],
      ['Europe/Skopje', '2026-10-31T23:30:00Z', '2024-12-01', '2026-11-01'],
      ['America/New_York', '2026-11-01T02:00:00Z', '2024-11-01', '2026-10-31'],
    ])(
      'limits the date box to the 24 months of the Budget strip up to today (%s at %s)',
      async (timeZone, now, earliest, today) => {
        vi.useFakeTimers({ toFake: ['Date'] })
        vi.setSystemTime(new Date(now))
        await renderIncomeSheet({ me: { ...testMe, timeZone } })
        const sheet = await findIncomeSheet()

        fireEvent.click(dateChip(sheet, translated('en', 'expenses.today')))
        const dateSheet = await screen.findByRole('dialog', { name: translated('en', 'expense.pickDate') })

        expect(dateField(dateSheet).min).toBe(earliest)
        expect(dateField(dateSheet).max).toBe(today)
      },
    )

    it.each(languages)('shows the hint for the last 24 months up to today, not the expense hint (%s)', async (language) => {
      await renderIncomeSheet({ language })
      const sheet = await findIncomeSheet(language)

      fireEvent.click(dateChip(sheet, translated(language, 'expenses.today')))
      const dateSheet = await screen.findByRole('dialog', { name: translated(language, 'expense.pickDate') })

      expect(within(dateSheet).getByText(translated(language, 'budget.entries.dateHint'))).toBeTruthy()
      expect(within(dateSheet).queryByText(translated(language, 'expense.dateHint'))).toBeNull()
    })

    it.each([
      ['2024-10-31', false],
      ['2024-11-01', true],
      ['2026-10-19', true],
      ['2026-10-20', false],
    ])('lets Done apply the typed date %s only inside that range: %s', async (typedDate, canApply) => {
      vi.useFakeTimers({ toFake: ['Date'] })
      vi.setSystemTime(new Date('2026-10-19T10:00:00Z'))
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()

      fireEvent.click(dateChip(sheet, translated('en', 'expenses.today')))
      const dateSheet = await screen.findByRole('dialog', { name: translated('en', 'expense.pickDate') })
      typeDate(dateSheet, typedDate)

      expect(isDisabled(doneButton(dateSheet))).toBe(!canApply)
    })

    it('gives the date chip a 44 px tap height', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()

      expect(Array.from(dateChip(sheet, translated('en', 'expenses.today')).classList)).toContain('min-h-11')
    })
  })

  describe('the note', () => {
    it('opens a note field with one tap, puts the cursor in it and sends the trimmed note', async () => {
      const { fetchMock } = await renderIncomeSheet({ answers: savedAnswer })
      const sheet = await findIncomeSheet()
      typeAmount('65000')

      fireEvent.click(noteChip(sheet))
      const note = within(sheet).getByLabelText<HTMLTextAreaElement>(translated('en', 'expense.note'))
      await waitFor(() => {
        expect(document.activeElement).toBe(note)
      })
      fireEvent.change(note, { target: { value: '  Salary  ' } })
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(sentPosts(fetchMock)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'POST', '/api/personal/entries')).toMatchObject({ note: 'Salary' })
    })

    it('sends a note of only spaces as no note', async () => {
      const { fetchMock } = await renderIncomeSheet({ answers: savedAnswer })
      const sheet = await findIncomeSheet()
      typeAmount('65000')

      fireEvent.click(noteChip(sheet))
      fireEvent.change(within(sheet).getByLabelText(translated('en', 'expense.note')), { target: { value: '   ' } })
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(sentPosts(fetchMock)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'POST', '/api/personal/entries')).toMatchObject({ note: null })
    })

    it('uses a note font of at least 16 px, so phones do not zoom the page', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()

      fireEvent.click(noteChip(sheet))

      expect(Array.from(within(sheet).getByLabelText(translated('en', 'expense.note')).classList)).toContain('text-[1.0625rem]')
    })
  })

  describe('the look', () => {
    it('draws the chips with the app colour tokens, so light and dark both follow the theme', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()

      for (const chip of [dateChip(sheet, translated('en', 'expenses.today')), noteChip(sheet)]) {
        expect(Array.from(chip.classList)).toEqual(expect.arrayContaining(['bg-field', 'border-field-border', 'text-field-foreground', 'min-h-11']))
      }
    })

    it.each(languages)('writes Save and the chips in %s', async (language) => {
      await renderIncomeSheet({ language })
      const sheet = await findIncomeSheet(language)

      expect(within(sheet).getByRole('button', { name: translated(language, 'groupSettings.save') })).toBeTruthy()
      expect(dateChip(sheet, translated(language, 'expenses.today'))).toBeTruthy()
      expect(noteChip(sheet, language)).toBeTruthy()
      expect(shownCurrency(sheet, 'MKD', language)).toBeTruthy()
    })
  })
})
