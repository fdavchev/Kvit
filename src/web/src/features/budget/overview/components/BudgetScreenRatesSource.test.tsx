import { screen, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import type { Currency } from '@/shared/utils/formatMoney'
import { findLeftCard } from '@/test/budgetLeftCardTestHelpers'
import { budgetNow, findTotalCard, renderBudget } from '@/test/budgetTestHelpers'
import {
  euroOnlyOctoberWithShareConvertedToEur,
  mkdOnlyWithSharesConvertedToMkd,
  octoberWithGroupShares,
  octoberWithGroupSharesConvertedToEur,
  octoberWithGroupSharesConvertedToMkd,
  octoberWithSalaryAndGroupSharesConvertedToMkd,
} from '@/test/groupShareTestData'
import { testMe } from '@/test/testMe'
import { translated } from '@/test/translated'

function meWithTotalIn(budgetTotalCurrency: Currency | null) {
  return { ...testMe, budgetTotalCurrency }
}

describe('BudgetScreen, the rates source note', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(budgetNow)
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('where a converted total is shown', () => {
    it.each(languages)('is on the total card after the line "includes converted amounts" (%s)', async (language) => {
      await renderBudget({ language, me: meWithTotalIn('MKD'), months: [octoberWithGroupSharesConvertedToMkd] })

      const card = await findTotalCard(language)
      const includesLine = within(card).getByText(translated(language, 'budget.card.includesConvertedEuro'))
      const note = within(card).getByText(translated(language, 'legal.ratesSource'))

      expect(includesLine.compareDocumentPosition(note)).toBe(Node.DOCUMENT_POSITION_FOLLOWING)
    })

    it.each(languages)('is on the card with income too (%s)', async (language) => {
      await renderBudget({
        language,
        me: meWithTotalIn('MKD'),
        months: [octoberWithSalaryAndGroupSharesConvertedToMkd],
      })

      const card = await findLeftCard(language)

      expect(within(card).getByText(translated(language, 'legal.ratesSource'))).toBeTruthy()
    })

    it('is on the total card of a EUR total that includes converted denar amounts', async () => {
      await renderBudget({ me: meWithTotalIn('EUR'), months: [octoberWithGroupSharesConvertedToEur] })

      const card = await findTotalCard()

      expect(within(card).getByText(translated('en', 'budget.card.includesConvertedDenars'))).toBeTruthy()
      expect(within(card).getByText(translated('en', 'legal.ratesSource'))).toBeTruthy()
    })

    it('is shown once on the screen, in grey', async () => {
      await renderBudget({ me: meWithTotalIn('MKD'), months: [octoberWithGroupSharesConvertedToMkd] })
      await findTotalCard()

      const notes = screen.getAllByText(translated('en', 'legal.ratesSource'))

      expect(notes).toHaveLength(1)
      expect(Array.from(notes[0].classList)).toContain('text-muted-foreground')
    })
  })

  describe('where nothing was converted', () => {
    it('is left out of an MKD total when every item of the month is in denars', async () => {
      await renderBudget({ me: meWithTotalIn('MKD'), months: [mkdOnlyWithSharesConvertedToMkd] })

      const card = await findTotalCard()

      expect(within(card).queryByText(translated('en', 'legal.ratesSource'))).toBeNull()
      expect(screen.queryByText(translated('en', 'legal.ratesSource'))).toBeNull()
    })

    it('is left out of a EUR total when every item of the month is in euro', async () => {
      await renderBudget({ me: meWithTotalIn('EUR'), months: [euroOnlyOctoberWithShareConvertedToEur] })

      const card = await findTotalCard()

      expect(within(card).queryByText(translated('en', 'legal.ratesSource'))).toBeNull()
      expect(screen.queryByText(translated('en', 'legal.ratesSource'))).toBeNull()
    })

    it.each(languages)('is left out of the card per currency when the setting is off (%s)', async (language) => {
      await renderBudget({ language, me: meWithTotalIn(null), months: [octoberWithGroupShares] })

      const card = await findTotalCard(language)

      expect(within(card).queryByText(translated(language, 'legal.ratesSource'))).toBeNull()
      expect(screen.queryByText(translated(language, 'legal.ratesSource'))).toBeNull()
    })

    it('is left out of the card per currency when a saved month still has converted totals but the setting is off', async () => {
      await renderBudget({ me: meWithTotalIn(null), months: [octoberWithGroupSharesConvertedToMkd] })
      await findTotalCard()

      expect(screen.queryByText(translated('en', 'legal.ratesSource'))).toBeNull()
    })
  })
})
