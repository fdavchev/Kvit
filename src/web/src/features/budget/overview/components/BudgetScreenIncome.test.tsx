import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { formatMoney, type Currency } from '@/shared/utils/formatMoney'
import { formatSignedMoney } from '@/shared/utils/formatSignedMoney'
import {
  budgetNow,
  categoryTotalsRegion,
  findEntryList,
  plusButton,
  renderBudget,
} from '@/test/budgetTestHelpers'
import {
  amountTexts,
  findLeftCard,
  incomeAttribute,
  leftAttribute,
  spentAttribute,
} from '@/test/budgetLeftCardTestHelpers'
import { amountField, saveButton, sentBody, typeAmount } from '@/test/expenseFormTestHelpers'
import { plainSpaces } from '@/test/expenseTestHelpers'
import { coffeeCategory, personalFoodCategory } from '@/test/personalCategoryTestData'
import {
  entryAnswerRow,
  euroIncomeOnlyOctober,
  incomeOnlyOctober,
  octoberWithSalary,
  octoberWithTwoIncomes,
  salaryIncomeEntry,
} from '@/test/personalEntryTestData'
import { deleteButton, findDetailsSheet, shownCurrency } from '@/test/personalEntryTestHelpers'
import { findIncomeEditSheet } from '@/test/personalIncomeTestHelpers'
import { jsonAnswer, requestCount } from '@/test/requestTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const incomeEmoji = '\u{1F4B0}'

function money(amountMinor: number, currency: Currency, language: Language = 'en'): string {
  return plainSpaces(formatMoney(amountMinor, currency, language))
}

function signedMoney(amountMinor: number, currency: Currency, language: Language = 'en'): string {
  return plainSpaces(formatSignedMoney(amountMinor, currency, language))
}

function readableText(element: Element): string {
  const copy = element.cloneNode(true)
  if (!(copy instanceof Element)) {
    throw new Error('Expected an element to read')
  }
  for (const hidden of copy.querySelectorAll('[aria-hidden="true"]')) {
    hidden.remove()
  }
  return plainSpaces(copy.textContent ?? '')
}

function rowAt(list: HTMLElement, index: number): HTMLElement {
  const row = within(list).getAllByRole('listitem')[index]
  if (row === undefined) {
    throw new Error(`The entry list has no row number ${index + 1}`)
  }
  return row
}

function titleOf(row: HTMLElement): string {
  const title = row.querySelector('[data-entry-text]')?.firstElementChild
  if (title === null || title === undefined) {
    throw new Error('The row has no title line')
  }
  return plainSpaces(title.textContent ?? '')
}

function subtitleOf(row: HTMLElement): string {
  const subtitle = row.querySelector('[data-entry-text]')?.lastElementChild
  if (subtitle === null || subtitle === undefined) {
    throw new Error('The row has no second line')
  }
  return plainSpaces(subtitle.textContent ?? '')
}

function amountOf(row: HTMLElement): HTMLElement {
  const amount = row.querySelector<HTMLElement>('[data-entry-amount]')
  if (amount === null) {
    throw new Error('The row has no amount')
  }
  return amount
}

function tileEmojiOf(row: HTMLElement): string | undefined {
  return row.querySelector('[aria-hidden="true"]')?.textContent ?? undefined
}

describe('BudgetScreen with income', () => {
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

  describe('an income row', () => {
    it('shows the money bag, the note as the title and the amount with a plus', async () => {
      await renderBudget({ months: [octoberWithSalary] })

      const row = rowAt(await findEntryList(), 2)

      expect(tileEmojiOf(row)).toBe(incomeEmoji)
      expect(titleOf(row)).toBe('Salary')
      expect(readableText(amountOf(row))).toBe(signedMoney(6500000, 'MKD'))
      expect(readableText(amountOf(row)).startsWith('+')).toBe(true)
    })

    it.each(languages)('uses the "Income" text as the title when the income has no note (%s)', async (language) => {
      await renderBudget({ language, months: [octoberWithTwoIncomes] })

      const row = rowAt(await findEntryList(language), 0)

      expect(titleOf(row)).toBe(translated(language, 'budget.income.listTitle'))
      expect(tileEmojiOf(row)).toBe(incomeEmoji)
    })

    it.each(languages)('writes the amount in the positive look with a plus, in each currency (%s)', async (language) => {
      await renderBudget({ language, months: [octoberWithTwoIncomes] })

      const list = await findEntryList(language)

      const euroIncome = amountOf(rowAt(list, 0))
      const denarIncome = amountOf(rowAt(list, 2))
      expect(readableText(euroIncome)).toBe(signedMoney(20000, 'EUR', language))
      expect(readableText(denarIncome)).toBe(signedMoney(6500000, 'MKD', language))
      expect(euroIncome.classList.contains('text-positive')).toBe(true)
      expect(denarIncome.classList.contains('text-positive')).toBe(true)
    })

    it('shows the day of the income on the second line', async () => {
      await renderBudget({ months: [octoberWithTwoIncomes] })

      const list = await findEntryList()

      expect(subtitleOf(rowAt(list, 0))).toContain(translated('en', 'expenses.today'))
      expect(subtitleOf(rowAt(list, 2))).toContain('15 Oct')
    })

    it('leaves a spending row as it was: its category, no plus and no positive colour', async () => {
      await renderBudget({ months: [octoberWithSalary] })

      const row = rowAt(await findEntryList(), 0)

      expect(titleOf(row)).toBe('Coffee')
      expect(tileEmojiOf(row)).toBe(coffeeCategory.emoji)
      expect(readableText(amountOf(row))).toBe(money(12000, 'MKD'))
      expect(readableText(amountOf(row))).not.toContain('+')
      expect(amountOf(row).classList.contains('text-positive')).toBe(false)
    })

    it('keeps the order of the answer of the server, income among spending', async () => {
      await renderBudget({ months: [octoberWithTwoIncomes] })

      const rows = within(await findEntryList()).getAllByRole('listitem')

      expect(rows.map(titleOf)).toEqual([translated('en', 'budget.income.listTitle'), 'Coffee', 'Salary'])
    })

    it('makes an income row a button at least 44 px high', async () => {
      await renderBudget({ months: [octoberWithSalary] })

      const button = within(rowAt(await findEntryList(), 2)).getByRole('button')

      expect(button.getAttribute('type')).toBe('button')
      expect(button.className.split(/\s+/)).toContain('min-h-16')
    })
  })

  describe('tapping an entry', () => {
    it('opens the income sheet, filled in, with Delete, when an income row is tapped', async () => {
      await renderBudget({ months: [octoberWithSalary] })
      const list = await findEntryList()

      fireEvent.click(within(rowAt(list, 2)).getByRole('button'))

      const sheet = await findIncomeEditSheet()
      expect(amountField().value).toBe('65000')
      expect(within(sheet).getByLabelText<HTMLTextAreaElement>(translated('en', 'expense.note')).value).toBe('Salary')
      expect(deleteButton(sheet)).toBeTruthy()
      expect(screen.queryByRole('dialog', { name: translated('en', 'budget.entries.pickCategory') })).toBeNull()
    })

    it('opens the income sheet and not the spending sheet for an income', async () => {
      await renderBudget({ months: [octoberWithSalary] })
      const list = await findEntryList()

      fireEvent.click(within(rowAt(list, 2)).getByRole('button'))

      await findIncomeEditSheet()
      expect(screen.getAllByRole('dialog')).toHaveLength(1)
    })

    it('opens the entry the person tapped when there are two incomes in two currencies', async () => {
      await renderBudget({ months: [octoberWithTwoIncomes] })
      const list = await findEntryList()

      fireEvent.click(within(rowAt(list, 0)).getByRole('button'))

      const sheet = await findIncomeEditSheet()
      expect(amountField().value).toBe('200')
      expect(shownCurrency(sheet, 'EUR')).toBeTruthy()
    })

    it('opens the spending sheet, as before, when a spending row is tapped in a month with income', async () => {
      await renderBudget({ months: [octoberWithSalary] })
      const list = await findEntryList()

      fireEvent.click(within(rowAt(list, 0)).getByRole('button'))

      expect(await findDetailsSheet(coffeeCategory)).toBeTruthy()
      expect(amountField().value).toBe('120')
      expect(screen.queryByRole('dialog', { name: translated('en', 'budget.income.editTitle') })).toBeNull()
    })

    it('opens the spending sheet of the food entry from the same list', async () => {
      await renderBudget({ months: [octoberWithSalary] })
      const list = await findEntryList()

      fireEvent.click(within(rowAt(list, 1)).getByRole('button'))

      expect(await findDetailsSheet(personalFoodCategory)).toBeTruthy()
    })

    it('saves a changed income with PUT, kind Income and no category from the screen', async () => {
      const { fetchMock } = await renderBudget({
        months: [octoberWithSalary],
        answers: {
          [`PUT /api/personal/entries/${salaryIncomeEntry.id}`]: jsonAnswer(entryAnswerRow(salaryIncomeEntry)),
        },
      })
      const list = await findEntryList()
      fireEvent.click(within(rowAt(list, 2)).getByRole('button'))
      await findIncomeEditSheet()

      typeAmount('70000')
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'PUT', `/api/personal/entries/${salaryIncomeEntry.id}`)).toBe(1)
      })
      const body = sentBody(fetchMock, 'PUT', `/api/personal/entries/${salaryIncomeEntry.id}`)
      expect(body).toMatchObject({ kind: 'Income', amountMinor: 7000000 })
      expect(Object.keys(body)).not.toContain('categoryId')
    })
  })

  describe('the Left card and the category tiles', () => {
    it('count the income only in Left and Income and the spending only in Spent, never mixing the currencies', async () => {
      await renderBudget({ months: [octoberWithSalary] })

      const card = await findLeftCard()

      expect(amountTexts(card, leftAttribute)).toEqual([money(6488000, 'MKD')])
      expect(amountTexts(card, incomeAttribute)).toEqual([signedMoney(6500000, 'MKD')])
      expect(amountTexts(card, spentAttribute)).toEqual([money(12000, 'MKD'), money(1250, 'EUR')])
    })

    it('list only the spending categories in the tiles, with no tile for income', async () => {
      await renderBudget({ months: [octoberWithSalary] })
      await findLeftCard()

      const tiles = within(categoryTotalsRegion()).getAllByRole('listitem')

      expect(tiles.map(readableText)).toEqual([`Coffee${money(12000, 'MKD')}`, `Food & drinks${money(1250, 'EUR')}`])
      expect(within(categoryTotalsRegion()).queryByText(translated('en', 'budget.income.listTitle'))).toBeNull()
    })

    it('show spending 0 in the currency of an income that has no spending when the month has incomes in two currencies', async () => {
      await renderBudget({ months: [octoberWithTwoIncomes] })

      const card = await findLeftCard()

      expect(amountTexts(card, spentAttribute)).toEqual([money(12000, 'MKD'), money(0, 'EUR')])
    })
  })

  describe('a month with income and no spending', () => {
    it.each(languages)('shows the income row and the Left card with Left equal to the income and Spent 0, with no calm "Nothing spent this month" text and no tiles (%s)', async (language) => {
      await renderBudget({ language, months: [incomeOnlyOctober] })

      const list = await findEntryList(language)
      const card = await findLeftCard(language)

      expect(within(list).getAllByRole('listitem')).toHaveLength(1)
      expect(titleOf(rowAt(list, 0))).toBe('Salary')
      expect(amountTexts(card, leftAttribute)).toEqual([money(6500000, 'MKD', language)])
      expect(amountTexts(card, spentAttribute)).toEqual([money(0, 'MKD', language)])
      expect(screen.queryByText(translated(language, 'budget.screen.emptyTitle'))).toBeNull()
      expect(screen.queryByRole('region', { name: translated(language, 'budget.screen.spentThisMonth') })).toBeNull()
      expect(screen.queryByRole('region', { name: translated(language, 'budget.screen.categoryTotals') })).toBeNull()
    })

    it('shows the zero as the Spent amount in the currency of the income, and no zero in the other currency', async () => {
      await renderBudget({ months: [incomeOnlyOctober] })
      const card = await findLeftCard()

      expect(amountTexts(card, spentAttribute)).toEqual([money(0, 'MKD')])
      expect(plainSpaces(document.body.textContent ?? '')).not.toContain(money(0, 'EUR'))
    })

    it('does not crash for an income in EUR only', async () => {
      await renderBudget({ months: [euroIncomeOnlyOctober] })

      const list = await findEntryList()
      const card = await findLeftCard()

      expect(readableText(amountOf(rowAt(list, 0)))).toBe(signedMoney(20000, 'EUR'))
      expect(titleOf(rowAt(list, 0))).toBe(translated('en', 'budget.income.listTitle'))
      expect(amountTexts(card, leftAttribute)).toEqual([money(20000, 'EUR')])
      expect(screen.queryByText(translated('en', 'budget.screen.emptyTitle'))).toBeNull()
    })

    it('keeps the "+" so the next entry can be added', async () => {
      await renderBudget({ months: [incomeOnlyOctober] })
      await findEntryList()

      expect(plusButton()).toBeTruthy()
    })

    it('opens the income sheet when the only row is tapped', async () => {
      await renderBudget({ months: [incomeOnlyOctober] })
      const list = await findEntryList()

      fireEvent.click(within(rowAt(list, 0)).getByRole('button'))

      expect(await findIncomeEditSheet()).toBeTruthy()
    })
  })

  describe('a month with no entries at all', () => {
    it('is unchanged: the empty text with no list', async () => {
      await renderBudget({ months: [{ month: '2026-10', entries: [], totals: [], incomeTotals: [] }] })

      expect(await screen.findByText(translated('en', 'budget.screen.emptyTitle'))).toBeTruthy()
      expect(screen.queryByRole('list', { name: translated('en', 'budget.screen.entries') })).toBeNull()
    })
  })
})
