import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { amountField, saveButton, sentBody, typeAmount } from '@/test/expenseFormTestHelpers'
import { archivedGymCategory, coffeeCategory, personalFoodCategory } from '@/test/personalCategoryTestData'
import { personalCategoryLabel } from '@/test/personalCategoryTestHelpers'
import {
  coffeeEntry,
  entryAnswerRow,
  gymEntryWithArchivedCategory,
  lunchInEuroEntry,
} from '@/test/personalEntryTestData'
import {
  categoryChip,
  categoryHeading,
  dateChip,
  deleteButton,
  findDetailsSheet,
  findPickSheet,
  isEntriesCacheRefreshed,
  personalEntriesPost,
  personalEntryDelete,
  personalEntryPut,
  personalEntryRestorePost,
  renderSpendingSheet,
  shownCurrency,
  waitForEveryDialogToClose,
} from '@/test/personalEntryTestHelpers'
import {
  jsonAnswer,
  noContentAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
} from '@/test/requestTestHelpers'
import { expectSaveOutsideScrollingFields, expectTitleOutsideScrollingBody } from '@/test/sheetScrollTestHelpers'
import { testMe } from '@/test/testMe'
import { pressToastAction, toastShownWithText } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const coffeePut = personalEntryPut(coffeeEntry.id)
const coffeeUrl = `/api/personal/entries/${coffeeEntry.id}`

describe('PersonalSpendingSheet, editing', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date('2026-10-19T10:00:00Z'))
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.clearAllMocks()
    vi.restoreAllMocks()
  })

  describe('opening', () => {
    it.each(languages)('goes straight to the amount stage, titled by the category of the entry (%s)', async (language) => {
      await renderSpendingSheet({ entry: coffeeEntry, language })

      const sheet = await findDetailsSheet(coffeeCategory, language)

      expect(amountField(language).value).toBe('120')
      expect(shownCurrency(sheet, 'MKD', language)).toBeTruthy()
      expect(dateChip(sheet, translated(language, 'expenses.today'))).toBeTruthy()
      expect(screen.queryByRole('dialog', { name: translated(language, 'budget.entries.pickCategory') })).toBeNull()
    })

    it('keeps the category, Save and Delete in view while the amount, chips and note scroll on a short window', async () => {
      await renderSpendingSheet({ entry: lunchInEuroEntry })

      const sheet = await findDetailsSheet(personalFoodCategory)

      expectTitleOutsideScrollingBody(sheet, translated('en', 'categories.food'))
      expectSaveOutsideScrollingFields(sheet, amountField(), saveButton())
      expectSaveOutsideScrollingFields(sheet, amountField(), deleteButton(sheet))
    })

    it('fills in a EUR entry with cents, its date and its note, with the note open', async () => {
      await renderSpendingSheet({ entry: lunchInEuroEntry })

      const sheet = await findDetailsSheet(personalFoodCategory)

      expect(amountField().value).toBe('12.50')
      expect(shownCurrency(sheet, 'EUR')).toBeTruthy()
      expect(dateChip(sheet, translated('en', 'expenses.yesterday'))).toBeTruthy()
      expect(within(sheet).getByLabelText<HTMLTextAreaElement>(translated('en', 'expense.note')).value).toBe('Lunch in Ohrid')
    })

    it('uses the currency of the entry, not the remembered one', async () => {
      window.localStorage.setItem(`kvit.lastEntryCurrency.${testMe.id}`, 'EUR')

      await renderSpendingSheet({ entry: coffeeEntry })

      expect(shownCurrency(await findDetailsSheet(coffeeCategory), 'MKD')).toBeTruthy()
    })

    it('shows the archived category of an old entry with its tile, like any other', async () => {
      await renderSpendingSheet({ entry: gymEntryWithArchivedCategory })

      const sheet = await findDetailsSheet(archivedGymCategory)

      expect(categoryHeading(sheet).querySelector<HTMLElement>('.size-11')?.style.backgroundColor).toBe('var(--category-red)')
      expect(categoryHeading(sheet).textContent).toContain('Gym')
    })

    it('offers a Category chip and a red Delete, both 44 px tall', async () => {
      await renderSpendingSheet({ entry: coffeeEntry })

      const sheet = await findDetailsSheet(coffeeCategory)

      expect(Array.from(categoryChip(sheet).classList)).toContain('min-h-11')
      expect(Array.from(deleteButton(sheet).classList)).toEqual(expect.arrayContaining(['text-danger', 'min-h-13']))
    })
  })

  describe('saving', () => {
    it('sends PUT with the changed amount and the other fields, without a request id', async () => {
      const { fetchMock } = await renderSpendingSheet({
        entry: lunchInEuroEntry,
        answers: { [personalEntryPut(lunchInEuroEntry.id)]: jsonAnswer(entryAnswerRow(lunchInEuroEntry)) },
      })
      await findDetailsSheet(personalFoodCategory)

      typeAmount('14,20')
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'PUT', `/api/personal/entries/${lunchInEuroEntry.id}`)).toBe(1)
      })
      expect(sentBody(fetchMock, 'PUT', `/api/personal/entries/${lunchInEuroEntry.id}`)).toEqual({
        categoryId: personalFoodCategory.id,
        amountMinor: 1420,
        currency: 'EUR',
        entryDate: '2026-10-18',
        note: 'Lunch in Ohrid',
      })
      expect(requestCount(fetchMock, 'POST', '/api/personal/entries')).toBe(0)
    })

    it('closes, shows "Saved", refreshes the entries and remembers the currency after a save', async () => {
      const { onClose, queryClient } = await renderSpendingSheet({
        entry: lunchInEuroEntry,
        answers: { [personalEntryPut(lunchInEuroEntry.id)]: jsonAnswer(entryAnswerRow(lunchInEuroEntry)) },
      })
      await findDetailsSheet(personalFoodCategory)

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(onClose).toHaveBeenCalledOnce()
      })
      expect(vi.mocked(toast.success)).toHaveBeenCalledWith(translated('en', 'groupSettings.saved'))
      expect(isEntriesCacheRefreshed(queryClient)).toBe(true)
      expect(window.localStorage.getItem(`kvit.lastEntryCurrency.${testMe.id}`)).toBe('EUR')
    })

    it('keeps the archived category when only the amount changes', async () => {
      const { fetchMock } = await renderSpendingSheet({
        entry: gymEntryWithArchivedCategory,
        answers: {
          [personalEntryPut(gymEntryWithArchivedCategory.id)]: jsonAnswer(entryAnswerRow(gymEntryWithArchivedCategory)),
        },
      })
      await findDetailsSheet(archivedGymCategory)

      typeAmount('1600')
      fireEvent.click(saveButton())

      await waitForEveryDialogToClose()
      expect(sentBody(fetchMock, 'PUT', `/api/personal/entries/${gymEntryWithArchivedCategory.id}`)).toMatchObject({
        categoryId: archivedGymCategory.id,
        amountMinor: 160000,
      })
    })

    it('sends one PUT when Save is tapped twice quickly', async () => {
      const { fetchMock } = await renderSpendingSheet({
        entry: coffeeEntry,
        answers: { [coffeePut]: jsonAnswer(entryAnswerRow(coffeeEntry)) },
      })
      await findDetailsSheet(coffeeCategory)

      fireEvent.click(saveButton())
      fireEvent.click(saveButton())

      await waitForEveryDialogToClose()
      expect(requestCount(fetchMock, 'PUT', coffeeUrl)).toBe(1)
    })

    it('puts the red edge on the Category chip when the new category was refused', async () => {
      await renderSpendingSheet({
        entry: coffeeEntry,
        answers: { [coffeePut]: problemAnswer(400, 'PERSONAL_ENTRY_CATEGORY_INVALID') },
      })
      const sheet = await findDetailsSheet(coffeeCategory)

      fireEvent.click(saveButton())

      expect((await screen.findByRole('alert')).textContent).toBe(translated('en', 'errors.PERSONAL_ENTRY_CATEGORY_INVALID'))
      expect(categoryChip(sheet).getAttribute('aria-invalid')).toBe('true')
    })
  })

  describe('changing the category', () => {
    it('opens the grid from the Category chip and comes back with the typed amount kept', async () => {
      const { fetchMock } = await renderSpendingSheet({
        entry: coffeeEntry,
        answers: { [coffeePut]: jsonAnswer(entryAnswerRow(coffeeEntry)) },
      })
      const sheet = await findDetailsSheet(coffeeCategory)
      typeAmount('180')

      fireEvent.click(categoryChip(sheet))
      const pickSheet = await findPickSheet()
      fireEvent.click(await within(pickSheet).findByRole('button', { name: personalCategoryLabel('en', personalFoodCategory) }))

      await findDetailsSheet(personalFoodCategory)
      expect(amountField().value).toBe('180')
      fireEvent.click(saveButton())
      await waitForEveryDialogToClose()
      expect(sentBody(fetchMock, 'PUT', coffeeUrl)).toMatchObject({ categoryId: personalFoodCategory.id, amountMinor: 18000 })
    })
  })

  describe('deleting', () => {
    it('deletes at once with one tap, with no question, and closes the sheet', async () => {
      const { fetchMock, onClose, queryClient } = await renderSpendingSheet({
        entry: coffeeEntry,
        answers: { [personalEntryDelete(coffeeEntry.id)]: noContentAnswer() },
      })
      const sheet = await findDetailsSheet(coffeeCategory)

      fireEvent.click(deleteButton(sheet))

      await waitFor(() => {
        expect(onClose).toHaveBeenCalledOnce()
      })
      expect(requestCount(fetchMock, 'DELETE', coffeeUrl)).toBe(1)
      expect(screen.queryByRole('dialog')).toBeNull()
      expect(isEntriesCacheRefreshed(queryClient)).toBe(true)
    })

    it.each(languages)('shows a "Deleted" toast with Undo (%s)', async (language) => {
      await renderSpendingSheet({
        entry: coffeeEntry,
        language,
        answers: { [personalEntryDelete(coffeeEntry.id)]: noContentAnswer() },
      })
      const sheet = await findDetailsSheet(coffeeCategory, language)

      fireEvent.click(deleteButton(sheet, language))

      await waitFor(() => {
        expect(toastShownWithText(translated(language, 'budget.entries.deleted'))).toBeTruthy()
      })
      expect(toastShownWithText(translated(language, 'budget.entries.deleted')).options.action).toMatchObject({
        label: translated(language, 'common.undo'),
      })
    })

    it('brings the entry back with Undo and says "Restored"', async () => {
      const { fetchMock } = await renderSpendingSheet({
        entry: coffeeEntry,
        answers: {
          [personalEntryDelete(coffeeEntry.id)]: noContentAnswer(),
          [personalEntryRestorePost(coffeeEntry.id)]: noContentAnswer(),
        },
      })
      fireEvent.click(deleteButton(await findDetailsSheet(coffeeCategory)))
      await waitForEveryDialogToClose()

      pressToastAction(toastShownWithText(translated('en', 'budget.entries.deleted')))

      await waitFor(() => {
        expect(vi.mocked(toast.success)).toHaveBeenCalledWith(translated('en', 'budget.entries.restored'))
      })
      expect(requestCount(fetchMock, 'POST', `${coffeeUrl}/restore`)).toBe(1)
    })

    it('shows the translated error when Undo comes too late', async () => {
      await renderSpendingSheet({
        entry: coffeeEntry,
        answers: {
          [personalEntryDelete(coffeeEntry.id)]: noContentAnswer(),
          [personalEntryRestorePost(coffeeEntry.id)]: problemAnswer(400, 'PERSONAL_ENTRY_RESTORE_EXPIRED'),
        },
      })
      fireEvent.click(deleteButton(await findDetailsSheet(coffeeCategory)))
      await waitForEveryDialogToClose()

      pressToastAction(toastShownWithText(translated('en', 'budget.entries.deleted')))

      await waitFor(() => {
        expect(vi.mocked(toast.error)).toHaveBeenCalledWith(translated('en', 'errors.PERSONAL_ENTRY_RESTORE_EXPIRED'))
      })
    })

    it('sends one DELETE when Delete is tapped twice quickly', async () => {
      const { fetchMock } = await renderSpendingSheet({
        entry: coffeeEntry,
        answers: { [personalEntryDelete(coffeeEntry.id)]: noContentAnswer() },
      })
      const sheet = await findDetailsSheet(coffeeCategory)

      fireEvent.click(deleteButton(sheet))
      fireEvent.click(deleteButton(sheet))

      await waitForEveryDialogToClose()
      expect(requestCount(fetchMock, 'DELETE', coffeeUrl)).toBe(1)
    })

    it('keeps the sheet open and shows the error as a toast when the delete fails for another reason', async () => {
      const { onClose } = await renderSpendingSheet({
        entry: coffeeEntry,
        answers: { [personalEntryDelete(coffeeEntry.id)]: problemAnswer(403, 'GUEST_NOT_ALLOWED') },
      })
      const sheet = await findDetailsSheet(coffeeCategory)

      fireEvent.click(deleteButton(sheet))

      await waitFor(() => {
        expect(vi.mocked(toast.error)).toHaveBeenCalledWith(translated('en', 'errors.GUEST_NOT_ALLOWED'))
      })
      expect(onClose).not.toHaveBeenCalled()
      expect(screen.getByRole('dialog')).toBeTruthy()
    })
  })

  describe('an entry that no longer exists', () => {
    it.each([
      ['Save', 'PUT'],
      ['Delete', 'DELETE'],
    ] as const)('closes the sheet, shows the error as a toast and refreshes the entries after %s', async (button, method) => {
      const request = method === 'PUT' ? coffeePut : personalEntryDelete(coffeeEntry.id)
      const { onClose, queryClient, fetchMock } = await renderSpendingSheet({
        entry: coffeeEntry,
        answers: { [request]: problemAnswer(404, 'PERSONAL_ENTRY_NOT_FOUND') },
      })
      const sheet = await findDetailsSheet(coffeeCategory)

      fireEvent.click(button === 'Save' ? saveButton() : deleteButton(sheet))

      await waitFor(() => {
        expect(onClose).toHaveBeenCalledOnce()
      })
      expect(vi.mocked(toast.error)).toHaveBeenCalledWith(translated('en', 'errors.PERSONAL_ENTRY_NOT_FOUND'))
      expect(isEntriesCacheRefreshed(queryClient)).toBe(true)
      expect(requestsOf(fetchMock, method, coffeeUrl)).toHaveLength(1)
      expect(requestCount(fetchMock, 'POST', '/api/personal/entries')).toBe(0)
      expect(screen.queryByRole('alert')).toBeNull()
    })
  })

  it('never sends the add request when editing', async () => {
    const { fetchMock } = await renderSpendingSheet({
      entry: coffeeEntry,
      answers: { [coffeePut]: jsonAnswer(entryAnswerRow(coffeeEntry)), [personalEntriesPost]: jsonAnswer({}) },
    })
    await findDetailsSheet(coffeeCategory)

    fireEvent.click(saveButton())

    await waitForEveryDialogToClose()
    expect(requestCount(fetchMock, 'POST', '/api/personal/entries')).toBe(0)
  })
})
