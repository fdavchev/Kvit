import { screen, within } from '@testing-library/react'
import { expect } from 'vitest'
import type { Language } from '@/core/i18n/language'
import { currencyLabel, type Currency } from '@/shared/utils/formatMoney'
import { money } from './budgetLeftCardTestHelpers'
import { categoryName, isBefore, plainSpaces } from './expenseTestHelpers'
import { foodCategory, transportCategory } from './expenseTestData'
import { translated } from './translated'

export interface ExpectedCategoryRow {
  name: string
  amountMinor: number
  emoji?: string
}

const pictograph = /\p{Extended_Pictographic}/u

export function totalsTitle(language: Language = 'en'): HTMLElement {
  return screen.getByRole('heading', { level: 2, name: translated(language, 'groupTotals.title') })
}

export function currencyRegion(currency: Currency, language: Language = 'en'): HTMLElement {
  return screen.getByRole('region', { name: currencyLabel(currency, language) })
}

export function queryCurrencyRegion(currency: Currency, language: Language = 'en'): HTMLElement | null {
  return screen.queryByRole('region', { name: currencyLabel(currency, language) })
}

export function findCurrencyRegion(currency: Currency, language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('region', { name: currencyLabel(currency, language) })
}

export function categoryRowsOf(region: HTMLElement): HTMLElement[] {
  return within(region).queryAllByRole('listitem')
}

export function totalAmountElements(
  region: HTMLElement,
  amountMinor: number,
  currency: Currency,
  language: Language = 'en',
): HTMLElement[] {
  return within(region)
    .queryAllByText(money(amountMinor, currency, language), { normalizer: plainSpaces })
    .filter((element) => element.closest('li') === null)
}

export function amountElementIn(
  row: HTMLElement,
  amountMinor: number,
  currency: Currency,
  language: Language = 'en',
): HTMLElement {
  return within(row).getByText(money(amountMinor, currency, language), { normalizer: plainSpaces })
}

export function nameElementIn(row: HTMLElement, name: string): HTMLElement {
  return within(row).getByText(name, { normalizer: plainSpaces })
}

export function pictographsIn(row: HTMLElement): string[] {
  return Array.from(row.querySelectorAll<HTMLElement>('[aria-hidden="true"]'))
    .map((element) => element.textContent ?? '')
    .filter((text) => pictograph.test(text))
}

export function expectRows(
  region: HTMLElement,
  currency: Currency,
  expected: readonly ExpectedCategoryRow[],
  language: Language = 'en',
): void {
  const rows = categoryRowsOf(region)
  expect(rows.map((row) => plainSpaces(row.textContent ?? ''))).toHaveLength(expected.length)
  expected.forEach((expectedRow, index) => {
    const row = rows[index]
    const name = nameElementIn(row, expectedRow.name)
    const amount = amountElementIn(row, expectedRow.amountMinor, currency, language)
    expect(isBefore(name, amount), `${expectedRow.name} should come before its amount`).toBe(true)
    if (expectedRow.emoji !== undefined) {
      const emoji = within(row).getByText(expectedRow.emoji)
      expect(emoji.closest('[aria-hidden="true"]'), `the emoji of ${expectedRow.name} should be hidden from screen readers`).not.toBeNull()
      expect(isBefore(emoji, name), `the emoji of ${expectedRow.name} should come before its name`).toBe(true)
    }
  })
}

export function mkdOnlyRows(language: Language = 'en'): ExpectedCategoryRow[] {
  return [
    { name: categoryName(language, foodCategory), amountMinor: 80000, emoji: foodCategory.emoji },
    { name: categoryName(language, transportCategory), amountMinor: 30000, emoji: transportCategory.emoji },
    { name: translated(language, 'groupTotals.noCategory'), amountMinor: 25000 },
  ]
}

export function minHeightStepOf(element: HTMLElement): number {
  const steps = Array.from(element.classList)
    .map((className) => /^min-h-(\d+)$/.exec(className))
    .filter((match): match is RegExpExecArray => match !== null)
    .map((match) => Number(match[1]))
  return steps.length === 0 ? 0 : Math.max(...steps)
}

export function hasClassOnItselfOrAncestorsBelow(element: HTMLElement, stop: HTMLElement, classNames: readonly string[]): boolean {
  for (let node: HTMLElement | null = element; node !== null && node !== stop; node = node.parentElement) {
    const classes = node.classList
    if (classNames.every((className) => classes.contains(className))) {
      return true
    }
  }
  return false
}
