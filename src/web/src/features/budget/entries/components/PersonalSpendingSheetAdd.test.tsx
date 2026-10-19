import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import type { PersonalCategory } from '@/core/services/personalCategories/personalCategoriesService'
import { kvitGlassPanelLook } from '@/shared/components/kvitGlassLooks'
import {
  addDays,
  amountField,
  invalidAmountTextCases,
  isDisabled,
  saveButton,
  sentBody,
  todayChip,
  typeAmount,
  yesterdayChip,
} from '@/test/expenseFormTestHelpers'
import { coffeeCategory, personalFoodCategory } from '@/test/personalCategoryTestData'
import {
  findSheet,
  personalCategoryCreatePost,
  personalCategoryLabel,
  saveButtonIn,
  sheetField,
} from '@/test/personalCategoryTestHelpers'
import { coffeeEntry, entryAnswerRow } from '@/test/personalEntryTestData'
import {
  dateChip,
  findDetailsSheet,
  findPickSheet,
  isEntriesCacheRefreshed,
  noteChip,
  personalEntriesPost,
  pickCategory,
  renderSpendingSheet,
  shownCurrency,
  waitForEveryDialogToClose,
} from '@/test/personalEntryTestHelpers'
import { jsonAnswer, networkFailureAnswer, requestsOf } from '@/test/requestTestHelpers'
import { testMe } from '@/test/testMe'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/
const savedAnswer = { [personalEntriesPost]: jsonAnswer(entryAnswerRow(coffeeEntry)) }
const sentPosts = (fetchMock: Parameters<typeof requestsOf>[0]) => requestsOf(fetchMock, 'POST', '/api/personal/entries')

const createdTea: PersonalCategory = {
  id: 'd0000000-0000-4000-8000-000000000009',
  key: null,
  name: 'Tea',
  emoji: '\u{1F375}',
  color: 'green',
  sortOrder: 103,
  isCustom: true,
  archived: false,
}

describe('PersonalSpendingSheet, adding', () => {
  beforeEach(() => {
    window.localStorage.clear()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.clearAllMocks()
    vi.restoreAllMocks()
  })

  describe('the first stage', () => {
    it.each(languages)('opens on the category grid titled "What did you spend on?" in a glass sheet (%s)', async (language) => {
      await renderSpendingSheet({ language })

      const sheet = await findPickSheet(language)

      expect(sheet.className.split(/\s+/)).toEqual(expect.arrayContaining(kvitGlassPanelLook.split(/\s+/)))
      expect(await within(sheet).findByRole('button', { name: personalCategoryLabel(language, personalFoodCategory) })).toBeTruthy()
    })

    it('moves to the amount with one tap on a category, with the amount focused and the number keypad', async () => {
      await renderSpendingSheet()

      const sheet = await pickCategory(coffeeCategory)

      await waitFor(() => {
        expect(document.activeElement).toBe(amountField())
      })
      expect(amountField().inputMode).toBe('numeric')
      expect(within(sheet).queryByRole('button', { name: personalCategoryLabel('en', personalFoodCategory) })).toBeNull()
    })

    it('shows the picked category in the header with its tile, and the header is not a button', async () => {
      await renderSpendingSheet()

      const sheet = await pickCategory(coffeeCategory)

      const heading = within(sheet).getByRole('heading', { name: 'Coffee' })
      expect(heading.querySelector<HTMLElement>('.size-11')?.style.backgroundColor).toBe('var(--category-teal)')
      expect(heading.closest('button')).toBeNull()
      expect(within(heading).queryByRole('button')).toBeNull()
    })

    it('goes on to the amount with the new category right after "+ New" is saved', async () => {
      await renderSpendingSheet({ answers: { [personalCategoryCreatePost]: jsonAnswer(createdTea) } })
      const pickSheet = await findPickSheet()
      fireEvent.click(await within(pickSheet).findByRole('button', { name: translated('en', 'budget.categories.new') }))
      const newSheet = await findSheet('en', 'budget.categories.newTitle')

      fireEvent.change(sheetField(newSheet, 'en', 'budget.categories.name'), { target: { value: 'Tea' } })
      fireEvent.click(saveButtonIn(newSheet))

      expect(await findDetailsSheet(createdTea)).toBeTruthy()
      expect(amountField()).toBeTruthy()
    })
  })

  describe('saving with the fewest taps', () => {
    it('saves with a category tap, the typed amount and Save: 2 taps and 1 field', async () => {
      const { fetchMock } = await renderSpendingSheet({ answers: savedAnswer })
      let taps = 0

      const sheet = await findPickSheet()
      fireEvent.click(await within(sheet).findByRole('button', { name: 'Coffee' }))
      taps += 1
      await findDetailsSheet(coffeeCategory)
      typeAmount('120')
      fireEvent.click(saveButton())
      taps += 1

      await waitFor(() => {
        expect(sentPosts(fetchMock)).toHaveLength(1)
      })
      expect(taps).toBe(2)
    })

    it('sends a request id, the category, the amount in deni, MKD, today and no note', async () => {
      vi.useFakeTimers({ toFake: ['Date'] })
      vi.setSystemTime(new Date('2026-10-19T10:00:00Z'))
      const { fetchMock } = await renderSpendingSheet({ answers: savedAnswer })
      await pickCategory(coffeeCategory)

      typeAmount('120')
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(sentPosts(fetchMock)).toHaveLength(1)
      })
      const body = sentBody(fetchMock, 'POST', '/api/personal/entries')
      expect(body).toEqual({
        clientRequestId: expect.stringMatching(uuidPattern),
        categoryId: coffeeCategory.id,
        amountMinor: 12000,
        currency: 'MKD',
        entryDate: '2026-10-19',
        note: null,
      })
    })

    it('closes, shows "Saved" and refreshes the entries after a save', async () => {
      const { onClose, queryClient } = await renderSpendingSheet({ answers: savedAnswer })
      await pickCategory(coffeeCategory)

      typeAmount('120')
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(onClose).toHaveBeenCalledOnce()
      })
      await waitForEveryDialogToClose()
      expect(vi.mocked(toast.success)).toHaveBeenCalledWith(translated('en', 'groupSettings.saved'))
      expect(isEntriesCacheRefreshed(queryClient)).toBe(true)
    })

    it('keeps Save disabled until the amount is a positive amount', async () => {
      await renderSpendingSheet()
      await pickCategory(coffeeCategory)

      expect(isDisabled(saveButton())).toBe(true)
      typeAmount('0')
      expect(isDisabled(saveButton())).toBe(true)
      typeAmount('5')
      expect(isDisabled(saveButton())).toBe(false)
    })

    it('sends one request when Save is tapped twice quickly', async () => {
      const { fetchMock } = await renderSpendingSheet({ answers: savedAnswer })
      await pickCategory(coffeeCategory)
      typeAmount('120')

      fireEvent.click(saveButton())
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(sentPosts(fetchMock)).toHaveLength(1)
      })
      await waitForEveryDialogToClose()
      expect(sentPosts(fetchMock)).toHaveLength(1)
    })

    it('sends one request when Enter is pressed twice in the amount', async () => {
      const { fetchMock } = await renderSpendingSheet({ answers: savedAnswer })
      await pickCategory(coffeeCategory)
      typeAmount('120')

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
      const { fetchMock } = await renderSpendingSheet({ answers: { [personalEntriesPost]: () => answer() } })
      await pickCategory(coffeeCategory)
      typeAmount('120')
      fireEvent.click(saveButton())
      await screen.findByRole('alert')

      answer = jsonAnswer(entryAnswerRow(coffeeEntry))
      fireEvent.click(saveButton())

      await waitForEveryDialogToClose()
      const [first, second] = sentPosts(fetchMock).map((request) => (request.body as { clientRequestId: string }).clientRequestId)
      expect(second).toBe(first)
    })

    it('makes a new request id for every time the sheet opens', async () => {
      const first = await renderSpendingSheet({ answers: savedAnswer })
      await pickCategory(coffeeCategory)
      typeAmount('120')
      fireEvent.click(saveButton())
      await waitForEveryDialogToClose()
      const firstId = (sentPosts(first.fetchMock)[0].body as { clientRequestId: string }).clientRequestId

      const second = await renderSpendingSheet({ answers: savedAnswer })
      await pickCategory(coffeeCategory)
      typeAmount('130')
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
      const { fetchMock } = await renderSpendingSheet({ answers: savedAnswer })
      await pickCategory(coffeeCategory)

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
      const { fetchMock } = await renderSpendingSheet({ answers: savedAnswer })
      const sheet = await pickCategory(coffeeCategory)

      fireEvent.click(shownCurrency(sheet, 'MKD'))
      typeAmount(text)
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(sentPosts(fetchMock)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'POST', '/api/personal/entries')).toMatchObject({ amountMinor, currency: 'EUR' })
    })

    it('opens the decimal keypad in EUR', async () => {
      await renderSpendingSheet()
      const sheet = await pickCategory(coffeeCategory)

      fireEvent.click(shownCurrency(sheet, 'MKD'))

      expect(amountField().inputMode).toBe('decimal')
    })

    it.each(invalidAmountTextCases)('refuses $name with a red edge and Save disabled', async ({ currency, text }) => {
      await renderSpendingSheet()
      const sheet = await pickCategory(coffeeCategory)
      if (currency === 'EUR') {
        fireEvent.click(shownCurrency(sheet, 'MKD'))
      }

      typeAmount(text)

      expect(isDisabled(saveButton())).toBe(true)
      expect(amountField().getAttribute('aria-invalid')).toBe('true')
    })

    it('uses a big amount font, so phones do not zoom the page', async () => {
      await renderSpendingSheet()
      await pickCategory(coffeeCategory)

      expect(Array.from(amountField().classList)).toContain('text-5xl')
    })
  })

  describe('the default currency', () => {
    async function saveOneEntry(currencyToPick: 'MKD' | 'EUR' | null): Promise<void> {
      await renderSpendingSheet({ answers: savedAnswer })
      const sheet = await pickCategory(coffeeCategory)
      if (currencyToPick === 'EUR') {
        fireEvent.click(shownCurrency(sheet, 'MKD'))
      }
      typeAmount('12')
      fireEvent.click(saveButton())
      await waitForEveryDialogToClose()
    }

    it('is MKD for the first entry ever', async () => {
      await renderSpendingSheet()

      const sheet = await pickCategory(coffeeCategory)

      expect(shownCurrency(sheet, 'MKD')).toBeTruthy()
    })

    it('is EUR on the next sheet after an entry in EUR was saved', async () => {
      await saveOneEntry('EUR')

      await renderSpendingSheet()
      const sheet = await pickCategory(coffeeCategory)

      expect(shownCurrency(sheet, 'EUR')).toBeTruthy()
    })

    it('goes back to MKD after the next entry is saved in MKD', async () => {
      await saveOneEntry('EUR')
      await renderSpendingSheet({ answers: savedAnswer })
      const sheet = await pickCategory(coffeeCategory)
      fireEvent.click(shownCurrency(sheet, 'EUR'))
      typeAmount('120')
      fireEvent.click(saveButton())
      await waitForEveryDialogToClose()

      await renderSpendingSheet()
      expect(shownCurrency(await pickCategory(coffeeCategory), 'MKD')).toBeTruthy()
    })

    it('is MKD for another person on the same browser', async () => {
      await saveOneEntry('EUR')

      await renderSpendingSheet({ me: { ...testMe, id: '9e8d7c6b-5a49-4382-9170-6f5e4d3c2b1a' } })
      const sheet = await pickCategory(coffeeCategory)

      expect(shownCurrency(sheet, 'MKD')).toBeTruthy()
    })

    it('is MKD when the browser storage cannot be read', async () => {
      await saveOneEntry('EUR')
      vi.spyOn(console, 'warn').mockImplementation(() => {})
      vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
        throw new DOMException('storage is blocked', 'SecurityError')
      })

      await renderSpendingSheet()
      const sheet = await pickCategory(coffeeCategory)

      expect(shownCurrency(sheet, 'MKD')).toBeTruthy()
    })

    it('is not changed by a save that the server refused', async () => {
      await renderSpendingSheet({ answers: { [personalEntriesPost]: networkFailureAnswer() } })
      const sheet = await pickCategory(coffeeCategory)
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
      const { fetchMock } = await renderSpendingSheet({ me: { ...testMe, timeZone }, answers: savedAnswer })
      const sheet = await pickCategory(coffeeCategory)

      expect(dateChip(sheet, translated('en', 'expenses.today'))).toBeTruthy()
      typeAmount('120')
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(sentPosts(fetchMock)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'POST', '/api/personal/entries')).toMatchObject({ entryDate: today })
    })

    it('changes to Yesterday through the date sheet and sends that day', async () => {
      vi.useFakeTimers({ toFake: ['Date'] })
      vi.setSystemTime(new Date('2026-10-19T10:00:00Z'))
      const { fetchMock } = await renderSpendingSheet({ answers: savedAnswer })
      const sheet = await pickCategory(coffeeCategory)
      typeAmount('120')

      fireEvent.click(dateChip(sheet, translated('en', 'expenses.today')))
      const dateSheet = await screen.findByRole('dialog', { name: translated('en', 'expense.pickDate') })
      expect(todayChip(dateSheet)).toBeTruthy()
      fireEvent.click(yesterdayChip(dateSheet))
      await waitFor(() => {
        expect(screen.queryByRole('dialog', { name: translated('en', 'expense.pickDate') })).toBeNull()
      })

      expect(dateChip(sheet, translated('en', 'expenses.yesterday'))).toBeTruthy()
      expect(amountField().value).toBe('120')
      fireEvent.click(saveButton())
      await waitFor(() => {
        expect(sentPosts(fetchMock)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'POST', '/api/personal/entries')).toMatchObject({ entryDate: addDays('2026-10-19', -1) })
    })

    it('gives the date chip a 44 px tap height', async () => {
      await renderSpendingSheet()
      const sheet = await pickCategory(coffeeCategory)

      expect(Array.from(dateChip(sheet, translated('en', 'expenses.today')).classList)).toContain('min-h-11')
    })
  })

  describe('the note', () => {
    it('opens a note field with one tap, puts the cursor in it and sends the trimmed note', async () => {
      const { fetchMock } = await renderSpendingSheet({ answers: savedAnswer })
      const sheet = await pickCategory(coffeeCategory)
      typeAmount('120')

      fireEvent.click(noteChip(sheet))
      const note = within(sheet).getByLabelText<HTMLTextAreaElement>(translated('en', 'expense.note'))
      await waitFor(() => {
        expect(document.activeElement).toBe(note)
      })
      fireEvent.change(note, { target: { value: '  coffee with Ana  ' } })
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(sentPosts(fetchMock)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'POST', '/api/personal/entries')).toMatchObject({ note: 'coffee with Ana' })
    })

    it('uses a note font of at least 16 px, so phones do not zoom the page', async () => {
      await renderSpendingSheet()
      const sheet = await pickCategory(coffeeCategory)

      fireEvent.click(noteChip(sheet))

      expect(Array.from(within(sheet).getByLabelText(translated('en', 'expense.note')).classList)).toContain('text-[1.0625rem]')
    })
  })

  describe('the look', () => {
    it('draws the chips with the app colour tokens, so light and dark both follow the theme', async () => {
      await renderSpendingSheet()
      const sheet = await pickCategory(coffeeCategory)

      for (const chip of [dateChip(sheet, translated('en', 'expenses.today')), noteChip(sheet)]) {
        expect(Array.from(chip.classList)).toEqual(expect.arrayContaining(['bg-field', 'border-field-border', 'text-field-foreground', 'min-h-11']))
      }
    })

    it.each(languages)('writes Save and the chips in %s', async (language) => {
      await renderSpendingSheet({ language })
      const sheet = await pickCategory(coffeeCategory, language)

      expect(within(sheet).getByRole('button', { name: translated(language, 'groupSettings.save') })).toBeTruthy()
      expect(dateChip(sheet, translated(language, 'expenses.today'))).toBeTruthy()
      expect(noteChip(sheet, language)).toBeTruthy()
      expect(shownCurrency(sheet, 'MKD', language)).toBeTruthy()
    })

    it('shows no Delete and no Category chip while adding', async () => {
      await renderSpendingSheet()
      const sheet = await pickCategory(coffeeCategory)

      expect(within(sheet).queryByRole('button', { name: translated('en', 'expense.delete') })).toBeNull()
      expect(within(sheet).queryByRole('button', { name: translated('en', 'expense.category') })).toBeNull()
    })
  })
})
