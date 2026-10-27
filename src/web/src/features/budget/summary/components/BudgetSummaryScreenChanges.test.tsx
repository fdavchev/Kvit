import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { readableText } from '@/test/budgetLeftCardTestHelpers'
import { budgetNow } from '@/test/budgetTestHelpers'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import {
  barelyMoreSummary,
  halfPercentSummary,
  incomeOnlySummary,
  octoberSummary,
  spendingOnlyLastMonthSummary,
  spendingOnlyThisMonthSummary,
  unchangedSummary,
} from '@/test/personalSummaryTestData'
import {
  amountReadout,
  categoryRows,
  changeOf,
  differenceOf,
  directionOf,
  downText,
  findCurrencyCard,
  percentText,
  renderSummaryScreen,
  signedMoney,
  totalRow,
  upText,
  visibleText,
} from '@/test/personalSummaryTestHelpers'
import type { PersonalSummary } from '@/core/services/personalSummary/personalSummaryService'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const redLook = 'text-negative'
const greenLook = 'text-positive'
const greyLook = 'text-muted-foreground'

function classesOf(element: HTMLElement): string[] {
  return Array.from(element.classList)
}

function hasNoRedOrGreen(element: HTMLElement): boolean {
  const classes = classesOf(element)
  return !classes.includes(redLook) && !classes.includes(greenLook)
}

async function mkdCardOf(summary: PersonalSummary) {
  await renderSummaryScreen({ summaries: [summary] })
  return findCurrencyCard('MKD')
}

describe('the monthly summary screen, the changes', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.clearAllMocks()
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(budgetNow)
    stubDeviceColorScheme('light')
    stubGoogleSignIn()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('the Spent total', () => {
    it.each(languages)('says "up 67 percent" for a screen reader when spending went up from 17,470 to 29,200 MKD (%s)', async (language) => {
      await renderSummaryScreen({ language })
      const card = await findCurrencyCard('MKD', language)

      const change = changeOf(totalRow(card, 'spending'))

      expect(directionOf(change)).toBe('up')
      expect(readableText(change)).toBe(upText(67, language))
    })

    it('shows the percent for the eye as "67 %", with the arrow hidden from a screen reader', async () => {
      const card = await mkdCardOf(octoberSummary)
      const change = changeOf(totalRow(card, 'spending'))

      expect(visibleText(change)).toContain(percentText(67))
      expect(change.querySelector('[aria-hidden="true"]')).not.toBeNull()
    })

    it('shows the difference in money with a plus sign when spending went up', async () => {
      const card = await mkdCardOf(octoberSummary)

      const difference = differenceOf(totalRow(card, 'spending'))

      expect(difference).not.toBeNull()
      expect(amountReadout(difference as HTMLElement)).toBe(signedMoney(1173000, 'MKD'))
    })

    it('paints a rise in spending red, in the change and in the difference', async () => {
      const card = await mkdCardOf(octoberSummary)
      const row = totalRow(card, 'spending')

      expect(classesOf(changeOf(row))).toContain(redLook)
      expect(classesOf(differenceOf(row) as HTMLElement)).toContain(redLook)
    })

    it.each(languages)('says "down 100 percent" when spending went down to 0 (%s)', async (language) => {
      await renderSummaryScreen({ language, summaries: [spendingOnlyLastMonthSummary] })
      const card = await findCurrencyCard('MKD', language)

      const change = changeOf(totalRow(card, 'spending'))

      expect(directionOf(change)).toBe('down')
      expect(readableText(change)).toBe(downText(100, language))
    })

    it('paints a fall in spending green and writes the difference with a minus sign', async () => {
      const card = await mkdCardOf(spendingOnlyLastMonthSummary)
      const row = totalRow(card, 'spending')

      expect(classesOf(changeOf(row))).toContain(greenLook)
      expect(classesOf(differenceOf(row) as HTMLElement)).toContain(greenLook)
      expect(amountReadout(differenceOf(row) as HTMLElement)).toBe(signedMoney(-500000, 'MKD'))
    })

    it.each(languages)('says "new" when there was no spending in the previous month, in red (%s)', async (language) => {
      await renderSummaryScreen({ language, summaries: [spendingOnlyThisMonthSummary] })
      const card = await findCurrencyCard('MKD', language)
      const row = totalRow(card, 'spending')

      const change = changeOf(row)

      expect(directionOf(change)).toBe('new')
      expect(readableText(change)).toBe(translated(language, 'budget.summary.new'))
      expect(classesOf(change)).toContain(redLook)
      expect(amountReadout(differenceOf(row) as HTMLElement)).toBe(signedMoney(300000, 'MKD', language))
    })

    it('never shows an infinite or not-a-number percent for a new total', async () => {
      const card = await mkdCardOf(spendingOnlyThisMonthSummary)

      expect(visibleText(card)).not.toMatch(/Infinity|NaN|∞/)
    })

    it.each(languages)('says "no change" in grey, with no difference, when both months are equal (%s)', async (language) => {
      await renderSummaryScreen({ language, summaries: [unchangedSummary] })
      const card = await findCurrencyCard('MKD', language)
      const row = totalRow(card, 'spending')

      const change = changeOf(row)

      expect(directionOf(change)).toBe('same')
      expect(readableText(change)).toBe(translated(language, 'budget.summary.same'))
      expect(classesOf(change)).toContain(greyLook)
      expect(hasNoRedOrGreen(change)).toBe(true)
      expect(differenceOf(row)).toBeNull()
    })

    it('rounds the percent to the whole number, an exact half up: 12.5 percent is 13', async () => {
      const card = await mkdCardOf(halfPercentSummary)

      expect(readableText(changeOf(totalRow(card, 'spending')))).toBe(upText(13))
    })

    it('says up 0 percent, not same, for a change that is real but below half a percent', async () => {
      const card = await mkdCardOf(barelyMoreSummary)
      const row = totalRow(card, 'spending')

      expect(directionOf(changeOf(row))).toBe('up')
      expect(readableText(changeOf(row))).toBe(upText(0))
      expect(amountReadout(differenceOf(row) as HTMLElement)).toBe(signedMoney(100, 'MKD'))
    })
  })

  describe('the Income total', () => {
    it.each(languages)('says "up 5 percent" when income went up from 38,000 to 40,000 MKD (%s)', async (language) => {
      await renderSummaryScreen({ language })
      const card = await findCurrencyCard('MKD', language)
      const row = totalRow(card, 'income')

      expect(directionOf(changeOf(row))).toBe('up')
      expect(readableText(changeOf(row))).toBe(upText(5, language))
      expect(amountReadout(differenceOf(row) as HTMLElement)).toBe(signedMoney(200000, 'MKD', language))
    })

    it('uses neutral colours, neither red nor green, when income went up', async () => {
      const card = await mkdCardOf(octoberSummary)
      const row = totalRow(card, 'income')

      expect(hasNoRedOrGreen(changeOf(row))).toBe(true)
      expect(hasNoRedOrGreen(differenceOf(row) as HTMLElement)).toBe(true)
      expect(classesOf(changeOf(row))).toContain(greyLook)
    })

    it('uses neutral colours, neither red nor green, when income went down to 0', async () => {
      await renderSummaryScreen()
      const card = await findCurrencyCard('EUR')
      const row = totalRow(card, 'income')

      expect(directionOf(changeOf(row))).toBe('down')
      expect(readableText(changeOf(row))).toBe(downText(100))
      expect(hasNoRedOrGreen(changeOf(row))).toBe(true)
      expect(hasNoRedOrGreen(differenceOf(row) as HTMLElement)).toBe(true)
      expect(amountReadout(differenceOf(row) as HTMLElement)).toBe(signedMoney(-20000, 'EUR'))
    })

    it('rounds a fall of 12.5 percent to down 13 and keeps it neutral', async () => {
      const card = await mkdCardOf(halfPercentSummary)
      const row = totalRow(card, 'income')

      expect(readableText(changeOf(row))).toBe(downText(13))
      expect(hasNoRedOrGreen(changeOf(row))).toBe(true)
    })

    it('says "no change" in grey when income is the same', async () => {
      const card = await mkdCardOf(unchangedSummary)
      const row = totalRow(card, 'income')

      expect(directionOf(changeOf(row))).toBe('same')
      expect(differenceOf(row)).toBeNull()
    })

    it('says "new" in a neutral colour when there was no income in the previous month', async () => {
      const summary: PersonalSummary = {
        ...incomeOnlySummary,
        currencies: [{ ...incomeOnlySummary.currencies[0], incomePreviousMinor: 0 }],
      }
      const card = await mkdCardOf(summary)
      const row = totalRow(card, 'income')

      expect(directionOf(changeOf(row))).toBe('new')
      expect(readableText(changeOf(row))).toBe(translated('en', 'budget.summary.new'))
      expect(hasNoRedOrGreen(changeOf(row))).toBe(true)
    })
  })

  describe('the changes of the categories', () => {
    it('gives every category row a change with its direction, in the order of the rows', async () => {
      await renderSummaryScreen()
      const card = await findCurrencyCard('MKD')

      const directions = categoryRows(card).map((row) => directionOf(changeOf(row)))

      expect(directions).toEqual(['new', 'down', 'same', 'up', 'down'])
    })

    it('says what a screen reader reads for each row: new, down 5, no change, up 550, down 100', async () => {
      await renderSummaryScreen()
      const card = await findCurrencyCard('MKD')

      const readings = categoryRows(card).map((row) => readableText(changeOf(row)))

      expect(readings).toEqual([
        translated('en', 'budget.summary.new'),
        downText(5),
        translated('en', 'budget.summary.same'),
        upText(550),
        downText(100),
      ])
    })

    it.each(languages)('says the same in %s with the Macedonian words from the texts of the app', async (language) => {
      await renderSummaryScreen({ language })
      const card = await findCurrencyCard('MKD', language)

      const readings = categoryRows(card, language).map((row) => readableText(changeOf(row)))

      expect(readings).toEqual([
        translated(language, 'budget.summary.new'),
        downText(5, language),
        translated(language, 'budget.summary.same'),
        upText(550, language),
        downText(100, language),
      ])
    })

    it('paints up and new red, down green and equal grey', async () => {
      await renderSummaryScreen()
      const card = await findCurrencyCard('MKD')

      const [coffee, food, groceries, gym, dog] = categoryRows(card).map(changeOf)

      expect(classesOf(coffee)).toContain(redLook)
      expect(classesOf(food)).toContain(greenLook)
      expect(classesOf(groceries)).toContain(greyLook)
      expect(hasNoRedOrGreen(groceries)).toBe(true)
      expect(classesOf(gym)).toContain(redLook)
      expect(classesOf(dog)).toContain(greenLook)
    })

    it('shows the percent for the eye in the rows that went up or down', async () => {
      await renderSummaryScreen()
      const card = await findCurrencyCard('MKD')

      const [, food, , gym] = categoryRows(card).map(changeOf)

      expect(visibleText(food)).toContain(percentText(5))
      expect(visibleText(gym)).toContain(percentText(550))
    })

    it('shows no money difference in a category row, only in the two totals', async () => {
      await renderSummaryScreen()
      const card = await findCurrencyCard('MKD')

      for (const row of categoryRows(card)) {
        expect(differenceOf(row)).toBeNull()
      }
    })
  })

  describe('for a screen reader', () => {
    it('never leaves a change that is only an arrow: every change reads as words and holds no arrow glyph', async () => {
      await renderSummaryScreen()
      await findCurrencyCard('MKD')

      const changes = Array.from(document.querySelectorAll<HTMLElement>('[data-summary-change]'))

      expect(changes).toHaveLength(10)
      for (const change of changes) {
        const reading = readableText(change)
        expect(reading).toMatch(/[A-Za-z]{2,}/)
        expect(reading).not.toMatch(/[▲▼↑↓△▽+-]/)
      }
    })

    it('puts the direction attribute on every change, from the four known words only', async () => {
      await renderSummaryScreen()
      await findCurrencyCard('MKD')

      const directions = Array.from(document.querySelectorAll<HTMLElement>('[data-summary-change]')).map(directionOf)

      expect(directions).toHaveLength(10)
      for (const direction of directions) {
        expect(['up', 'down', 'same', 'new']).toContain(direction)
      }
    })
  })
})
