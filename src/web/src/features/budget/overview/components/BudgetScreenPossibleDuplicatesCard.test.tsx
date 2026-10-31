import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import {
  duplicatePair,
  octoberWithOnlyTheDinnerPair,
  octoberWithOnlyTheGroceriesPair,
  octoberWithOnlyTheTaxiPair,
  octoberWithTwoPairs,
  taxiRideEntry,
} from '@/test/budgetDuplicateTestData'
import {
  cardText,
  dinnerSentence,
  duplicateCardName,
  findDuplicateCard,
  groceriesSentence,
  newButton,
  queryDuplicateCard,
  sameButton,
  taxiSentence,
} from '@/test/budgetDuplicateTestHelpers'
import { hasTapHeightOfAtLeast44 } from '@/test/budgetLimitTestHelpers'
import {
  budgetNow,
  findTotalCard,
  monthButton,
  monthStrip,
  personalEntriesGet,
  renderBudget,
} from '@/test/budgetTestHelpers'
import { isBefore } from '@/test/expenseTestHelpers'
import { euroTaxiShare, octoberWithGroupShares } from '@/test/groupShareTestData'
import { octoberMonth, septemberWithArchivedCategory } from '@/test/personalEntryTestData'
import { findRepeatingRow } from '@/test/recurringIncomeTestHelpers'
import { networkFailureAnswer, neverAnswer } from '@/test/requestTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

describe('BudgetScreen, the "Is this the same?" card', () => {
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
    it.each(languages)('shows a region with the visible heading "Is this the same?" when the month has a possible duplicate (%s)', async (language) => {
      await renderBudget({ language, months: [octoberWithOnlyTheDinnerPair] })

      const card = await findDuplicateCard(language)

      const heading = within(card).getByRole('heading', { name: duplicateCardName(language) })
      expect(heading.textContent).toBe(translated(language, 'budget.duplicates.title'))
      expect(card.getAttribute('aria-hidden')).not.toBe('true')
    })

    it('shows no card when the month has no possible duplicates', async () => {
      await renderBudget({ months: [octoberMonth] })
      await findTotalCard()

      expect(queryDuplicateCard()).toBeNull()
    })

    it('shows no card when the month has group shares but no possible duplicates', async () => {
      await renderBudget({ months: [octoberWithGroupShares] })
      await findTotalCard()

      expect(queryDuplicateCard()).toBeNull()
    })

    it('does not show while the month is loading', async () => {
      await renderBudget({ answers: { [personalEntriesGet('2026-10')]: neverAnswer() } })

      await waitFor(() => {
        expect(screen.getByRole('status')).toBeTruthy()
      })
      expect(queryDuplicateCard()).toBeNull()
    })

    it('does not show when the month cannot be loaded', async () => {
      await renderBudget({ answers: { [personalEntriesGet('2026-10')]: networkFailureAnswer() } })

      expect(await screen.findByRole('alert')).toBeTruthy()
      expect(queryDuplicateCard()).toBeNull()
    })

    it('belongs to the month on screen: a month without a pair has no card, and October brings its card back', async () => {
      await renderBudget({ months: [octoberWithOnlyTheDinnerPair, septemberWithArchivedCategory] })
      await findDuplicateCard()

      fireEvent.click(monthButton('Sep'))
      await waitFor(() => {
        expect(screen.getByRole('list', { name: translated('en', 'budget.screen.entries') }).textContent).toContain('Gym')
      })

      expect(queryDuplicateCard()).toBeNull()
      fireEvent.click(monthButton('Oct'))
      expect(await findDuplicateCard()).toBeTruthy()
    })
  })

  describe('one pair at a time', () => {
    it('shows only the first pair of the answer', async () => {
      await renderBudget({ months: [octoberWithTwoPairs] })

      const card = await findDuplicateCard()

      expect(cardText(card)).toContain(dinnerSentence())
      expect(cardText(card)).not.toContain('Taxi')
    })

    it('shows one card with exactly the two answer buttons, even when there are two pairs', async () => {
      await renderBudget({ months: [octoberWithTwoPairs] })

      const card = await findDuplicateCard()

      expect(screen.getAllByRole('region', { name: duplicateCardName() })).toHaveLength(1)
      expect(within(card).getAllByRole('button')).toHaveLength(2)
    })
  })

  describe('the sentence', () => {
    it.each(languages)('names the entry, the group expense and the group with both amounts in MKD and both days (%s)', async (language) => {
      await renderBudget({ language, months: [octoberWithOnlyTheDinnerPair] })

      const card = await findDuplicateCard(language)

      expect(cardText(card)).toContain(dinnerSentence(language))
    })

    it.each(languages)('shows the amounts in EUR with cents when the pair is in euros (%s)', async (language) => {
      await renderBudget({ language, months: [octoberWithOnlyTheTaxiPair] })

      const card = await findDuplicateCard(language)

      expect(cardText(card)).toContain(taxiSentence(language))
    })

    it.each(languages)('uses the category names when the entry has no note and the group expense has no title (%s)', async (language) => {
      await renderBudget({ language, months: [octoberWithOnlyTheGroceriesPair] })

      const card = await findDuplicateCard(language)

      expect(cardText(card)).toContain(groceriesSentence(language))
    })

    it('writes the day of the entry as "Yesterday" and the day of the expense as "Today" like the entry rows do', async () => {
      await renderBudget({ months: [octoberWithOnlyTheGroceriesPair] })

      const card = await findDuplicateCard()

      expect(cardText(card)).toContain('17 Oct')
      expect(cardText(card)).toContain(translated('en', 'expenses.today'))
    })

    it('keeps an ampersand and apostrophes of a note as typed', async () => {
      const note = "Tom & Jerry's \"big\" dinner"
      const entry = { ...taxiRideEntry, note }
      const month = {
        ...octoberWithOnlyTheTaxiPair,
        entries: [entry],
        possibleDuplicates: [duplicatePair(entry, euroTaxiShare)],
      }
      await renderBudget({ months: [month] })

      const card = await findDuplicateCard()

      expect(cardText(card)).toContain(`“${note}”`)
    })

    it('shows the category name of a built-in category in the language of the app', async () => {
      await renderBudget({ language: 'mk', months: [octoberWithOnlyTheGroceriesPair] })

      const card = await findDuplicateCard('mk')

      expect(cardText(card)).toContain(translated('mk', 'categories.groceries'))
      expect(cardText(card)).not.toContain(translated('en', 'categories.groceries'))
    })
  })

  describe('where it sits', () => {
    it('sits under the month strip and above the "Spent this month" card', async () => {
      await renderBudget({ months: [octoberWithOnlyTheDinnerPair] })
      const total = await findTotalCard()

      const card = await findDuplicateCard()

      expect(isBefore(monthStrip(), card)).toBe(true)
      expect(isBefore(card, total)).toBe(true)
    })

    it('sits above the Repeating income row', async () => {
      await renderBudget({ months: [octoberWithOnlyTheDinnerPair] })
      const repeating = await findRepeatingRow()

      const card = await findDuplicateCard()

      expect(isBefore(card, repeating)).toBe(true)
    })
  })

  describe('the two buttons', () => {
    it.each(languages)('are "Same, merge" and "No, it\'s new", Same first (%s)', async (language) => {
      await renderBudget({ language, months: [octoberWithOnlyTheDinnerPair] })
      const card = await findDuplicateCard(language)

      const buttons = within(card).getAllByRole('button')

      expect(buttons.map((button) => button.textContent)).toEqual([
        translated(language, 'budget.duplicates.same'),
        translated(language, 'budget.duplicates.new'),
      ])
    })

    it('are at least 44 px tall', async () => {
      await renderBudget({ months: [octoberWithOnlyTheDinnerPair] })
      const card = await findDuplicateCard()

      expect(hasTapHeightOfAtLeast44(sameButton(card))).toBe(true)
      expect(hasTapHeightOfAtLeast44(newButton(card))).toBe(true)
    })

    it('are real buttons that the keyboard can reach in the reading order, so Enter and Space press them', async () => {
      await renderBudget({ months: [octoberWithOnlyTheDinnerPair] })
      const card = await findDuplicateCard()
      const same = sameButton(card)
      const other = newButton(card)

      expect(same.tagName).toBe('BUTTON')
      expect(other.tagName).toBe('BUTTON')
      expect(same.getAttribute('type')).toBe('button')
      expect(other.getAttribute('type')).toBe('button')
      expect(same.tabIndex).toBeGreaterThanOrEqual(0)
      expect(other.tabIndex).toBeGreaterThanOrEqual(0)
      expect(isBefore(same, other)).toBe(true)
      same.focus()
      expect(document.activeElement).toBe(same)
      other.focus()
      expect(document.activeElement).toBe(other)
    })

    it('start enabled', async () => {
      await renderBudget({ months: [octoberWithOnlyTheDinnerPair] })
      const card = await findDuplicateCard()

      expect(sameButton(card).disabled).toBe(false)
      expect(newButton(card).disabled).toBe(false)
    })
  })
})
