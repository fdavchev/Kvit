import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import {
  mergedDinnerOutEntry,
  missingShareExpenseId,
  octoberWithEntryOnTheFirstMergedWithShareOfSeptember,
  octoberWithMergedDinnerAndCoffee,
  octoberWithMergedEntryWhoseShareIsMissing,
  octoberWithMergedUntitledGroceries,
  octoberWithOnlyTheDinnerMerged,
  septemberWithEntryOnThe30thMergedWithShareOfOctober,
} from '@/test/budgetDuplicateTestData'
import {
  entryRowButton,
  entryRowContaining,
  mergedFallbackLabel,
  mergedLabel,
  opacityPercentOf,
} from '@/test/budgetDuplicateTestHelpers'
import { amountTexts, leftAttribute, money, readableText } from '@/test/budgetLeftCardTestHelpers'
import { budgetNow, findEntryList, findTotalCard, renderBudget } from '@/test/budgetTestHelpers'
import { compositeOver, colorOfToken, contrastOfColors, type ThemeName } from '@/test/indexCssColors'
import { personalFoodCategory } from '@/test/personalCategoryTestData'
import { personalCategoryLabel } from '@/test/personalCategoryTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const themes: ThemeName[] = ['light', 'dark']

describe('BudgetScreen, a merged entry in the list', () => {
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

  describe('the label', () => {
    it.each(languages)('says "Merged with {expense} · {group}" with the names of the group share of the same answer (%s)', async (language) => {
      await renderBudget({ language, months: [octoberWithOnlyTheDinnerMerged] })
      const list = await findEntryList(language)

      const row = entryRowContaining(list, 'Dinner out')

      expect(readableText(row)).toContain(mergedLabel(language, 'Dinner', 'Flat'))
    })

    it('puts a middle dot between the expense and the group', async () => {
      await renderBudget({ months: [octoberWithOnlyTheDinnerMerged] })
      const list = await findEntryList()

      expect(readableText(entryRowContaining(list, 'Dinner out'))).toContain('Merged with Dinner · Flat')
    })

    it('names the expense by its category when the group expense has no title', async () => {
      await renderBudget({ months: [octoberWithMergedUntitledGroceries] })
      const list = await findEntryList()

      const merged = entryRowContaining(list, 'Merged with')

      expect(readableText(merged)).toContain(mergedLabel('en', translated('en', 'categories.groceries'), 'Flat'))
    })

    it('is part of the name of the row button, so a screen reader says it too', async () => {
      await renderBudget({ months: [octoberWithOnlyTheDinnerMerged] })
      const list = await findEntryList()

      const button = entryRowButton(entryRowContaining(list, 'Dinner out'))

      expect(button.textContent).toContain(mergedLabel('en', 'Dinner', 'Flat'))
    })

    it('is on the merged row only, not on the other rows of the month', async () => {
      await renderBudget({ months: [octoberWithMergedUntitledGroceries] })
      const list = await findEntryList()
      const rows = within(list).getAllByRole('listitem')

      const labelled = rows.filter((row) => readableText(row).includes('Merged with'))

      expect(rows).toHaveLength(2)
      expect(labelled).toHaveLength(1)
      expect(readableText(entryRowContaining(list, 'Dinner out'))).not.toContain('Merged')
    })

    it('is not on any row of a month without merged entries', async () => {
      await renderBudget({ months: [octoberWithMergedDinnerAndCoffee] })
      const list = await findEntryList()

      const coffeeRow = entryRowContaining(list, 'Coffee')

      expect(readableText(coffeeRow)).not.toContain('Merged')
    })
  })

  describe('when the group expense is not in the answer of the month', () => {
    it.each(languages)('says "Merged with a group expense" for an entry of the 1st whose expense belongs to the month before (%s)', async (language) => {
      await renderBudget({ language, months: [octoberWithEntryOnTheFirstMergedWithShareOfSeptember] })
      const list = await findEntryList(language)

      const row = entryRowContaining(list, 'Dinner out')

      expect(readableText(row)).toContain(mergedFallbackLabel(language))
    })

    it('says "Merged with a group expense" for an entry of the 30th whose expense belongs to the month after', async () => {
      await renderBudget({
        startAt: routes.budgetOf('2026-09'),
        months: [septemberWithEntryOnThe30thMergedWithShareOfOctober],
      })
      const list = await findEntryList()

      const row = entryRowContaining(list, 'Dinner out')

      expect(readableText(row)).toContain(mergedFallbackLabel())
    })

    it('shows the fallback text without a group name and without a middle dot', async () => {
      await renderBudget({ months: [octoberWithEntryOnTheFirstMergedWithShareOfSeptember] })
      const list = await findEntryList()

      const row = readableText(entryRowContaining(list, 'Dinner out'))

      expect(row).not.toContain('Flat')
      expect(row).not.toContain('Merged with Dinner')
    })

    it('fails with an error that names the expense id when an entry in the middle of the month is merged with an expense that is not in the answer', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})

      await renderBudget({ months: [octoberWithMergedEntryWhoseShareIsMissing] })

      await waitFor(() => {
        expect(document.body.textContent).toContain(missingShareExpenseId)
      })
      expect(screen.queryByRole('list', { name: translated('en', 'budget.screen.entries') })).toBeNull()
    })
  })

  describe('the grey look', () => {
    it('lowers the opacity of the merged row, and no other row', async () => {
      await renderBudget({ months: [octoberWithMergedUntitledGroceries] })
      const list = await findEntryList()
      const merged = entryRowContaining(list, 'Merged with')
      const other = entryRowContaining(list, 'Dinner out')

      expect(opacityPercentOf(merged)).not.toBeNull()
      expect(opacityPercentOf(merged)).toBeLessThan(100)
      expect(opacityPercentOf(other)).toBeNull()
    })

    it.each(themes)('keeps the text of a merged row at 4.5:1 or more against the card in the %s theme, for the title and for the grey line under it', async (theme) => {
      await renderBudget({ months: [octoberWithMergedUntitledGroceries] })
      const list = await findEntryList()
      const opacity = opacityPercentOf(entryRowContaining(list, 'Merged with'))
      const card = colorOfToken('card', theme)

      for (const textToken of ['card-foreground', 'muted-foreground']) {
        const faded = compositeOver({ rgb: colorOfToken(textToken, theme), alpha: (opacity ?? 100) / 100 }, card)

        expect(contrastOfColors(faded, card), `${textToken} at ${opacity} % in ${theme}`).toBeGreaterThanOrEqual(4.5)
      }
    })

    it('keeps the title, the note, the day and the amount of the entry readable in the row', async () => {
      await renderBudget({ months: [octoberWithOnlyTheDinnerMerged] })
      const list = await findEntryList()

      const text = readableText(entryRowContaining(list, 'Dinner out'))

      expect(text).toContain(personalCategoryLabel('en', personalFoodCategory))
      expect(text).toContain('17 Oct')
      expect(text).toContain('Dinner out')
      expect(text).toContain(money(mergedDinnerOutEntry.amountMinor, 'MKD'))
    })
  })

  describe('opening it', () => {
    it('stays a button that opens the edit sheet of the entry, like any other row', async () => {
      await renderBudget({ months: [octoberWithOnlyTheDinnerMerged] })
      const list = await findEntryList()

      fireEvent.click(entryRowButton(entryRowContaining(list, 'Dinner out')))

      expect(await screen.findByRole('dialog', { name: personalCategoryLabel('en', personalFoodCategory) })).toBeTruthy()
    })
  })

  describe('the totals', () => {
    it('does not add the amount of a merged entry to the totals again, because the totals of the answer already leave it out', async () => {
      await renderBudget({ months: [octoberWithMergedDinnerAndCoffee] })

      const card = await findTotalCard()

      expect(amountTexts(card, leftAttribute)).toEqual([money(12000 + 95000, 'MKD')])
    })

    it('still lists the merged entry, with its own amount, in the entries', async () => {
      await renderBudget({ months: [octoberWithMergedDinnerAndCoffee] })
      const list = await findEntryList()

      expect(readableText(entryRowContaining(list, 'Dinner out'))).toContain(money(100000, 'MKD'))
    })
  })
})
