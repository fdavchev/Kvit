import { fireEvent, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { isDescribedBy } from '@/test/describedByTestHelpers'
import {
  categoryLimitPut,
  findLimitsCard,
  limitAmountField,
  openLimitSheetFromRow,
  renderBudgetWithLimits,
  saveLimitButton,
  typeLimit,
  wholeMonthName,
} from '@/test/budgetLimitTestHelpers'
import { octoberWithLimits } from '@/test/budgetLimitTestData'
import { budgetNow } from '@/test/budgetTestHelpers'
import { isBefore } from '@/test/expenseTestHelpers'
import { coffeeCategory } from '@/test/personalCategoryTestData'
import { problemAnswer, type AnswerFactory } from '@/test/requestTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const invalidAmountText = 'Type an amount above 0, in whole denars.'

const invalidAmounts = [
  ['zero', '0'],
  ['zeros only', '00'],
  ['a number with deni', '12.5'],
  ['a number with a decimal comma', '12,5'],
  ['letters', 'abc'],
  ['a minus sign', '-5'],
  ['a number with a space inside', '1 000'],
  ['one denar above the bound', '10000000000'],
  ['far above the bound', '99999999999'],
]

const validAmounts = [
  ['a number of whole denars', '5000'],
  ['the same number with a zero in front', '05000'],
  ['one denar', '1'],
  ['the biggest allowed amount', '9999999999'],
]

async function openCoffeeSheet(language: 'en' | 'mk' = 'en', answers: Record<string, AnswerFactory> = {}) {
  await renderBudgetWithLimits({ language, months: [octoberWithLimits], answers })
  const card = await findLimitsCard(language)
  return openLimitSheetFromRow(card, 'Coffee', language)
}

describe('BudgetScreen, the message about an amount that cannot be a limit', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(budgetNow)
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.clearAllMocks()
    vi.restoreAllMocks()
  })

  describe('when it shows', () => {
    it.each(invalidAmounts)('shows the message under the field for %s ("%s"), links it to the field, marks the field and keeps Save off', async (_name, text) => {
      const sheet = await openCoffeeSheet()

      typeLimit(sheet, text)

      const message = within(sheet).getByText(invalidAmountText)
      expect(isBefore(limitAmountField(sheet), message)).toBe(true)
      expect(isBefore(message, saveLimitButton(sheet))).toBe(true)
      expect(isDescribedBy(limitAmountField(sheet), message)).toBe(true)
      expect(limitAmountField(sheet).getAttribute('aria-invalid')).toBe('true')
      expect(saveLimitButton(sheet).disabled).toBe(true)
    })

    it.each(languages)('writes the message in the language of the app (%s)', async (language) => {
      const sheet = await openCoffeeSheet(language)

      typeLimit(sheet, '0', language)

      const message = within(sheet).getByText(translated(language, 'budget.limits.amountInvalid'))
      expect(isDescribedBy(limitAmountField(sheet, language), message)).toBe(true)
    })

    it('shows it in the sheet of the whole month too', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const sheet = await openLimitSheetFromRow(await findLimitsCard(), wholeMonthName())

      typeLimit(sheet, '12.5')

      expect(within(sheet).getByText(invalidAmountText)).toBeTruthy()
    })
  })

  describe('when it does not show', () => {
    it('shows no message and no red edge while the field is empty', async () => {
      const sheet = await openCoffeeSheet()

      typeLimit(sheet, '')

      expect(within(sheet).queryByText(invalidAmountText)).toBeNull()
      expect(limitAmountField(sheet).getAttribute('aria-invalid')).toBe('false')
    })

    it('shows no message for the limit that exists', async () => {
      const sheet = await openCoffeeSheet()

      expect(limitAmountField(sheet).value).toBe('4500')
      expect(within(sheet).queryByText(invalidAmountText)).toBeNull()
      expect(limitAmountField(sheet).getAttribute('aria-invalid')).toBe('false')
    })

    it.each(validAmounts)('shows no message for %s ("%s")', async (_name, text) => {
      const sheet = await openCoffeeSheet()

      typeLimit(sheet, text)

      expect(within(sheet).queryByText(invalidAmountText)).toBeNull()
      expect(limitAmountField(sheet).getAttribute('aria-invalid')).toBe('false')
    })

    it('takes the message away once the amount is corrected', async () => {
      const sheet = await openCoffeeSheet()
      typeLimit(sheet, '12.5')
      expect(within(sheet).getByText(invalidAmountText)).toBeTruthy()

      typeLimit(sheet, '12')

      expect(within(sheet).queryByText(invalidAmountText)).toBeNull()
      expect(limitAmountField(sheet).getAttribute('aria-invalid')).toBe('false')
    })

    it('takes the message away once the field is emptied', async () => {
      const sheet = await openCoffeeSheet()
      typeLimit(sheet, 'abc')
      expect(within(sheet).getByText(invalidAmountText)).toBeTruthy()

      typeLimit(sheet, '')

      expect(within(sheet).queryByText(invalidAmountText)).toBeNull()
    })

    it('shows the message of the server, and not this one, when the server refuses a valid amount', async () => {
      const sheet = await openCoffeeSheet('en', {
        [categoryLimitPut(coffeeCategory.id)]: problemAnswer(400, 'BUDGET_LIMIT_AMOUNT_INVALID'),
      })
      typeLimit(sheet, '12000')

      fireEvent.click(saveLimitButton(sheet))

      expect((await within(sheet).findByRole('alert')).textContent).toBe(translated('en', 'errors.BUDGET_LIMIT_AMOUNT_INVALID'))
      await waitFor(() => {
        expect(within(sheet).queryByText(invalidAmountText)).toBeNull()
      })
    })
  })
})
