import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { classesOf } from '@/test/budgetLeftCardTestHelpers'
import { nearWordsOf, nearWordsText, requireNearWords, rowButtonDescribedByNearWords } from '@/test/budgetLimitNearTestHelpers'
import { octoberWithLimits, octoberWithMonthLimitSpent } from '@/test/budgetLimitTestData'
import {
  findLimitsCard,
  limitBarOf,
  limitConvertedNoteOf,
  limitOverOf,
  limitRowNamed,
  limitRows,
  renderBudgetWithLimits,
  wholeMonthName,
} from '@/test/budgetLimitTestHelpers'
import { budgetNow } from '@/test/budgetTestHelpers'
import { isBefore } from '@/test/expenseTestHelpers'
import { personalCategoryLabel } from '@/test/personalCategoryTestHelpers'
import { personalFoodCategory, personalGroceriesCategory } from '@/test/personalCategoryTestData'
import { barCases, barLimitMinor, expectBarMoves, expectBarShows } from '@/test/progressBarTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const foodLabel = personalCategoryLabel('en', personalFoodCategory)
const groceriesLabel = personalCategoryLabel('en', personalGroceriesCategory)

describe('BudgetScreen, the Limits card in words: "Near the limit"', () => {
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

  describe('which rows show the words', () => {
    it('shows them on the rows in the near state: 90 % of a limit and a limit that is exactly reached', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const card = await findLimitsCard()

      const rowsWithWords = limitRows(card).filter((row) => nearWordsOf(row) !== null)

      expect(rowsWithWords.map((row) => limitBarOf(row).getAttribute('aria-label'))).toEqual([groceriesLabel, 'Dog'])
    })

    it('shows nothing extra on the rows that are ok', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const card = await findLimitsCard()

      for (const name of [wholeMonthName(), foodLabel]) {
        const row = limitRowNamed(card, name)
        expect(nearWordsOf(row)).toBeNull()
        expect(row.textContent).not.toContain('Near the limit')
        expect(limitOverOf(row)).toBeNull()
      }
    })

    it('keeps only the "N over" line on a row that is over, and adds no words', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const card = await findLimitsCard()

      const row = limitRowNamed(card, 'Coffee')

      expect(nearWordsOf(row)).toBeNull()
      expect(row.textContent).not.toContain('Near the limit')
      expect(limitOverOf(row)).not.toBeNull()
    })

    it('shows the words without an "N over" line on a row that has exactly reached its limit', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const card = await findLimitsCard()

      const row = limitRowNamed(card, 'Dog')

      expect(nearWordsOf(row)).not.toBeNull()
      expect(limitOverOf(row)).toBeNull()
    })

    it.each(barCases)('shows the words on a row with $label: $state', async ({ spentMinor, state }) => {
      await renderBudgetWithLimits({ months: [octoberWithMonthLimitSpent(spentMinor)] })
      const [row] = limitRows(await findLimitsCard())

      expect(nearWordsOf(row) !== null).toBe(state === 'near')
      expect(limitOverOf(row) !== null).toBe(state === 'over')
    })
  })

  describe('how the words look', () => {
    it('writes "Near the limit" in English', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const card = await findLimitsCard()

      expect(requireNearWords(limitRowNamed(card, groceriesLabel)).textContent).toBe('Near the limit')
    })

    it('writes "Близу до лимитот" in Macedonian', async () => {
      await renderBudgetWithLimits({ language: 'mk', months: [octoberWithLimits] })
      const card = await findLimitsCard('mk')

      const row = limitRows(card).find((candidate) => nearWordsOf(candidate, 'mk') !== null)

      expect(requireNearWords(row ?? document.body, 'mk').textContent).toBe('Близу до лимитот')
      expect(nearWordsText('mk')).toBe(translated('mk', 'budget.limits.nearWords'))
    })

    it('writes them in the amber warning colour, not in red', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const card = await findLimitsCard()

      const classes = classesOf(requireNearWords(limitRowNamed(card, groceriesLabel)))

      expect(classes).toContain('text-warning')
      expect(classes).not.toContain('text-negative')
    })

    it('puts them where the red "N over" line stands: under the bar and before the note about converted euros', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const card = await findLimitsCard()
      const row = limitRowNamed(card, groceriesLabel)
      const words = requireNearWords(row)
      const converted = limitConvertedNoteOf(row)

      expect(isBefore(limitBarOf(row), words)).toBe(true)
      expect(converted).not.toBeNull()
      expect(isBefore(words, converted ?? words)).toBe(true)
    })
  })

  describe('what screen readers hear', () => {
    it.each(languages)('includes the words in the description of the button of a near row (%s)', async (language) => {
      await renderBudgetWithLimits({ language, months: [octoberWithLimits] })
      const card = await findLimitsCard(language)

      const nearRows = limitRows(card).filter((row) => nearWordsOf(row, language) !== null)

      expect(nearRows).toHaveLength(2)
      for (const row of nearRows) {
        expect(rowButtonDescribedByNearWords(row, language)).toBe(true)
      }
    })

    it('leaves the words out of the description of a row that is ok or over', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const card = await findLimitsCard()

      for (const name of [wholeMonthName(), foodLabel, 'Coffee']) {
        expect(rowButtonDescribedByNearWords(limitRowNamed(card, name))).toBe(false)
      }
    })
  })

  describe('the bar, run against the same table as the plan bar', () => {
    it.each(barCases)('shows the bar of $label in the state "$state"', async ({ spentMinor, state }) => {
      await renderBudgetWithLimits({ months: [octoberWithMonthLimitSpent(spentMinor)] })
      const [row] = limitRows(await findLimitsCard())

      expectBarShows(limitBarOf(row), spentMinor, barLimitMinor, state)
    })

    it('draws the bar from the left edge, animating only the transform, and not under reduced motion', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const card = await findLimitsCard()

      for (const row of limitRows(card)) {
        expectBarMoves(limitBarOf(row))
      }
    })
  })
})
