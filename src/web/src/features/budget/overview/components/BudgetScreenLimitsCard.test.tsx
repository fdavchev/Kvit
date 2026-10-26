import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { endpoints } from '@/core/api/endpoints'
import { languages } from '@/core/i18n/language'
import {
  limitBarOf,
  limitFillOf,
  limitNameOf,
  limitRowButton,
  limitRowNamed,
  limitRowText,
  limitRows,
  findLimitsCard,
  hasTapHeightOfAtLeast44,
  queryLimitsCard,
  querySetLimitButton,
  renderBudgetWithLimits,
  setLimitButton,
  wholeMonthName,
} from '@/test/budgetLimitTestHelpers'
import {
  emptyOctoberWithALimit,
  octoberWithLimits,
  octoberWithLimitsInAnotherOrder,
  octoberWithMonthLimitSpent,
  octoberWithOnlyACoffeeLimit,
  octoberWithOnlyTheWholeMonthLimit,
  septemberWithLimits,
} from '@/test/budgetLimitTestData'
import { classesOf, money } from '@/test/budgetLeftCardTestHelpers'
import {
  budgetNow,
  categoryTotalsRegion,
  findEntryList,
  findTotalCard,
  monthButton,
  personalEntriesGet,
  renderBudget,
} from '@/test/budgetTestHelpers'
import { isBefore, plainSpaces } from '@/test/expenseTestHelpers'
import { emptyMonth, octoberMonth } from '@/test/personalEntryTestData'
import {
  archivedGymCategory,
  coffeeCategory,
  personalFoodCategory,
  personalGroceriesCategory,
  petCategory,
} from '@/test/personalCategoryTestData'
import { categoryListAnswer, personalCategoriesWithArchivedGet, personalCategoryLabel } from '@/test/personalCategoryTestHelpers'
import { findRepeatingRow } from '@/test/recurringIncomeTestHelpers'
import {
  networkFailureAnswer,
  neverAnswer,
  requestCount,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const foodLabel = personalCategoryLabel('en', personalFoodCategory)
const groceriesLabel = personalCategoryLabel('en', personalGroceriesCategory)

function failingThenAnswering(failure: AnswerFactory, success: AnswerFactory): AnswerFactory {
  let calls = 0
  return () => {
    calls += 1
    return calls === 1 ? failure() : success()
  }
}

describe('BudgetScreen, the "Limits" card', () => {
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

  describe('when it is shown', () => {
    it.each(languages)('shows a region headed "Limits" with a row for the whole month and one for each category limit (%s)', async (language) => {
      await renderBudgetWithLimits({ language, months: [octoberWithLimits] })

      const card = await findLimitsCard(language)

      expect(within(card).getByRole('heading', { level: 2, name: translated(language, 'budget.limits.title') })).toBeTruthy()
      expect(limitRows(card)).toHaveLength(5)
    })

    it('shows only the one-line "Set a limit" button, and no Limits region, when the month has no limit', async () => {
      await renderBudgetWithLimits({ months: [octoberMonth] })
      await findTotalCard()

      expect(queryLimitsCard()).toBeNull()
      expect(setLimitButton()).toBeTruthy()
      expect(screen.queryAllByRole('progressbar')).toHaveLength(0)
    })

    it('shows the "Set a limit" button in an empty month too, so a limit can be set before the first entry', async () => {
      await renderBudgetWithLimits({ months: [emptyMonth('2026-10')] })
      await screen.findByText(translated('en', 'budget.screen.emptyTitle'))

      expect(queryLimitsCard()).toBeNull()
      expect(setLimitButton()).toBeTruthy()
    })

    it('shows the card with its rows in an empty month that has a limit', async () => {
      await renderBudgetWithLimits({ months: [emptyOctoberWithALimit] })

      const card = await findLimitsCard()

      expect(limitRows(card).map((row) => limitRowText(row).amounts)).toEqual([`${money(0, 'MKD')} / ${money(2500000, 'MKD')}`])
    })

    it('shows a card with only the whole-month row when only that limit exists', async () => {
      await renderBudgetWithLimits({ months: [octoberWithOnlyTheWholeMonthLimit] })

      const card = await findLimitsCard()

      expect(limitRows(card).map((row) => limitRowText(row).name)).toEqual([wholeMonthName()])
    })

    it('shows a card with only category rows when there is no whole-month limit', async () => {
      await renderBudgetWithLimits({ months: [octoberWithOnlyACoffeeLimit] })

      const card = await findLimitsCard()

      expect(limitRows(card).map((row) => limitRowText(row).name)).toEqual(['Coffee'])
    })

    it('shows the "Set a limit" button inside the card as well, so another limit can be added', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })

      const card = await findLimitsCard()

      expect(within(card).getByRole('button', { name: translated('en', 'budget.limits.setLimit') })).toBeTruthy()
      expect(screen.getAllByRole('button', { name: translated('en', 'budget.limits.setLimit') })).toHaveLength(1)
    })

    it('goes away when another month without limits is picked, and comes back with the month that has them', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits, { ...octoberMonth, month: '2026-09' }] })
      await findLimitsCard()

      fireEvent.click(monthButton('Sep'))

      await waitFor(() => {
        expect(queryLimitsCard()).toBeNull()
        expect(setLimitButton()).toBeTruthy()
      })
      fireEvent.click(monthButton('Oct'))
      expect(await findLimitsCard()).toBeTruthy()
    })

    it('shows the limits that were in force in the month picked, as the server sends them', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits, septemberWithLimits] })
      await findLimitsCard()

      fireEvent.click(monthButton('Sep'))

      await waitFor(() => {
        expect(limitRows(queryLimitsCard() ?? document.body).map((row) => limitRowText(row).name)).toEqual([wholeMonthName(), 'Gym'])
      })
    })

    it('shows nothing of it while the month loads', async () => {
      await renderBudgetWithLimits({ answers: { [personalEntriesGet('2026-10')]: neverAnswer() } })

      await screen.findByRole('status')

      expect(queryLimitsCard()).toBeNull()
      expect(querySetLimitButton()).toBeNull()
    })

    it('shows nothing of it when the month fails to load', async () => {
      await renderBudgetWithLimits({ answers: { [personalEntriesGet('2026-10')]: networkFailureAnswer() } })

      await screen.findByText(translated('en', 'errors.network'))

      expect(queryLimitsCard()).toBeNull()
      expect(querySetLimitButton()).toBeNull()
    })
  })

  describe('where it sits', () => {
    it('comes after the total card and the Repeating income row, before the category tiles and the entries', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })

      const card = await findLimitsCard()

      expect(isBefore(await findTotalCard(), card)).toBe(true)
      expect(isBefore(await findRepeatingRow(), card)).toBe(true)
      expect(isBefore(card, categoryTotalsRegion())).toBe(true)
      expect(isBefore(card, await findEntryList())).toBe(true)
    })

    it('puts the "Set a limit" button in the same place when the month has no limit', async () => {
      await renderBudgetWithLimits({ months: [octoberMonth] })

      const button = await screen.findByRole('button', { name: translated('en', 'budget.limits.setLimit') })

      expect(isBefore(await findTotalCard(), button)).toBe(true)
      expect(isBefore(await findRepeatingRow(), button)).toBe(true)
      expect(isBefore(button, categoryTotalsRegion())).toBe(true)
      expect(isBefore(button, await findEntryList())).toBe(true)
    })
  })

  describe('what the rows show', () => {
    it('lists the whole month first and then the categories in the order the server sends', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })

      const card = await findLimitsCard()

      expect(limitRows(card).map((row) => limitRowText(row).name)).toEqual([wholeMonthName(), foodLabel, groceriesLabel, 'Coffee', 'Dog'])
    })

    it('keeps the order of the server and does not sort the categories itself', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimitsInAnotherOrder] })

      const card = await findLimitsCard()

      expect(limitRows(card).map((row) => limitRowText(row).name)).toEqual([wholeMonthName(), 'Dog', 'Coffee', groceriesLabel, foodLabel])
    })

    it.each(languages)('names the whole month and the categories in the language of the app (%s)', async (language) => {
      await renderBudgetWithLimits({ language, months: [octoberWithLimits] })

      const card = await findLimitsCard(language)

      expect(limitRows(card).map((row) => limitRowText(row).name)).toEqual([
        wholeMonthName(language),
        personalCategoryLabel(language, personalFoodCategory),
        personalCategoryLabel(language, personalGroceriesCategory),
        'Coffee',
        'Dog',
      ])
    })

    it('shows the emoji of the category next to its name and none inside the name', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })

      const card = await findLimitsCard()
      const coffeeRow = limitRowNamed(card, 'Coffee')

      expect(coffeeRow.textContent).toContain(coffeeCategory.emoji)
      expect(limitNameOf(coffeeRow).textContent).toBe('Coffee')
      expect(limitRowNamed(card, 'Dog').textContent).toContain(petCategory.emoji)
    })

    it('shows the name of a category that has been archived, from the list that includes archived ones', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits, septemberWithLimits] })
      await findLimitsCard()

      fireEvent.click(monthButton('Sep'))

      await waitFor(() => {
        expect(limitRowNamed(queryLimitsCard() ?? document.body, 'Gym')).toBeTruthy()
      })
      expect(limitRowNamed(queryLimitsCard() ?? document.body, 'Gym').textContent).toContain(archivedGymCategory.emoji)
    })

    it.each(languages)('shows "spent / limit" in MKD for every row (%s)', async (language) => {
      await renderBudgetWithLimits({ language, months: [octoberWithLimits] })

      const card = await findLimitsCard(language)

      expect(limitRows(card).map((row) => limitRowText(row).amounts)).toEqual([
        `${money(1750000, 'MKD', language)} / ${money(2500000, 'MKD', language)}`,
        `${money(720000, 'MKD', language)} / ${money(1000000, 'MKD', language)}`,
        `${money(900000, 'MKD', language)} / ${money(1000000, 'MKD', language)}`,
        `${money(480000, 'MKD', language)} / ${money(450000, 'MKD', language)}`,
        `${money(500000, 'MKD', language)} / ${money(500000, 'MKD', language)}`,
      ])
    })

    it('draws the spent amount and the limit with KvitAmount: a text for screen readers and a rolling number hidden from them', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })

      const card = await findLimitsCard()

      for (const row of limitRows(card)) {
        const amounts = row.querySelector<HTMLElement>('[data-limit-amounts]')
        expect(amounts?.querySelectorAll('.sr-only')).toHaveLength(2)
        expect(amounts?.querySelectorAll('[aria-hidden="true"]').length).toBeGreaterThanOrEqual(2)
      }
    })
  })

  describe('the bar', () => {
    it.each([
      [null, 1750000, 2500000, 'ok', 'scaleX(0.7)'],
      [foodLabel, 720000, 1000000, 'ok', 'scaleX(0.72)'],
      [groceriesLabel, 900000, 1000000, 'near', 'scaleX(0.9)'],
      ['Coffee', 450000, 450000, 'over', 'scaleX(1)'],
      ['Dog', 500000, 500000, 'near', 'scaleX(1)'],
    ])('on the row %s has the value %s of %s, the state "%s" and a fill scaled to %s', async (name, valueNow, valueMax, state, transform) => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const card = await findLimitsCard()

      const row = limitRowNamed(card, name ?? wholeMonthName())
      const bar = limitBarOf(row)

      expect(bar.getAttribute('aria-valuemin')).toBe('0')
      expect(bar.getAttribute('aria-valuenow')).toBe(String(valueNow))
      expect(bar.getAttribute('aria-valuemax')).toBe(String(valueMax))
      expect(bar.getAttribute('data-limit-state')).toBe(state)
      expect(limitFillOf(row).style.transform).toBe(transform)
    })

    it('names every bar like its row, for screen readers', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const card = await findLimitsCard()

      expect(within(card).getAllByRole('progressbar').map((bar) => bar.getAttribute('aria-label'))).toEqual([
        wholeMonthName(),
        foodLabel,
        groceriesLabel,
        'Coffee',
        'Dog',
      ])
    })

    it('keeps the fill scaled to 1 when the spending is over the limit', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const card = await findLimitsCard()

      expect(limitFillOf(limitRowNamed(card, 'Coffee')).style.transform).toBe('scaleX(1)')
    })

    it('shows an empty fill when nothing is spent', async () => {
      await renderBudgetWithLimits({ months: [emptyOctoberWithALimit] })
      const card = await findLimitsCard()

      const [row] = limitRows(card)

      expect(limitBarOf(row).getAttribute('aria-valuenow')).toBe('0')
      expect(limitBarOf(row).getAttribute('data-limit-state')).toBe('ok')
      expect(limitFillOf(row).style.transform).toBe('scaleX(0)')
    })

    it.each([
      [333300, 'ok'],
      [799900, 'ok'],
      [1000000, 'near'],
      [1000100, 'over'],
    ])('scales the fill to the exact ratio of spending %s to a limit of 1000000 deni, with the state "%s"', async (spentMinor, state) => {
      await renderBudgetWithLimits({ months: [octoberWithMonthLimitSpent(spentMinor)] })
      const card = await findLimitsCard()

      const [row] = limitRows(card)

      expect(limitFillOf(row).style.transform).toBe(`scaleX(${Math.min(spentMinor, 1000000) / 1000000})`)
      expect(limitBarOf(row).getAttribute('data-limit-state')).toBe(state)
    })

    it('does not pin the width of the fill, because the transform draws the length', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const card = await findLimitsCard()

      for (const row of limitRows(card)) {
        expect(limitFillOf(row).style.width).toBe('')
      }
    })

    it('draws the fill from its left edge, so it grows to the right', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const card = await findLimitsCard()

      for (const row of limitRows(card)) {
        expect(classesOf(limitFillOf(row))).toContain('origin-left')
      }
    })

    it('animates only the transform of the fill, and not under reduced motion', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const card = await findLimitsCard()

      for (const row of limitRows(card)) {
        const classes = classesOf(limitFillOf(row))
        expect(classes.filter((name) => name.startsWith('transition'))).toEqual(['transition-transform'])
        expect(classes).toContain('motion-reduce:transition-none')
      }
    })
    it.each([
      [799900, 'ok'],
      [800000, 'near'],
      [1000000, 'near'],
      [1000100, 'over'],
    ])('is in the state of spending %s of a limit of 1000000 deni: %s (green under 80 %%, amber from 80 %% up to the limit, red over it)', async (spentMinor, state) => {
      await renderBudgetWithLimits({ months: [octoberWithMonthLimitSpent(spentMinor)] })
      const card = await findLimitsCard()

      const [row] = limitRows(card)

      expect(limitBarOf(row).getAttribute('data-limit-state')).toBe(state)
    })

    it('uses the positive colour under 80 %, the negative colour when over, and a colour of its own in between', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const card = await findLimitsCard()

      const ok = classesOf(limitFillOf(limitRowNamed(card, foodLabel)))
      const near = classesOf(limitFillOf(limitRowNamed(card, groceriesLabel)))
      const over = classesOf(limitFillOf(limitRowNamed(card, 'Coffee')))

      expect(ok).toContain('bg-positive')
      expect(over).toContain('bg-negative')
      expect(near).not.toContain('bg-positive')
      expect(near).not.toContain('bg-negative')
      expect(near).not.toEqual(ok)
    })
  })

  describe('the "N over" line', () => {
    it.each(languages)('shows the amount over under a row that is over its limit (%s)', async (language) => {
      await renderBudgetWithLimits({ language, months: [octoberWithLimits] })
      const card = await findLimitsCard(language)

      const coffee = limitRowText(limitRowNamed(card, 'Coffee'))

      expect(coffee.over).toBe(translated(language, 'budget.limits.over', { amount: money(30000, 'MKD', language) }))
    })

    it('shows it on no other row, also not on the row that has exactly reached its limit', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const card = await findLimitsCard()

      const overLines = limitRows(card).map((row) => limitRowText(row).over)

      expect(overLines).toEqual([null, null, null, translated('en', 'budget.limits.over', { amount: money(30000, 'MKD') }), null])
    })

    it('writes the line in the negative colour', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const card = await findLimitsCard()

      const line = limitRowNamed(card, 'Coffee').querySelector<HTMLElement>('[data-limit-over]')

      expect(classesOf(line ?? document.body)).toContain('text-negative')
    })
  })

  describe('the note about converted euros', () => {
    it.each(languages)('says how many euros were converted under the bar of a row that includes some (%s)', async (language) => {
      await renderBudgetWithLimits({ language, months: [octoberWithLimits] })
      const card = await findLimitsCard(language)

      const note = limitRowText(limitRowNamed(card, personalCategoryLabel(language, personalGroceriesCategory))).converted

      expect(note).toBe(translated(language, 'budget.limits.includesConverted', { amount: money(2000, 'EUR', language) }))
    })

    it('shows it on no row that includes no euros', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const card = await findLimitsCard()

      expect(limitRows(card).map((row) => limitRowText(row).converted !== null)).toEqual([false, false, true, false, false])
    })

    it('comes after the bar in the row', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const card = await findLimitsCard()
      const row = limitRowNamed(card, groceriesLabel)

      const note = row.querySelector<HTMLElement>('[data-limit-converted]')

      expect(isBefore(limitBarOf(row), note ?? document.body)).toBe(true)
    })
  })

  describe('tapping', () => {
    it('gives every row exactly one button, at least 44 px high', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const card = await findLimitsCard()

      for (const row of limitRows(card)) {
        expect(within(row).getAllByRole('button')).toHaveLength(1)
        expect(hasTapHeightOfAtLeast44(limitRowButton(row))).toBe(true)
      }
    })

    it('makes the "Set a limit" button at least 44 px high, alone and inside the card', async () => {
      await renderBudgetWithLimits({ months: [octoberMonth] })
      await findTotalCard()
      expect(hasTapHeightOfAtLeast44(setLimitButton())).toBe(true)
    })

    it('makes the "Set a limit" button inside the card at least 44 px high', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const card = await findLimitsCard()

      expect(hasTapHeightOfAtLeast44(within(card).getByRole('button', { name: translated('en', 'budget.limits.setLimit') }))).toBe(true)
    })

    it('lets long category names wrap instead of widening the row', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const card = await findLimitsCard()

      expect(classesOf(limitNameOf(limitRowNamed(card, 'Coffee')))).toContain('break-words')
    })
  })

  describe('the category tiles', () => {
    it('stay as they were: no bar, no limit text, nothing to tap', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      await findLimitsCard()

      const tiles = categoryTotalsRegion()

      expect(within(tiles).queryAllByRole('button')).toHaveLength(0)
      expect(within(tiles).queryAllByRole('progressbar')).toHaveLength(0)
      expect(tiles.textContent).not.toMatch(/limit/i)
      expect(screen.queryByText(/tap to set/i)).toBeNull()
    })

    it('open no sheet when a tile is tapped, also not for a category that has no limit', async () => {
      await renderBudgetWithLimits({ months: [octoberWithOnlyTheWholeMonthLimit] })
      await findLimitsCard()
      const tiles = categoryTotalsRegion()

      fireEvent.click(within(tiles).getByText('Coffee'))

      expect(screen.queryByRole('dialog')).toBeNull()
    })

    it('show the same amounts as without limits', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      await findLimitsCard()

      const tileText = plainSpaces(categoryTotalsRegion().textContent ?? '')

      expect(tileText).toContain(money(12000, 'MKD'))
      expect(tileText).toContain(money(1250, 'EUR'))
    })
  })

  describe('the names of the categories', () => {
    it('asks for the list that includes archived categories only when a category limit is shown', async () => {
      const { fetchMock } = await renderBudgetWithLimits({ months: [octoberWithOnlyTheWholeMonthLimit] })
      await findLimitsCard()

      expect(requestCount(fetchMock, 'GET', endpoints.personalCategoriesWithArchived)).toBe(0)
    })

    it('asks for it once when category limits are shown', async () => {
      const { fetchMock } = await renderBudgetWithLimits({ months: [octoberWithLimits] })
      await findLimitsCard()

      expect(requestCount(fetchMock, 'GET', endpoints.personalCategoriesWithArchived)).toBe(1)
    })

    it('shows no bar while the names are not known yet', async () => {
      await renderBudget({
        months: [octoberWithLimits],
        answers: { [personalCategoriesWithArchivedGet]: neverAnswer() },
      })
      await findTotalCard()

      expect(screen.queryAllByRole('progressbar')).toHaveLength(0)
      expect(screen.queryByText(wholeMonthName())).toBeNull()
    })

    it('shows the error and a Retry button when the names cannot be loaded, and the rows after Retry', async () => {
      await renderBudget({
        months: [octoberWithLimits],
        answers: {
          [personalCategoriesWithArchivedGet]: failingThenAnswering(
            networkFailureAnswer(),
            categoryListAnswer(() => [personalFoodCategory, personalGroceriesCategory, coffeeCategory, petCategory]),
          ),
        },
      })
      await findTotalCard()

      fireEvent.click(await screen.findByRole('button', { name: translated('en', 'common.retry') }))

      await waitFor(() => {
        expect(limitRows(screen.getByRole('region', { name: translated('en', 'budget.limits.title') }))).toHaveLength(5)
      })
    })
  })
})
