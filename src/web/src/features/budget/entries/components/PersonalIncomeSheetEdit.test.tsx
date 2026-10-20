import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { amountField, isDisabled, saveButton, sentBody, typeAmount } from '@/test/expenseFormTestHelpers'
import { entryAnswerRow, salaryIncomeEntry, untitledEuroIncomeEntry } from '@/test/personalEntryTestData'
import {
  dateChip,
  deleteButton,
  isEntriesCacheRefreshed,
  noteChip,
  personalEntriesPost,
  personalEntryDelete,
  personalEntryPut,
  personalEntryRestorePost,
  repeatsChips,
  shownCurrency,
  waitForEveryDialogToClose,
} from '@/test/personalEntryTestHelpers'
import { findIncomeSheet, renderIncomeSheet } from '@/test/personalIncomeTestHelpers'
import {
  jsonAnswer,
  neverAnswer,
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

const salaryPut = personalEntryPut(salaryIncomeEntry.id)
const salaryUrl = `/api/personal/entries/${salaryIncomeEntry.id}`
const salaryDelete = personalEntryDelete(salaryIncomeEntry.id)

function salaryDateChip(sheet: HTMLElement): HTMLElement {
  return within(sheet).getByRole('button', { name: /15 Oct/ })
}

describe('PersonalIncomeSheet, editing', () => {
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
    it.each(languages)('is filled in with the amount, the currency, the date and the note of the income (%s)', async (language) => {
      await renderIncomeSheet({ entry: salaryIncomeEntry, language })

      const sheet = await findIncomeSheet(language)

      expect(amountField(language).value).toBe('65000')
      expect(shownCurrency(sheet, 'MKD', language)).toBeTruthy()
      expect(within(sheet).getByLabelText<HTMLTextAreaElement>(translated(language, 'expense.note')).value).toBe('Salary')
      expect(screen.queryByRole('dialog', { name: translated(language, 'budget.entries.pickCategory') })).toBeNull()
    })

    it('shows the date of the income on the date chip', async () => {
      await renderIncomeSheet({ entry: salaryIncomeEntry })

      const sheet = await findIncomeSheet()

      expect(salaryDateChip(sheet)).toBeTruthy()
    })

    it('fills in a EUR income with cents, with the note chip closed when the income has no note', async () => {
      await renderIncomeSheet({ entry: untitledEuroIncomeEntry })

      const sheet = await findIncomeSheet()

      expect(amountField().value).toBe('200')
      expect(shownCurrency(sheet, 'EUR')).toBeTruthy()
      expect(dateChip(sheet, translated('en', 'expenses.today'))).toBeTruthy()
      expect(noteChip(sheet)).toBeTruthy()
    })

    it('uses the currency of the income, not the remembered one', async () => {
      window.localStorage.setItem(`kvit.lastEntryCurrency.${testMe.id}`, 'EUR')

      await renderIncomeSheet({ entry: salaryIncomeEntry })

      expect(shownCurrency(await findIncomeSheet(), 'MKD')).toBeTruthy()
    })

    it('keeps the title, Save and Delete in view while the amount, chips and note scroll on a short window', async () => {
      await renderIncomeSheet({ entry: salaryIncomeEntry })

      const sheet = await findIncomeSheet()

      expectTitleOutsideScrollingBody(sheet, translated('en', 'budget.income.title'))
      expectSaveOutsideScrollingFields(sheet, amountField(), saveButton())
      expectSaveOutsideScrollingFields(sheet, amountField(), deleteButton(sheet))
    })

    it('offers a red Delete 44 px tall, and no category chip and no Repeats chip', async () => {
      await renderIncomeSheet({ entry: salaryIncomeEntry })

      const sheet = await findIncomeSheet()

      expect(Array.from(deleteButton(sheet).classList)).toEqual(expect.arrayContaining(['text-danger', 'min-h-13']))
      expect(within(sheet).queryByRole('button', { name: translated('en', 'expense.category') })).toBeNull()
      expect(repeatsChips(sheet)).toHaveLength(0)
    })
  })

  describe('saving', () => {
    it('sends PUT with kind Income, the changed amount and the other fields, with no categoryId and no request id', async () => {
      const { fetchMock } = await renderIncomeSheet({
        entry: salaryIncomeEntry,
        answers: { [salaryPut]: jsonAnswer(entryAnswerRow(salaryIncomeEntry)) },
      })
      await findIncomeSheet()

      typeAmount('70000')
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'PUT', salaryUrl)).toBe(1)
      })
      const body = sentBody(fetchMock, 'PUT', salaryUrl)
      expect(body).toEqual({
        kind: 'Income',
        amountMinor: 7000000,
        currency: 'MKD',
        entryDate: '2026-10-15',
        note: 'Salary',
      })
      expect(Object.keys(body)).not.toContain('categoryId')
      expect(Object.keys(body)).not.toContain('clientRequestId')
      expect(requestCount(fetchMock, 'POST', '/api/personal/entries')).toBe(0)
    })

    it('sends kind Income even when nothing was changed', async () => {
      const { fetchMock } = await renderIncomeSheet({
        entry: salaryIncomeEntry,
        answers: { [salaryPut]: jsonAnswer(entryAnswerRow(salaryIncomeEntry)) },
      })
      await findIncomeSheet()

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'PUT', salaryUrl)).toBe(1)
      })
      expect(sentBody(fetchMock, 'PUT', salaryUrl)).toMatchObject({ kind: 'Income', amountMinor: 6500000 })
    })

    it('sends no note when the note was emptied', async () => {
      const { fetchMock } = await renderIncomeSheet({
        entry: salaryIncomeEntry,
        answers: { [salaryPut]: jsonAnswer(entryAnswerRow(salaryIncomeEntry)) },
      })
      const sheet = await findIncomeSheet()

      fireEvent.change(within(sheet).getByLabelText(translated('en', 'expense.note')), { target: { value: '' } })
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'PUT', salaryUrl)).toBe(1)
      })
      expect(sentBody(fetchMock, 'PUT', salaryUrl)).toMatchObject({ note: null })
    })

    it('closes, shows "Saved", refreshes the entries and remembers the currency after a save', async () => {
      const { onClose, queryClient } = await renderIncomeSheet({
        entry: untitledEuroIncomeEntry,
        answers: { [personalEntryPut(untitledEuroIncomeEntry.id)]: jsonAnswer(entryAnswerRow(untitledEuroIncomeEntry)) },
      })
      await findIncomeSheet()

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(onClose).toHaveBeenCalledOnce()
      })
      expect(vi.mocked(toast.success)).toHaveBeenCalledWith(translated('en', 'groupSettings.saved'))
      expect(isEntriesCacheRefreshed(queryClient)).toBe(true)
      expect(window.localStorage.getItem(`kvit.lastEntryCurrency.${testMe.id}`)).toBe('EUR')
    })

    it('keeps a date older than the Budget strip when it is saved without changing it', async () => {
      const oldIncome = { ...salaryIncomeEntry, entryDate: '2023-05-04' }
      const { fetchMock } = await renderIncomeSheet({
        entry: oldIncome,
        answers: { [salaryPut]: jsonAnswer(entryAnswerRow(oldIncome)) },
      })
      await findIncomeSheet()

      typeAmount('70000')
      fireEvent.click(saveButton())

      await waitForEveryDialogToClose()
      expect(sentBody(fetchMock, 'PUT', salaryUrl)).toMatchObject({ entryDate: '2023-05-04', amountMinor: 7000000 })
    })

    it('sends one PUT when Save is tapped twice quickly', async () => {
      const { fetchMock } = await renderIncomeSheet({
        entry: salaryIncomeEntry,
        answers: { [salaryPut]: jsonAnswer(entryAnswerRow(salaryIncomeEntry)) },
      })
      await findIncomeSheet()

      fireEvent.click(saveButton())
      fireEvent.click(saveButton())

      await waitForEveryDialogToClose()
      expect(requestCount(fetchMock, 'PUT', salaryUrl)).toBe(1)
    })

    it('shows the translated message above Save and keeps the sheet open when the kind change is refused', async () => {
      const { onClose } = await renderIncomeSheet({
        entry: salaryIncomeEntry,
        answers: { [salaryPut]: problemAnswer(400, 'PERSONAL_ENTRY_KIND_CANNOT_CHANGE') },
      })
      await findIncomeSheet()

      fireEvent.click(saveButton())

      expect((await screen.findByRole('alert')).textContent).toBe(translated('en', 'errors.PERSONAL_ENTRY_KIND_CANNOT_CHANGE'))
      expect(onClose).not.toHaveBeenCalled()
    })

    it('puts the red edge on the amount when the amount was refused on an edit', async () => {
      await renderIncomeSheet({
        entry: salaryIncomeEntry,
        answers: { [salaryPut]: problemAnswer(400, 'PERSONAL_ENTRY_AMOUNT_TOO_LARGE') },
      })
      await findIncomeSheet()

      fireEvent.click(saveButton())

      expect((await screen.findByRole('alert')).textContent).toBe(translated('en', 'errors.PERSONAL_ENTRY_AMOUNT_TOO_LARGE'))
      expect(amountField().getAttribute('aria-invalid')).toBe('true')
    })
  })

  describe('deleting', () => {
    it('deletes at once with one tap, with no question, and closes the sheet', async () => {
      const { fetchMock, onClose, queryClient } = await renderIncomeSheet({
        entry: salaryIncomeEntry,
        answers: { [salaryDelete]: noContentAnswer() },
      })
      const sheet = await findIncomeSheet()

      fireEvent.click(deleteButton(sheet))

      await waitFor(() => {
        expect(onClose).toHaveBeenCalledOnce()
      })
      expect(requestCount(fetchMock, 'DELETE', salaryUrl)).toBe(1)
      expect(screen.queryByRole('dialog')).toBeNull()
      expect(isEntriesCacheRefreshed(queryClient)).toBe(true)
    })

    it.each(languages)('shows a "Deleted" toast with Undo (%s)', async (language) => {
      await renderIncomeSheet({
        entry: salaryIncomeEntry,
        language,
        answers: { [salaryDelete]: noContentAnswer() },
      })
      const sheet = await findIncomeSheet(language)

      fireEvent.click(deleteButton(sheet, language))

      await waitFor(() => {
        expect(toastShownWithText(translated(language, 'budget.entries.deleted'))).toBeTruthy()
      })
      expect(toastShownWithText(translated(language, 'budget.entries.deleted')).options.action).toMatchObject({
        label: translated(language, 'common.undo'),
      })
    })

    it('brings the income back with Undo and says "Restored"', async () => {
      const { fetchMock } = await renderIncomeSheet({
        entry: salaryIncomeEntry,
        answers: {
          [salaryDelete]: noContentAnswer(),
          [personalEntryRestorePost(salaryIncomeEntry.id)]: noContentAnswer(),
        },
      })
      fireEvent.click(deleteButton(await findIncomeSheet()))
      await waitForEveryDialogToClose()

      pressToastAction(toastShownWithText(translated('en', 'budget.entries.deleted')))

      await waitFor(() => {
        expect(vi.mocked(toast.success)).toHaveBeenCalledWith(translated('en', 'budget.entries.restored'))
      })
      expect(requestCount(fetchMock, 'POST', `${salaryUrl}/restore`)).toBe(1)
    })

    it('shows the translated error when Undo comes too late', async () => {
      await renderIncomeSheet({
        entry: salaryIncomeEntry,
        answers: {
          [salaryDelete]: noContentAnswer(),
          [personalEntryRestorePost(salaryIncomeEntry.id)]: problemAnswer(400, 'PERSONAL_ENTRY_RESTORE_EXPIRED'),
        },
      })
      fireEvent.click(deleteButton(await findIncomeSheet()))
      await waitForEveryDialogToClose()

      pressToastAction(toastShownWithText(translated('en', 'budget.entries.deleted')))

      await waitFor(() => {
        expect(vi.mocked(toast.error)).toHaveBeenCalledWith(translated('en', 'errors.PERSONAL_ENTRY_RESTORE_EXPIRED'))
      })
    })

    it('sends one DELETE when Delete is tapped twice quickly', async () => {
      const { fetchMock } = await renderIncomeSheet({
        entry: salaryIncomeEntry,
        answers: { [salaryDelete]: noContentAnswer() },
      })
      const sheet = await findIncomeSheet()

      fireEvent.click(deleteButton(sheet))
      fireEvent.click(deleteButton(sheet))

      await waitForEveryDialogToClose()
      expect(requestCount(fetchMock, 'DELETE', salaryUrl)).toBe(1)
    })

    it('keeps the sheet open and shows the error as a toast when the delete fails for another reason', async () => {
      const { onClose } = await renderIncomeSheet({
        entry: salaryIncomeEntry,
        answers: { [salaryDelete]: problemAnswer(403, 'GUEST_NOT_ALLOWED') },
      })
      const sheet = await findIncomeSheet()

      fireEvent.click(deleteButton(sheet))

      await waitFor(() => {
        expect(vi.mocked(toast.error)).toHaveBeenCalledWith(translated('en', 'errors.GUEST_NOT_ALLOWED'))
      })
      expect(onClose).not.toHaveBeenCalled()
      expect(screen.getByRole('dialog')).toBeTruthy()
    })
  })

  describe('one action at a time', () => {
    it('disables Delete, the chips and Save while a save is on its way, and sends no DELETE', async () => {
      const { fetchMock } = await renderIncomeSheet({
        entry: salaryIncomeEntry,
        answers: { [salaryPut]: neverAnswer(), [salaryDelete]: noContentAnswer() },
      })
      const sheet = await findIncomeSheet()

      fireEvent.click(saveButton())
      await waitFor(() => {
        expect(isDisabled(deleteButton(sheet))).toBe(true)
      })
      fireEvent.click(deleteButton(sheet))

      expect(isDisabled(saveButton())).toBe(true)
      expect(isDisabled(salaryDateChip(sheet))).toBe(true)
      expect(requestCount(fetchMock, 'DELETE', salaryUrl)).toBe(0)
    })

    it('disables Save and the chips while a delete is on its way, and sends no PUT', async () => {
      const { fetchMock } = await renderIncomeSheet({
        entry: salaryIncomeEntry,
        answers: { [salaryDelete]: neverAnswer(), [salaryPut]: jsonAnswer(entryAnswerRow(salaryIncomeEntry)) },
      })
      const sheet = await findIncomeSheet()

      fireEvent.click(deleteButton(sheet))
      await waitFor(() => {
        expect(isDisabled(saveButton())).toBe(true)
      })

      expect(isDisabled(salaryDateChip(sheet))).toBe(true)
      expect(requestCount(fetchMock, 'PUT', salaryUrl)).toBe(0)
    })
  })

  describe('an income that no longer exists', () => {
    it.each([
      ['Save', 'PUT'],
      ['Delete', 'DELETE'],
    ] as const)('closes the sheet, shows the error as a toast and refreshes the entries after %s', async (button, method) => {
      const request = method === 'PUT' ? salaryPut : salaryDelete
      const { onClose, queryClient, fetchMock } = await renderIncomeSheet({
        entry: salaryIncomeEntry,
        answers: { [request]: problemAnswer(404, 'PERSONAL_ENTRY_NOT_FOUND') },
      })
      const sheet = await findIncomeSheet()

      fireEvent.click(button === 'Save' ? saveButton() : deleteButton(sheet))

      await waitFor(() => {
        expect(onClose).toHaveBeenCalledOnce()
      })
      expect(vi.mocked(toast.error)).toHaveBeenCalledWith(translated('en', 'errors.PERSONAL_ENTRY_NOT_FOUND'))
      expect(isEntriesCacheRefreshed(queryClient)).toBe(true)
      expect(requestsOf(fetchMock, method, salaryUrl)).toHaveLength(1)
      expect(requestCount(fetchMock, 'POST', '/api/personal/entries')).toBe(0)
      expect(screen.queryByRole('alert')).toBeNull()
    })
  })

  it('never sends the add request when editing', async () => {
    const { fetchMock } = await renderIncomeSheet({
      entry: salaryIncomeEntry,
      answers: { [salaryPut]: jsonAnswer(entryAnswerRow(salaryIncomeEntry)), [personalEntriesPost]: jsonAnswer({}) },
    })
    await findIncomeSheet()

    fireEvent.click(saveButton())

    await waitForEveryDialogToClose()
    expect(requestCount(fetchMock, 'POST', '/api/personal/entries')).toBe(0)
  })
})
