import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import {
  categoryLimitPut,
  findChooseSheet,
  findLimitSheet,
  findLimitsCard,
  limitAmountField,
  limitRowText,
  limitRows,
  monthLimitPut,
  monthThatChanges,
  queryRemoveLimitButton,
  removeLimitButton,
  renderBudgetWithLimits,
  saveLimitButton,
  setLimitButton,
  typeLimit,
  wholeMonthName,
} from '@/test/budgetLimitTestHelpers'
import { octoberWithLimits, octoberWithOnlyACoffeeLimit } from '@/test/budgetLimitTestData'
import { budgetNow, findTotalCard, personalEntriesGet } from '@/test/budgetTestHelpers'
import { dismissSheetByTappingBackdrop } from '@/test/expenseFormTestHelpers'
import { octoberMonth } from '@/test/personalEntryTestData'
import {
  archivedGymCategory,
  coffeeCategory,
  personalFoodCategory,
  personalGroceriesCategory,
} from '@/test/personalCategoryTestData'
import { personalCategoryLabel } from '@/test/personalCategoryTestHelpers'
import { catchUpUrl } from '@/test/recurringIncomeTestData'
import { requestsOf } from '@/test/requestTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

function expectButtonsInOrder(sheet: HTMLElement, names: readonly string[]): void {
  const expected = names.map((name) => within(sheet).getByRole('button', { name }))
  const all = within(sheet).getAllByRole('button')

  expect(all).toHaveLength(expected.length)
  expected.forEach((button, index) => {
    expect(all[index]).toBe(button)
  })
}

async function settle(): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve, 50))
}

describe('BudgetScreen, choosing what to set a limit for', () => {
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

  describe('the choice sheet', () => {
    it.each(languages)('opens from "Set a limit" and offers the whole month and then every active category in the order of the category list (%s)', async (language) => {
      await renderBudgetWithLimits({ language, months: [octoberMonth] })
      await findTotalCard(language)

      fireEvent.click(setLimitButton(language))
      const sheet = await findChooseSheet(language)

      expectButtonsInOrder(sheet, [
        wholeMonthName(language),
        personalCategoryLabel(language, personalFoodCategory),
        personalCategoryLabel(language, personalGroceriesCategory),
        'Coffee',
        'Dog',
      ])
    })

    it('does not offer an archived category, a new category or the editing of categories', async () => {
      await renderBudgetWithLimits({ months: [octoberMonth] })
      await findTotalCard()

      fireEvent.click(setLimitButton())
      const sheet = await findChooseSheet()

      expect(within(sheet).queryByRole('button', { name: archivedGymCategory.name })).toBeNull()
      expect(within(sheet).queryByRole('button', { name: translated('en', 'budget.categories.new') })).toBeNull()
      expect(within(sheet).queryByRole('button', { name: translated('en', 'budget.categories.editMine') })).toBeNull()
    })

    it('opens from the "Set a limit" button inside the card as well', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const card = await findLimitsCard()

      fireEvent.click(within(card).getByRole('button', { name: translated('en', 'budget.limits.setLimit') }))

      expect(await findChooseSheet()).toBeTruthy()
    })

    it('sends nothing when it is dismissed, and brings the focus back to the "Set a limit" button', async () => {
      const { fetchMock } = await renderBudgetWithLimits({ months: [octoberMonth] })
      await findTotalCard()
      const button = setLimitButton()
      button.focus()
      fireEvent.click(button)
      await findChooseSheet()

      dismissSheetByTappingBackdrop()

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      await settle()
      expect(document.activeElement).toBe(button)
      const writes = fetchMock.mock.calls.filter(([input, init]) => (init?.method ?? 'GET') !== 'GET' && String(input) !== catchUpUrl)
      expect(writes).toHaveLength(0)
    })
  })

  describe('what happens after a choice', () => {
    it('replaces the choice sheet with the limit sheet of the whole month, empty, without a Remove limit button', async () => {
      await renderBudgetWithLimits({ months: [octoberMonth] })
      await findTotalCard()
      fireEvent.click(setLimitButton())

      fireEvent.click(within(await findChooseSheet()).getByRole('button', { name: wholeMonthName() }))
      const sheet = await findLimitSheet(wholeMonthName())

      expect(limitAmountField(sheet).value).toBe('')
      expect(queryRemoveLimitButton(sheet)).toBeNull()
      await waitFor(() => {
        expect(screen.queryByRole('dialog', { name: translated('en', 'budget.limits.chooseTitle') })).toBeNull()
      })
    })

    it('replaces the choice sheet with the limit sheet of the category that was tapped', async () => {
      await renderBudgetWithLimits({ months: [octoberMonth] })
      await findTotalCard()
      fireEvent.click(setLimitButton())

      fireEvent.click(within(await findChooseSheet()).getByRole('button', { name: 'Coffee' }))

      expect(await findLimitSheet('Coffee')).toBeTruthy()
    })

    it('opens a category that already has a limit with that limit typed in and a Remove limit button', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const card = await findLimitsCard()
      fireEvent.click(within(card).getByRole('button', { name: translated('en', 'budget.limits.setLimit') }))

      fireEvent.click(within(await findChooseSheet()).getByRole('button', { name: 'Coffee' }))
      const sheet = await findLimitSheet('Coffee')

      expect(limitAmountField(sheet).value).toBe('4500')
      expect(removeLimitButton(sheet)).toBeTruthy()
    })

    it('puts the cursor in the amount field at once, so no tap is needed to start typing', async () => {
      await renderBudgetWithLimits({ months: [octoberMonth] })
      await findTotalCard()
      fireEvent.click(setLimitButton())
      fireEvent.click(within(await findChooseSheet()).getByRole('button', { name: 'Coffee' }))
      const sheet = await findLimitSheet('Coffee')

      await waitFor(() => {
        expect(document.activeElement).toBe(limitAmountField(sheet))
      })
    })
  })

  describe('the first limit', () => {
    it('is set with three taps and one typed amount: Set a limit, the category, Save', async () => {
      const month = monthThatChanges(octoberMonth, octoberWithOnlyACoffeeLimit)
      const { fetchMock } = await renderBudgetWithLimits({
        months: [octoberMonth],
        answers: {
          [personalEntriesGet('2026-10')]: month.monthAnswer,
          [categoryLimitPut(coffeeCategory.id)]: month.acceptChange,
        },
      })
      await findTotalCard()
      let taps = 0

      fireEvent.click(setLimitButton())
      taps += 1
      fireEvent.click(within(await findChooseSheet()).getByRole('button', { name: 'Coffee' }))
      taps += 1
      const sheet = await findLimitSheet('Coffee')
      typeLimit(sheet, '4500')
      fireEvent.click(saveLimitButton(sheet))
      taps += 1

      expect(taps).toBe(3)
      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', `/api/personal/limits/categories/${coffeeCategory.id}`)).toHaveLength(1)
      })
      expect(requestsOf(fetchMock, 'PUT', `/api/personal/limits/categories/${coffeeCategory.id}`)[0].body).toEqual({ limitMinor: 450000 })
    })

    it('shows the new Limits card with the limit once it is saved', async () => {
      const month = monthThatChanges(octoberMonth, octoberWithOnlyACoffeeLimit)
      await renderBudgetWithLimits({
        months: [octoberMonth],
        answers: {
          [personalEntriesGet('2026-10')]: month.monthAnswer,
          [categoryLimitPut(coffeeCategory.id)]: month.acceptChange,
        },
      })
      await findTotalCard()
      fireEvent.click(setLimitButton())
      fireEvent.click(within(await findChooseSheet()).getByRole('button', { name: 'Coffee' }))
      const sheet = await findLimitSheet('Coffee')

      typeLimit(sheet, '4500')
      fireEvent.click(saveLimitButton(sheet))

      const card = await findLimitsCard()
      expect(limitRows(card).map((row) => limitRowText(row).name)).toEqual(['Coffee'])
      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
    })

    it('sets the limit of the whole month with PUT /api/personal/limits/month', async () => {
      const month = monthThatChanges(octoberMonth, octoberWithLimits)
      const { fetchMock } = await renderBudgetWithLimits({
        months: [octoberMonth],
        answers: {
          [personalEntriesGet('2026-10')]: month.monthAnswer,
          [monthLimitPut]: month.acceptChange,
        },
      })
      await findTotalCard()
      fireEvent.click(setLimitButton())
      fireEvent.click(within(await findChooseSheet()).getByRole('button', { name: wholeMonthName() }))
      const sheet = await findLimitSheet(wholeMonthName())

      typeLimit(sheet, '25000')
      fireEvent.click(saveLimitButton(sheet))

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', '/api/personal/limits/month')).toHaveLength(1)
      })
      expect(requestsOf(fetchMock, 'PUT', '/api/personal/limits/month')[0].body).toEqual({ limitMinor: 2500000 })
      expect(await findLimitsCard()).toBeTruthy()
    })
  })
})
