import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { amountField, saveButton, typeAmount } from '@/test/expenseFormTestHelpers'
import { dateChip, noteChip, personalEntriesPost, shownCurrency } from '@/test/personalEntryTestHelpers'
import { findIncomeSheet, renderIncomeSheet } from '@/test/personalIncomeTestHelpers'
import { networkFailureAnswer, problemAnswer, requestsOf } from '@/test/requestTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

type RefusedField = 'amount' | 'date' | 'note' | null

interface RefusalCase {
  errorCode: string
  status: number
  field: RefusedField
}

const refusalCases: RefusalCase[] = [
  { errorCode: 'PERSONAL_ENTRY_AMOUNT_NOT_POSITIVE', status: 400, field: 'amount' },
  { errorCode: 'PERSONAL_ENTRY_AMOUNT_TOO_LARGE', status: 400, field: 'amount' },
  { errorCode: 'MONEY_NOT_ON_CURRENCY_STEP', status: 400, field: 'amount' },
  { errorCode: 'PERSONAL_ENTRY_CURRENCY_INVALID', status: 400, field: 'amount' },
  { errorCode: 'PERSONAL_ENTRY_DATE_INVALID', status: 400, field: 'date' },
  { errorCode: 'PERSONAL_ENTRY_NOTE_INVALID', status: 400, field: 'note' },
  { errorCode: 'PERSONAL_ENTRY_INCOME_HAS_CATEGORY', status: 400, field: null },
  { errorCode: 'PERSONAL_ENTRY_CLIENT_REQUEST_ID_USED', status: 400, field: null },
  { errorCode: 'GUEST_NOT_ALLOWED', status: 403, field: null },
]

function refusedFields(sheet: HTMLElement): RefusedField[] {
  const note = within(sheet).getByLabelText(translated('en', 'expense.note'))
  const marks: [RefusedField, boolean][] = [
    ['amount', amountField().getAttribute('aria-invalid') === 'true'],
    ['date', dateChip(sheet, translated('en', 'expenses.today')).getAttribute('aria-invalid') === 'true'],
    ['note', note.getAttribute('aria-invalid') === 'true'],
  ]
  return marks.filter(([, isMarked]) => isMarked).map(([field]) => field)
}

async function saveRefusedIncome(errorCode: string, status: number) {
  const rendered = await renderIncomeSheet({ answers: { [personalEntriesPost]: problemAnswer(status, errorCode) } })
  const sheet = await findIncomeSheet()
  typeAmount('65000')
  fireEvent.click(noteChip(sheet))
  fireEvent.change(within(sheet).getByLabelText(translated('en', 'expense.note')), { target: { value: 'Salary' } })
  fireEvent.click(saveButton())
  await screen.findByRole('alert')
  return { ...rendered, sheet }
}

describe('PersonalIncomeSheet, refused saves', () => {
  beforeEach(() => {
    window.localStorage.clear()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.clearAllMocks()
    vi.restoreAllMocks()
  })

  it.each(refusalCases)('shows the translated message for $errorCode above Save', async ({ errorCode, status }) => {
    await saveRefusedIncome(errorCode, status)

    const alert = screen.getByRole('alert')
    expect(alert.textContent).toBe(translated('en', `errors.${errorCode}`))
    expect(alert.compareDocumentPosition(saveButton()) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
  })

  it.each(refusalCases)('puts the red edge on the field $errorCode is about ($field) and on no other', async ({ errorCode, status, field }) => {
    const { sheet } = await saveRefusedIncome(errorCode, status)

    expect(refusedFields(sheet)).toEqual(field === null ? [] : [field])
  })

  it.each(refusalCases)('keeps the typed amount, the note and the sheet open after $errorCode', async ({ errorCode, status }) => {
    const { sheet, onClose } = await saveRefusedIncome(errorCode, status)

    expect(amountField().value).toBe('65000')
    expect(within(sheet).getByLabelText<HTMLTextAreaElement>(translated('en', 'expense.note')).value).toBe('Salary')
    expect(onClose).not.toHaveBeenCalled()
  })

  it('takes the red edge off the amount once the amount is changed', async () => {
    await saveRefusedIncome('PERSONAL_ENTRY_AMOUNT_TOO_LARGE', 400)

    typeAmount('130')

    expect(amountField().getAttribute('aria-invalid')).not.toBe('true')
  })

  it('takes the red edge off the amount once the currency is changed', async () => {
    const { sheet } = await saveRefusedIncome('PERSONAL_ENTRY_CURRENCY_INVALID', 400)

    fireEvent.click(shownCurrency(sheet, 'MKD'))

    expect(amountField().getAttribute('aria-invalid')).not.toBe('true')
  })

  it('takes the red edge off the note once the note is changed', async () => {
    const { sheet } = await saveRefusedIncome('PERSONAL_ENTRY_NOTE_INVALID', 400)

    const note = within(sheet).getByLabelText(translated('en', 'expense.note'))
    fireEvent.change(note, { target: { value: 'shorter' } })

    expect(note.getAttribute('aria-invalid')).not.toBe('true')
  })

  it.each(languages)('shows the network message when the server cannot be reached (%s)', async (language) => {
    await renderIncomeSheet({ language, answers: { [personalEntriesPost]: networkFailureAnswer() } })
    await findIncomeSheet(language)
    typeAmount('65000', language)

    fireEvent.click(saveButton(language))

    expect((await screen.findByRole('alert')).textContent).toBe(translated(language, 'errors.network'))
  })

  it('lets Save be tapped again after a refusal and sends a second request', async () => {
    const { fetchMock } = await saveRefusedIncome('PERSONAL_ENTRY_AMOUNT_TOO_LARGE', 400)

    typeAmount('130')
    fireEvent.click(saveButton())

    await waitFor(() => {
      expect(requestsOf(fetchMock, 'POST', '/api/personal/entries')).toHaveLength(2)
    })
  })
})
