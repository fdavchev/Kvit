import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import type { PersonalEntryMonth } from '@/core/services/personalEntries/personalEntriesService'
import indexCss from '@/index.css?raw'
import { formatMoney, currencyLabel, type Currency } from '@/shared/utils/formatMoney'
import {
  budgetNow,
  categoryTotalsRegion,
  findEntryList,
  findTotalCard,
  monthButton,
  monthStrip,
  renderBudget,
} from '@/test/budgetTestHelpers'
import { plainSpaces } from '@/test/expenseTestHelpers'
import { blockAfter } from '@/test/indexCssColors'
import { personalCategoryLabel } from '@/test/personalCategoryTestHelpers'
import {
  archivedGymCategory,
  coffeeCategory,
  personalFoodCategory,
  personalGroceriesCategory,
} from '@/test/personalCategoryTestData'
import {
  euroOnlyAugust,
  mkdOnlyMonth,
  octoberMonth,
  septemberWithArchivedCategory,
} from '@/test/personalEntryTestData'
import { requestCount } from '@/test/requestTestHelpers'
import { translated } from '@/test/translated'

const longAmountsMonth: PersonalEntryMonth = {
  ...mkdOnlyMonth,
  totals: [
    {
      currency: 'MKD',
      totalMinor: 12345678900,
      categories: [{ category: personalGroceriesCategory, totalMinor: 12345678900, count: 1 }],
    },
    {
      currency: 'EUR',
      totalMinor: 123456789,
      categories: [{ category: personalFoodCategory, totalMinor: 123456789, count: 1 }],
    },
  ],
}

function money(amountMinor: number, currency: Currency, language: Language = 'en'): string {
  return plainSpaces(formatMoney(amountMinor, currency, language))
}

function textOf(element: Element): string {
  return plainSpaces(element.textContent ?? '')
}

function readableText(element: Element): string {
  const copy = element.cloneNode(true)
  if (!(copy instanceof Element)) {
    throw new Error('Expected an element to read')
  }
  for (const hidden of copy.querySelectorAll('[aria-hidden="true"]')) {
    hidden.remove()
  }
  return textOf(copy)
}

function amountLines(card: HTMLElement): string[] {
  return Array.from(card.querySelectorAll('[data-total-amount]')).map((line) => textOf(line.querySelector('.sr-only') ?? line))
}

describe('BudgetScreen', () => {
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

  describe('the month strip', () => {
    it.each(languages)('lists the last 24 months up to this month, oldest first, with this month pressed (%s)', async (language) => {
      await renderBudget({ language })

      const buttons = within(await screen.findByRole('group', { name: translated(language, 'budget.screen.months') })).getAllByRole('button')

      expect(buttons).toHaveLength(24)
      expect(buttons.map((button) => button.textContent).slice(0, 3)).toEqual(
        language === 'en' ? ['Nov 2024', 'Dec 2024', 'Jan 2025'] : ['ное 2024', 'дек 2024', 'јан 2025'],
      )
      expect(buttons.map((button) => button.textContent).slice(-3)).toEqual(
        language === 'en' ? ['Aug', 'Sep', 'Oct'] : ['авг', 'сеп', 'окт'],
      )
      expect(buttons.filter((button) => button.getAttribute('aria-pressed') === 'true')).toEqual([buttons.at(-1)])
    })

    it('hides the scrollbar of the strip and scrolls it sideways with an up-and-down mouse wheel', async () => {
      await renderBudget()
      await findTotalCard()
      const strip = monthStrip()
      Object.defineProperty(strip, 'scrollWidth', { configurable: true, value: 1600 })
      Object.defineProperty(strip, 'clientWidth', { configurable: true, value: 400 })
      Object.defineProperty(strip, 'scrollLeft', { configurable: true, writable: true, value: 1200 })

      fireEvent.wheel(strip, { deltaY: -100 })

      expect(strip.className.split(/\s+/)).toEqual(expect.arrayContaining(['overflow-x-auto', 'sideways-scroll']))
      expect(strip.scrollLeft).toBe(1100)
    })

    it('draws the rounded background on a frame around the strip, so the faded edges do not fade the background', async () => {
      await renderBudget()
      await findTotalCard()

      const strip = monthStrip()

      expect(strip.className.split(/\s+/)).not.toContain('bg-card')
      expect(strip.parentElement?.className.split(/\s+/)).toEqual(expect.arrayContaining(['rounded-full', 'bg-card']))
    })

    it('shows the year on January of this year, where the year changes', async () => {
      await renderBudget()

      await findTotalCard()
      expect(monthButton('Jan 2026')).toBeTruthy()
      expect(monthButton('Feb')).toBeTruthy()
    })

    it('asks the server for this month when the screen opens', async () => {
      const { fetchMock } = await renderBudget()

      await findTotalCard()
      expect(requestCount(fetchMock, 'GET', '/api/personal/entries?month=2026-10')).toBe(1)
    })

    it('takes this month from the time zone of the person, not of the device', async () => {
      vi.setSystemTime(new Date('2026-10-31T23:30:00Z'))

      const { fetchMock } = await renderBudget({ months: [octoberMonth, { ...octoberMonth, month: '2026-11' }] })

      await findTotalCard()
      expect(requestCount(fetchMock, 'GET', '/api/personal/entries?month=2026-11')).toBe(1)
      expect(monthButton('Nov').getAttribute('aria-pressed')).toBe('true')
    })

    it('asks the server for September and shows it when September is tapped', async () => {
      const { fetchMock } = await renderBudget({ months: [octoberMonth, septemberWithArchivedCategory] })
      await findTotalCard()

      fireEvent.click(monthButton('Sep'))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', '/api/personal/entries?month=2026-09')).toBe(1)
      })
      expect(within(await findTotalCard()).getByText(money(150000, 'MKD'), { normalizer: plainSpaces })).toBeTruthy()
      expect(monthButton('Sep').getAttribute('aria-pressed')).toBe('true')
      expect(monthButton('Oct').getAttribute('aria-pressed')).toBe('false')
    })

    it('shows the saved month at once, without skeletons, when the person comes back to it', async () => {
      await renderBudget({ months: [octoberMonth, septemberWithArchivedCategory] })
      await findTotalCard()
      fireEvent.click(monthButton('Sep'))
      await screen.findAllByText(personalCategoryLabel('en', archivedGymCategory))

      fireEvent.click(monthButton('Oct'))

      expect(screen.queryByRole('status')).toBeNull()
      const rows = within(screen.getByRole('list', { name: translated('en', 'budget.screen.entries') })).getAllByRole('listitem')
      expect(rows.map(readableText)).toEqual([`CoffeeToday${money(12000, 'MKD')}`, `Food & drinksYesterday · Lunch in Ohrid${money(1250, 'EUR')}`])
    })

    it('scrolls the strip to its right end when the screen opens, so this month is in view', async () => {
      vi.spyOn(HTMLElement.prototype, 'scrollWidth', 'get').mockReturnValue(1200)
      vi.spyOn(HTMLElement.prototype, 'clientWidth', 'get').mockReturnValue(312)

      await renderBudget()

      expect(monthStrip().scrollLeft).toBe(888)
    })

    it('gives every month a tap area of at least 44 px and scrolls sideways inside the strip only', async () => {
      await renderBudget()
      await findTotalCard()

      const strip = monthStrip()
      expect(strip.className.split(/\s+/)).toContain('overflow-x-auto')
      for (const button of within(strip).getAllByRole('button')) {
        expect(button.className.split(/\s+/)).toEqual(expect.arrayContaining(['min-h-11', 'flex-none']))
      }
    })

    it('gives the pressed month the filled pill look of the bottom bar', async () => {
      await renderBudget()
      await findTotalCard()

      expect(monthButton('Oct').className.split(/\s+/)).toEqual(
        expect.arrayContaining(['aria-pressed:bg-tab-active', 'aria-pressed:text-tab-active-foreground']),
      )
    })
  })

  describe('the total card', () => {
    it.each(languages)('is labelled "Spent this month" and shows the MKD total big and the EUR total on its own line (%s)', async (language) => {
      await renderBudget({ language })

      const card = await findTotalCard(language)

      expect(within(card).getByText(translated(language, 'budget.screen.spentThisMonth'))).toBeTruthy()
      expect(amountLines(card)).toEqual([money(12000, 'MKD', language), money(1250, 'EUR', language)])
    })

    it('keeps the fixed label on an older month instead of the month name', async () => {
      await renderBudget({ months: [octoberMonth, septemberWithArchivedCategory] })
      await findTotalCard()

      fireEvent.click(monthButton('Sep'))

      await screen.findAllByText(personalCategoryLabel('en', archivedGymCategory))
      expect(await findTotalCard()).toBeTruthy()
      expect(screen.queryByText(/September/)).toBeNull()
    })

    it('writes no "+" in front of the EUR line', async () => {
      await renderBudget()

      const card = await findTotalCard()

      expect(textOf(card)).not.toContain('+')
    })

    it('shows only the MKD total when the month has no EUR', async () => {
      await renderBudget({ months: [mkdOnlyMonth] })

      const card = await findTotalCard()

      expect(amountLines(card)).toEqual([money(150400, 'MKD')])
    })

    it('shows the EUR total as the big one when the month has only EUR', async () => {
      vi.setSystemTime(new Date('2026-08-20T10:00:00Z'))
      await renderBudget({ months: [euroOnlyAugust] })

      const card = await findTotalCard()

      expect(amountLines(card)).toEqual([money(4250, 'EUR')])
      expect(card.querySelector('[data-total-amount="main"]')?.textContent).toContain('42.50')
    })

    it.each(languages)('keeps each total whole, shrunk to fit the card, so a long amount never breaks inside its label (%s)', async (language) => {
      await renderBudget({ language, months: [longAmountsMonth] })

      const card = await findTotalCard(language)
      const lines = Array.from(card.querySelectorAll<HTMLElement>('[data-total-amount]'))

      expect(lines).toHaveLength(2)
      for (const line of lines) {
        const fitted = line.querySelector<HTMLElement>('.fit-amount')
        expect(line.className.split(/\s+/)).toContain('@container')
        expect(line.className.split(/\s+/)).not.toContain('break-words')
        expect(fitted?.style.getPropertyValue('--amount-length')).toBe(String(readableText(line).length))
      }
      expect(amountLines(card)).toEqual([money(12345678900, 'MKD', language), money(123456789, 'EUR', language)])
    })

    it('makes the first amount the big one and the second a smaller line', async () => {
      await renderBudget()

      const card = await findTotalCard()

      const main = card.querySelector('[data-total-amount="main"]')
      const second = card.querySelector('[data-total-amount="second"]')
      expect(textOf(main ?? card)).toContain(money(12000, 'MKD'))
      expect(textOf(second ?? card)).toContain(money(1250, 'EUR'))
      expect(main?.className).toContain('text-[2.25rem]')
      expect(second?.className).not.toContain('text-[2.25rem]')
    })
  })

  describe('the category totals', () => {
    it.each(languages)('shows one grid without a currency label when the month has one currency (%s)', async (language) => {
      await renderBudget({ language, months: [mkdOnlyMonth] })
      await findTotalCard(language)

      const region = categoryTotalsRegion(language)

      expect(within(region).getAllByRole('list')).toHaveLength(1)
      expect(within(region).queryByRole('group')).toBeNull()
      expect(within(region).getAllByRole('listitem').map(readableText)).toEqual([
        `${personalCategoryLabel(language, personalGroceriesCategory)}${money(123400, 'MKD', language)}`,
        `${personalCategoryLabel(language, coffeeCategory)}${money(27000, 'MKD', language)}`,
      ])
    })

    it.each(languages)('shows one labelled grid per currency when the month has MKD and EUR, never mixing them (%s)', async (language) => {
      await renderBudget({ language })
      await findTotalCard(language)

      const region = categoryTotalsRegion(language)
      const mkdGroup = within(region).getByRole('group', { name: currencyLabel('MKD', language) })
      const eurGroup = within(region).getByRole('group', { name: currencyLabel('EUR', language) })

      expect(within(mkdGroup).getAllByRole('listitem').map(readableText)).toEqual([
        `${personalCategoryLabel(language, coffeeCategory)}${money(12000, 'MKD', language)}`,
      ])
      expect(within(eurGroup).getAllByRole('listitem').map(readableText)).toEqual([
        `${personalCategoryLabel(language, personalFoodCategory)}${money(1250, 'EUR', language)}`,
      ])
    })

    it('puts the groups in the order of the totals, MKD before EUR', async () => {
      await renderBudget()
      await findTotalCard()

      const names = within(categoryTotalsRegion())
        .getAllByRole('group')
        .map((group) => group.getAttribute('aria-labelledby'))
        .map((id) => document.getElementById(id ?? '')?.textContent)

      expect(names).toEqual(['MKD', 'EUR'])
    })

    it('draws each tile in the colour of its category, with its emoji', async () => {
      await renderBudget({ months: [mkdOnlyMonth] })
      await findTotalCard()

      const coffeeTile = within(categoryTotalsRegion()).getAllByRole('listitem')[1]
      const emojiTile = coffeeTile?.querySelector<HTMLElement>('[aria-hidden="true"]')

      expect(emojiTile?.textContent).toBe(coffeeCategory.emoji)
      expect(emojiTile?.style.backgroundColor).toBe('var(--category-teal)')
    })

    it('lays the tiles out four in a row', async () => {
      await renderBudget({ months: [mkdOnlyMonth] })
      await findTotalCard()

      expect(within(categoryTotalsRegion()).getByRole('list').className.split(/\s+/)).toContain('grid-cols-4')
    })

    it.each(languages)('keeps a long amount in one piece on one line, shrunk to fit the tile, with its currency (%s)', async (language) => {
      await renderBudget({ language, months: [longAmountsMonth] })
      await findTotalCard(language)

      const tiles = within(categoryTotalsRegion(language)).getAllByRole('listitem')

      expect(tiles).toHaveLength(2)
      for (const tile of tiles) {
        const amount = tile.querySelector<HTMLElement>('[data-tile-amount]')
        const fitted = amount?.querySelector<HTMLElement>('.fit-amount')
        expect(amount?.className.split(/\s+/)).toContain('@container')
        expect(amount?.className.split(/\s+/)).not.toContain('break-words')
        expect(fitted?.style.getPropertyValue('--amount-length')).toBe(String(amount?.textContent?.length))
      }
      expect(textOf(tiles[0])).toContain(money(12345678900, 'MKD', language))
      expect(textOf(tiles[1])).toContain(money(123456789, 'EUR', language))
    })

    it('shrinks the tile amount by the width of its tile and its length, never above the normal size and never below three quarters of it', () => {
      const fitAmount = blockAfter(indexCss, '@utility fit-amount')

      expect(fitAmount).toContain('font-size: max(0.75em, min(1em, calc(100cqi / (var(--amount-length) * var(--average-glyph-width)))));')
    })

    it('lets a fitted amount wrap only between its unbroken pieces, never inside the number or the label', () => {
      const fitAmount = blockAfter(indexCss, '@utility fit-amount')

      expect(fitAmount).not.toContain('white-space: nowrap;')
      expect(fitAmount).toContain('overflow-wrap: normal;')
    })
  })

  describe('the entries', () => {
    it('lists the entries in the order the server sends them, newest first', async () => {
      await renderBudget({ months: [mkdOnlyMonth] })

      const rows = within(await findEntryList()).getAllByRole('listitem')

      expect(rows.map(readableText)).toEqual([
        `CoffeeToday${money(12000, 'MKD')}`,
        `CoffeeYesterday${money(15000, 'MKD')}`,
        `Groceries12 Oct · Weekly shop${money(123400, 'MKD')}`,
      ])
    })

    it('shows each entry in its own currency', async () => {
      await renderBudget()

      const rows = within(await findEntryList()).getAllByRole('listitem')

      expect(rows.map(readableText)).toEqual([
        `CoffeeToday${money(12000, 'MKD')}`,
        `Food & drinksYesterday · Lunch in Ohrid${money(1250, 'EUR')}`,
      ])
    })

    it('writes the day and the category in Macedonian', async () => {
      await renderBudget({ language: 'mk', months: [mkdOnlyMonth] })

      const rows = within(await findEntryList('mk')).getAllByRole('listitem')

      expect(rows.map(readableText)).toEqual([
        `CoffeeДенес${money(12000, 'MKD', 'mk')}`,
        `CoffeeВчера${money(15000, 'MKD', 'mk')}`,
        `${personalCategoryLabel('mk', personalGroceriesCategory)}12 окт · Weekly shop${money(123400, 'MKD', 'mk')}`,
      ])
    })

    it('shows an old entry whose category was archived with that category name and emoji', async () => {
      vi.setSystemTime(new Date('2026-09-20T10:00:00Z'))
      await renderBudget({ months: [septemberWithArchivedCategory] })

      const row = within(await findEntryList()).getByRole('listitem')

      expect(readableText(row)).toContain(archivedGymCategory.name)
      expect(row.querySelector('[aria-hidden="true"]')?.textContent).toBe(archivedGymCategory.emoji)
    })

    it('makes every entry a button at least 44 px high that the keyboard can reach', async () => {
      await renderBudget()

      const buttons = within(await findEntryList()).getAllByRole('button')

      expect(buttons).toHaveLength(2)
      for (const button of buttons) {
        expect(button.tagName).toBe('BUTTON')
        expect(button.getAttribute('type')).toBe('button')
        expect(button.className.split(/\s+/)).toContain('min-h-16')
      }
    })

    it('keeps long names and notes inside the row, so a 360 px phone does not scroll sideways', async () => {
      await renderBudget()

      const button = within(await findEntryList()).getAllByRole('button')[1]
      const textColumn = button?.querySelector('[data-entry-text]')
      const amount = button?.querySelector('[data-entry-amount]')

      expect(textColumn?.className.split(/\s+/)).toEqual(expect.arrayContaining(['min-w-0', 'flex-1']))
      expect(amount?.className.split(/\s+/)).toContain('whitespace-nowrap')
    })
  })
})
