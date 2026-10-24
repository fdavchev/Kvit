import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { isDisabled, saveButton, sentBody, typeAmount } from '@/test/expenseFormTestHelpers'
import { entryAnswerRow, generatedSalaryEntry, salaryIncomeEntry } from '@/test/personalEntryTestData'
import { deleteButton, describedText, personalEntryPut, waitForEveryDialogToClose } from '@/test/personalEntryTestHelpers'
import { findIncomeEditSheet, renderIncomeSheet } from '@/test/personalIncomeTestHelpers'
import { jsonAnswer, problemAnswer, requestsOf } from '@/test/requestTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const generatedUrl = `/api/personal/entries/${generatedSalaryEntry.id}`

function generatedDateChip(sheet: HTMLElement, language: Language = 'en'): HTMLElement {
  return within(sheet).getByRole('button', { name: language === 'en' ? /1 Oct/ : /1 окт/ })
}

function pickDateSheet(language: Language = 'en'): HTMLElement | null {
  return screen.queryByRole('dialog', { name: translated(language, 'expense.pickDate') })
}

describe('PersonalIncomeSheet, editing an entry made by a repeating income', () => {
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

  describe('the date', () => {
    it.each(languages)('shows the date on a chip that says it cannot be changed, with the reason under the chips (%s)', async (language) => {
      await renderIncomeSheet({ entry: generatedSalaryEntry, language })
      const sheet = await findIncomeEditSheet(language)

      const chip = generatedDateChip(sheet, language)

      expect(chip.getAttribute('aria-disabled')).toBe('true')
      expect(within(sheet).getByText(translated(language, 'budget.income.dateLockedReason'))).toBeTruthy()
      expect(describedText(chip)).toBe(translated(language, 'budget.income.dateLockedReason'))
    })

    it('keeps the locked date chip in the Tab order, so a keyboard or screen reader still reaches the date', async () => {
      await renderIncomeSheet({ entry: generatedSalaryEntry })
      const sheet = await findIncomeEditSheet()

      const chip = generatedDateChip(sheet)

      expect(chip.matches(':disabled')).toBe(false)
      expect(chip.tabIndex).toBeGreaterThanOrEqual(0)
    })

    it('opens no date box when the locked chip is pressed, by a tap or by Enter or Space (both press it the same way)', async () => {
      await renderIncomeSheet({ entry: generatedSalaryEntry })
      const sheet = await findIncomeEditSheet()

      fireEvent.click(generatedDateChip(sheet))

      expect(pickDateSheet()).toBeNull()
    })

    it('leaves the date chip of an income the person added open to change, with no reason', async () => {
      await renderIncomeSheet({ entry: salaryIncomeEntry })
      const sheet = await findIncomeEditSheet()

      const chip = within(sheet).getByRole('button', { name: /15 Oct/ })

      expect(isDisabled(chip)).toBe(false)
      expect(chip.hasAttribute('aria-describedby')).toBe(false)
      expect(within(sheet).queryByText(translated('en', 'budget.income.dateLockedReason'))).toBeNull()
      fireEvent.click(chip)
      expect(pickDateSheet()).not.toBeNull()
    })
  })

  describe('what still works', () => {
    it('keeps the amount, the note, Save and Delete open to use', async () => {
      await renderIncomeSheet({ entry: generatedSalaryEntry })
      const sheet = await findIncomeEditSheet()

      expect(isDisabled(saveButton())).toBe(false)
      expect(isDisabled(deleteButton(sheet))).toBe(false)
      expect(within(sheet).getByLabelText(translated('en', 'expense.note')).matches(':disabled')).toBe(false)
    })

    it('saves a changed amount with the date it came with', async () => {
      const { fetchMock } = await renderIncomeSheet({
        entry: generatedSalaryEntry,
        answers: { [personalEntryPut(generatedSalaryEntry.id)]: jsonAnswer(entryAnswerRow({ ...generatedSalaryEntry, amountMinor: 7000000 })) },
      })
      await findIncomeEditSheet()

      typeAmount('70000')
      fireEvent.click(saveButton())

      await waitForEveryDialogToClose()
      expect(requestsOf(fetchMock, 'PUT', generatedUrl)).toHaveLength(1)
      expect(sentBody(fetchMock, 'PUT', generatedUrl)).toMatchObject({ amountMinor: 7000000, entryDate: '2026-10-01' })
    })

    it.each(languages)('marks the date chip and shows the translated message when the server refuses a moved date (%s)', async (language) => {
      await renderIncomeSheet({
        entry: generatedSalaryEntry,
        language,
        answers: { [personalEntryPut(generatedSalaryEntry.id)]: problemAnswer(400, 'PERSONAL_ENTRY_DATE_LOCKED') },
      })
      const sheet = await findIncomeEditSheet(language)

      fireEvent.click(saveButton(language))

      expect((await screen.findByRole('alert')).textContent).toContain(translated(language, 'errors.PERSONAL_ENTRY_DATE_LOCKED'))
      await waitFor(() => {
        expect(generatedDateChip(sheet, language).getAttribute('aria-invalid')).toBe('true')
      })
    })
  })
})
