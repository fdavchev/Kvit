import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { kvitGlassPanelLook } from '@/shared/components/kvitGlassLooks'
import {
  amountField,
  dateField,
  dismissSheetByTappingBackdrop,
  doneButton,
  isDisabled,
  todayChip,
  typeAmount,
  typeDate,
  yesterdayChip,
} from '@/test/expenseFormTestHelpers'
import { isBefore } from '@/test/expenseTestHelpers'
import { coffeeCategory } from '@/test/personalCategoryTestData'
import { dateChip, noteChip, pickCategory, renderSpendingSheet, repeatsChips } from '@/test/personalEntryTestHelpers'
import { findIncomeSheet, renderIncomeSheet } from '@/test/personalIncomeTestHelpers'
import {
  anyStartChip,
  chooseEnds,
  endsChip,
  endsOption,
  findDateSheet,
  openEndsBox,
  queryAnyEndsChip,
  repeatsChip,
  repeatsChipIn,
  sheetNow,
  startChip,
  tapRepeats,
} from '@/test/recurringIncomeTestHelpers'
import { testMe } from '@/test/testMe'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const latinLetters = /[A-Za-zÀ-ɏ]/
const today = translated('en', 'expenses.today')

async function openStartDateSheet(sheet: HTMLElement, language: 'en' | 'mk' = 'en'): Promise<HTMLElement> {
  fireEvent.click(anyStartChip(sheet, language))
  return findDateSheet(language)
}

async function pickStartDate(sheet: HTMLElement, date: string): Promise<void> {
  const dateSheet = await openStartDateSheet(sheet)
  typeDate(dateSheet, date)
  fireEvent.click(doneButton(dateSheet))
  await waitFor(() => {
    expect(screen.queryByRole('dialog', { name: translated('en', 'expense.pickDate') })).toBeNull()
  })
}

describe('PersonalIncomeSheet, the Repeats chip', () => {
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

  describe('the chip', () => {
    it('shows exactly one Repeats chip while adding, saying Never', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()

      expect(repeatsChips(sheet)).toHaveLength(1)
      expect(repeatsChipIn(sheet, 'Never')).toBe(repeatsChips(sheet)[0])
    })

    it.each(languages)('writes the Never, Monthly and Weekly labels of the chip in %s', async (language) => {
      await renderIncomeSheet({ language })
      const sheet = await findIncomeSheet(language)

      expect(repeatsChipIn(sheet, 'Never', language)).toBeTruthy()
      tapRepeats(sheet, 1, language)
      expect(repeatsChipIn(sheet, 'Monthly', language)).toBeTruthy()
      tapRepeats(sheet, 1, language)
      expect(repeatsChipIn(sheet, 'Weekly', language)).toBeTruthy()
    })

    it.each([
      [1, 'Monthly'],
      [2, 'Weekly'],
      [3, 'Never'],
      [4, 'Monthly'],
    ] as const)('says %i taps later: %s', async (taps, expected) => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()

      tapRepeats(sheet, taps)

      expect(repeatsChipIn(sheet, expected)).toBeTruthy()
    })

    it('draws the chip with the app colour tokens and a 44 px tap height like the other chips', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()

      expect(Array.from(repeatsChip(sheet).classList)).toEqual(
        expect.arrayContaining(['bg-field', 'border-field-border', 'text-field-foreground', 'min-h-11']),
      )
    })

    it('is not on the spending sheet', async () => {
      await renderSpendingSheet()
      const sheet = await pickCategory(coffeeCategory)

      expect(repeatsChips(sheet)).toHaveLength(0)
    })

    it('stays a button that is not a submit button', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()

      expect(repeatsChip(sheet).getAttribute('type')).toBe('button')
    })

    it('sits in the same row of chips as the date and the note', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()

      expect(repeatsChip(sheet).parentElement).toBe(dateChip(sheet, today).parentElement)
      expect(repeatsChip(sheet).parentElement).toBe(noteChip(sheet).parentElement)
    })

  })

  describe('while it says Never', () => {
    it('shows the date chip as Today, with no Ends chip and no rhythm hint', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()

      expect(dateChip(sheet, today)).toBeTruthy()
      expect(queryAnyEndsChip(sheet)).toBeNull()
      expect(within(sheet).queryByText(/^Every (month|week)/)).toBeNull()
      expect(within(sheet).queryByRole('button', { name: /^Starts/ })).toBeNull()
    })

    it('goes back to exactly this look after three taps', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()

      tapRepeats(sheet, 3)

      expect(dateChip(sheet, today)).toBeTruthy()
      expect(queryAnyEndsChip(sheet)).toBeNull()
      expect(within(sheet).queryByText(/^Every (month|week)/)).toBeNull()
    })
  })

  describe('the start, the Ends chip and the hint when it repeats', () => {
    it.each([1, 2])('reads the date chip as the start, "Starts: Today", after %i taps', async (taps) => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()

      tapRepeats(sheet, taps)

      expect(startChip(sheet, today)).toBeTruthy()
      expect(within(sheet).queryByRole('button', { name: today })).toBeNull()
    })

    it.each(languages)('writes the start chip in %s', async (language) => {
      await renderIncomeSheet({ language })
      const sheet = await findIncomeSheet(language)

      tapRepeats(sheet, 1, language)

      expect(startChip(sheet, translated(language, 'expenses.today'), language)).toBeTruthy()
    })

    it.each([1, 2])('shows an "Ends: never" chip after %i taps', async (taps) => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()

      tapRepeats(sheet, taps)

      expect(endsChip(sheet, null)).toBeTruthy()
    })

    it('draws the Ends chip like the other chips, 44 px tall', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()

      tapRepeats(sheet, 1)

      expect(Array.from(endsChip(sheet, null).classList)).toEqual(
        expect.arrayContaining(['bg-field', 'border-field-border', 'text-field-foreground', 'min-h-11']),
      )
    })

    it('shows "Every month on the 20th" for Monthly with the start of 20 October', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()

      tapRepeats(sheet, 1)

      expect(within(sheet).getByText('Every month on the 20th')).toBeTruthy()
    })

    it('shows "Every week on Tuesday" for Weekly with the start of 20 October 2026', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()

      tapRepeats(sheet, 2)

      expect(within(sheet).getByText('Every week on Tuesday')).toBeTruthy()
      expect(within(sheet).queryByText('Every month on the 20th')).toBeNull()
    })

    it('puts the hint under the chips', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()

      tapRepeats(sheet, 1)

      const hint = within(sheet).getByText('Every month on the 20th')
      expect(isBefore(endsChip(sheet, null), hint)).toBe(true)
      expect(isBefore(repeatsChip(sheet), hint)).toBe(true)
      expect(isBefore(startChip(sheet, today), hint)).toBe(true)
    })

    it('writes the monthly hint in Macedonian with the day and no Latin letters', async () => {
      await renderIncomeSheet({ language: 'mk' })
      const sheet = await findIncomeSheet('mk')

      tapRepeats(sheet, 1, 'mk')

      const hint = within(sheet).getByText(/20/)
      expect(hint.textContent).not.toMatch(latinLetters)
      expect(hint.textContent).not.toContain('{{')
      expect(hint.textContent).not.toBe('Every month on the 20th')
    })

    it('writes the weekly hint in Macedonian from the weekday table of the app', async () => {
      await renderIncomeSheet({ language: 'mk' })
      const sheet = await findIncomeSheet('mk')

      tapRepeats(sheet, 2, 'mk')

      expect(
        within(sheet).getByText(
          translated('mk', 'budget.recurring.weekly', { weekday: translated('mk', 'budget.recurring.weekdays.tuesday') }),
        ),
      ).toBeTruthy()
    })

    it.each([
      ['2026-10-01', '1st'],
      ['2026-10-02', '2nd'],
      ['2026-10-03', '3rd'],
      ['2026-10-04', '4th'],
      ['2026-10-11', '11th'],
      ['2026-10-12', '12th'],
      ['2026-10-13', '13th'],
      ['2026-10-21', '21st'],
      ['2026-10-22', '22nd'],
      ['2026-10-23', '23rd'],
      ['2026-10-24', '24th'],
      ['2026-10-30', '30th'],
      ['2026-10-31', '31st'],
    ])('writes the day %s as the %s in the monthly hint', async (date, ordinal) => {
      vi.setSystemTime(new Date(`${date}T10:00:00Z`))
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()

      tapRepeats(sheet, 1)

      expect(within(sheet).getByText(`Every month on the ${ordinal}`)).toBeTruthy()
    })

    it.each([
      ['2026-10-18', 'Sunday'],
      ['2026-10-19', 'Monday'],
      ['2026-10-20', 'Tuesday'],
      ['2026-10-21', 'Wednesday'],
      ['2026-10-22', 'Thursday'],
      ['2026-10-23', 'Friday'],
      ['2026-10-24', 'Saturday'],
    ])('writes %s as %s in the weekly hint', async (date, weekday) => {
      vi.setSystemTime(new Date(`${date}T10:00:00Z`))
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()

      tapRepeats(sheet, 2)

      expect(within(sheet).getByText(`Every week on ${weekday}`)).toBeTruthy()
    })

    it('takes the day of the hint from today in the time zone of the person, not of the device', async () => {
      vi.setSystemTime(new Date('2026-10-19T22:30:00Z'))
      await renderIncomeSheet({ me: { ...testMe, timeZone: 'America/New_York' } })
      const sheet = await findIncomeSheet()

      tapRepeats(sheet, 1)

      expect(within(sheet).getByText('Every month on the 19th')).toBeTruthy()
    })

    it('follows the start when it is changed to Yesterday: the 19th and Monday', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()
      tapRepeats(sheet, 1)

      const dateSheet = await openStartDateSheet(sheet)
      fireEvent.click(yesterdayChip(dateSheet))
      await waitFor(() => {
        expect(screen.queryByRole('dialog', { name: translated('en', 'expense.pickDate') })).toBeNull()
      })

      expect(startChip(sheet, translated('en', 'expenses.yesterday'))).toBeTruthy()
      expect(within(sheet).getByText('Every month on the 19th')).toBeTruthy()
      tapRepeats(sheet, 1)
      expect(within(sheet).getByText('Every week on Monday')).toBeTruthy()
    })

    it('follows a typed start in the future: the 5th of December', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()
      tapRepeats(sheet, 1)

      await pickStartDate(sheet, '2026-12-05')

      expect(startChip(sheet, '5 Dec')).toBeTruthy()
      expect(within(sheet).getByText('Every month on the 5th')).toBeTruthy()
    })

    it('keeps the typed amount and the note when the chip is tapped', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()
      typeAmount('65000')
      fireEvent.click(noteChip(sheet))
      fireEvent.change(within(sheet).getByLabelText(translated('en', 'expense.note')), { target: { value: 'Salary' } })

      tapRepeats(sheet, 1)

      expect(within(sheet).getByLabelText<HTMLTextAreaElement>(translated('en', 'expense.note')).value).toBe('Salary')
      expect(amountField().value).toBe('65000')
    })
  })

  describe('the date box of the start', () => {
    it.each([
      ['Europe/Skopje', '2026-10-20T10:00:00Z', '2024-11-01', '2027-10-20'],
      ['Europe/Skopje', '2026-10-19T22:30:00Z', '2024-11-01', '2027-10-20'],
      ['America/New_York', '2026-10-19T22:30:00Z', '2024-11-01', '2027-10-19'],
      ['Europe/Skopje', '2028-02-29T10:00:00Z', '2026-03-01', '2029-02-28'],
    ])(
      'reaches from the oldest month of the Budget strip to one year after today (%s at %s)',
      async (timeZone, now, earliest, latest) => {
        vi.setSystemTime(new Date(now))
        await renderIncomeSheet({ me: { ...testMe, timeZone } })
        const sheet = await findIncomeSheet()
        tapRepeats(sheet, 1)

        const dateSheet = await openStartDateSheet(sheet)

        expect(dateField(dateSheet).min).toBe(earliest)
        expect(dateField(dateSheet).max).toBe(latest)
      },
    )

    it('is limited to today again after the chip is tapped back to Never', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()
      tapRepeats(sheet, 3)

      fireEvent.click(dateChip(sheet, today))
      const dateSheet = await findDateSheet()

      expect(dateField(dateSheet).max).toBe('2026-10-20')
    })

    it.each(languages)('shows the hint about the start window, not the hint of income once (%s)', async (language) => {
      await renderIncomeSheet({ language })
      const sheet = await findIncomeSheet(language)
      tapRepeats(sheet, 1, language)

      const dateSheet = await openStartDateSheet(sheet, language)

      expect(within(dateSheet).getByText(translated(language, 'budget.income.startDateHint'))).toBeTruthy()
      expect(within(dateSheet).queryByText(translated(language, 'budget.entries.dateHint'))).toBeNull()
      expect(within(dateSheet).queryByText(translated(language, 'expense.dateHint'))).toBeNull()
    })

    it.each([
      ['2024-10-31', false],
      ['2024-11-01', true],
      ['2027-10-20', true],
      ['2027-10-21', false],
    ])('lets Done apply the typed start %s only inside that range: %s', async (typedDate, canApply) => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()
      tapRepeats(sheet, 1)

      const dateSheet = await openStartDateSheet(sheet)
      typeDate(dateSheet, typedDate)

      expect(isDisabled(doneButton(dateSheet))).toBe(!canApply)
    })

    it('still offers Today in the start date box', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()
      tapRepeats(sheet, 1)

      const dateSheet = await openStartDateSheet(sheet)

      expect(todayChip(dateSheet)).toBeTruthy()
    })
  })

  describe('the box "When does it end?"', () => {
    it('opens with one tap on the Ends chip, as a glass box with its title', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()
      tapRepeats(sheet, 1)

      const box = await openEndsBox(sheet)

      expect(box.className.split(/\s+/)).toEqual(expect.arrayContaining(kvitGlassPanelLook.split(/\s+/)))
      expect(within(box).getByRole('heading', { name: translated('en', 'budget.income.endsTitle') })).toBeTruthy()
    })

    it.each(languages)('offers Never, In 3 months, In 6 months, In 1 year and Pick a date in %s', async (language) => {
      await renderIncomeSheet({ language })
      const sheet = await findIncomeSheet(language)
      tapRepeats(sheet, 1, language)

      const box = await openEndsBox(sheet, language)

      expect(within(box).getAllByRole('button')).toHaveLength(5)
      for (const key of ['endsOptionNever', 'endsIn3Months', 'endsIn6Months', 'endsIn1Year', 'endsPickDate'] as const) {
        expect(endsOption(box, key, language)).toBeTruthy()
      }
    })

    it('marks Never as the chosen option when nothing was chosen yet', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()
      tapRepeats(sheet, 1)

      const box = await openEndsBox(sheet)

      expect(endsOption(box, 'endsOptionNever').getAttribute('aria-pressed')).toBe('true')
      expect(endsOption(box, 'endsIn3Months').getAttribute('aria-pressed')).not.toBe('true')
    })

    it('gives every option a tap height of at least 44 px', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()
      tapRepeats(sheet, 1)

      const box = await openEndsBox(sheet)

      for (const option of within(box).getAllByRole('button')) {
        expect(option.className.split(/\s+/)).toEqual(expect.arrayContaining([expect.stringMatching(/^min-h-(1[1-9]|20)$/)]))
      }
    })

    it.each([
      ['endsIn3Months', '20 Jan 2027'],
      ['endsIn6Months', '20 Apr 2027'],
      ['endsIn1Year', '20 Oct 2027'],
    ] as const)('closes after %s and the chip shows the date %s', async (key, day) => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()
      tapRepeats(sheet, 1)

      await chooseEnds(sheet, key)

      expect(endsChip(sheet, day)).toBeTruthy()
      expect(screen.getAllByRole('dialog')).toHaveLength(1)
    })

    it('marks the chosen option when the box is opened again', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()
      tapRepeats(sheet, 1)
      await chooseEnds(sheet, 'endsIn3Months')

      const box = await openEndsBox(sheet)

      expect(endsOption(box, 'endsIn3Months').getAttribute('aria-pressed')).toBe('true')
      expect(endsOption(box, 'endsOptionNever').getAttribute('aria-pressed')).not.toBe('true')
    })

    it('goes back to "Ends: never" when Never is chosen after a date', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()
      tapRepeats(sheet, 1)
      await chooseEnds(sheet, 'endsIn6Months')

      await chooseEnds(sheet, 'endsOptionNever')

      expect(endsChip(sheet, null)).toBeTruthy()
    })

    it('leaves the end as it was when the box is closed with Escape', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()
      tapRepeats(sheet, 1)
      await chooseEnds(sheet, 'endsIn3Months')
      const box = await openEndsBox(sheet)

      fireEvent.keyDown(box, { key: 'Escape' })

      await waitFor(() => {
        expect(screen.queryByRole('dialog', { name: translated('en', 'budget.income.endsTitle') })).toBeNull()
      })
      expect(endsChip(sheet, '20 Jan 2027')).toBeTruthy()
    })

    it('leaves the end as it was when the backdrop is tapped', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()
      tapRepeats(sheet, 1)
      await openEndsBox(sheet)

      dismissSheetByTappingBackdrop()

      await waitFor(() => {
        expect(screen.queryByRole('dialog', { name: translated('en', 'budget.income.endsTitle') })).toBeNull()
      })
      expect(endsChip(sheet, null)).toBeTruthy()
    })

    it.each([
      ['2026-12-05', 'endsIn3Months', '5 Mar 2027'],
      ['2026-12-05', 'endsIn6Months', '5 Jun 2027'],
      ['2026-12-05', 'endsIn1Year', '5 Dec 2027'],
      ['2026-08-31', 'endsIn3Months', '30 Nov'],
      ['2026-08-31', 'endsIn6Months', '28 Feb 2027'],
      ['2026-08-31', 'endsIn1Year', '31 Aug 2027'],
    ] as const)('counts from the start %s: %s gives %s', async (start, key, day) => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()
      tapRepeats(sheet, 1)
      await pickStartDate(sheet, start)

      await chooseEnds(sheet, key)

      expect(endsChip(sheet, day)).toBeTruthy()
    })
  })

  describe('picking an end date', () => {
    async function openEndDateSheet(sheet: HTMLElement): Promise<HTMLElement> {
      const box = await openEndsBox(sheet)
      fireEvent.click(endsOption(box, 'endsPickDate'))
      return findDateSheet()
    }

    it('opens the date box from "Pick a date…" and shows the hint about the end window', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()
      tapRepeats(sheet, 1)

      const dateSheet = await openEndDateSheet(sheet)

      expect(within(dateSheet).getByText(translated('en', 'budget.income.endDateHint'))).toBeTruthy()
      expect(within(dateSheet).queryByText(translated('en', 'budget.income.startDateHint'))).toBeNull()
    })

    it.each(languages)('writes the end hint in %s', async (language) => {
      await renderIncomeSheet({ language })
      const sheet = await findIncomeSheet(language)
      tapRepeats(sheet, 1, language)
      const box = await openEndsBox(sheet, language)
      fireEvent.click(endsOption(box, 'endsPickDate', language))

      const dateSheet = await findDateSheet(language)

      expect(within(dateSheet).getByText(translated(language, 'budget.income.endDateHint'))).toBeTruthy()
    })

    it('reaches from the start to ten years after the start', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()
      tapRepeats(sheet, 1)

      const dateSheet = await openEndDateSheet(sheet)

      expect(dateField(dateSheet).min).toBe('2026-10-20')
      expect(dateField(dateSheet).max).toBe('2036-10-20')
    })

    it('follows a start in the future', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()
      tapRepeats(sheet, 1)
      await pickStartDate(sheet, '2026-12-05')

      const dateSheet = await openEndDateSheet(sheet)

      expect(dateField(dateSheet).min).toBe('2026-12-05')
      expect(dateField(dateSheet).max).toBe('2036-12-05')
    })

    it.each([
      ['2026-10-19', false],
      ['2026-10-20', true],
      ['2036-10-20', true],
      ['2036-10-21', false],
    ])('lets Done apply the typed end %s only inside that range: %s', async (typedDate, canApply) => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()
      tapRepeats(sheet, 1)
      const dateSheet = await openEndDateSheet(sheet)

      typeDate(dateSheet, typedDate)

      expect(isDisabled(doneButton(dateSheet))).toBe(!canApply)
    })

    it('does not offer Yesterday as an end when that is before the start, and still offers Today', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()
      tapRepeats(sheet, 1)

      const dateSheet = await openEndDateSheet(sheet)

      expect(isDisabled(yesterdayChip(dateSheet))).toBe(true)
      expect(isDisabled(todayChip(dateSheet))).toBe(false)
    })

    it('shows the typed end on the chip, closes the date box and leaves only the income sheet', async () => {
      await renderIncomeSheet()
      const sheet = await findIncomeSheet()
      tapRepeats(sheet, 1)
      const dateSheet = await openEndDateSheet(sheet)

      typeDate(dateSheet, '2027-02-10')
      fireEvent.click(doneButton(dateSheet))

      await waitFor(() => {
        expect(screen.getAllByRole('dialog')).toHaveLength(1)
      })
      expect(endsChip(sheet, '10 Feb 2027')).toBeTruthy()
    })
  })
})
