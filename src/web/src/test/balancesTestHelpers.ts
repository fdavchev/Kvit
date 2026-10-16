import { screen, within } from '@testing-library/react'
import { expect } from 'vitest'
import { plainSpaces } from './expenseTestHelpers'

export const positiveClass = 'text-positive'
export const negativeClass = 'text-negative'

const balanceCardName = /^Your balance/

export function sectionNamed(name: string | RegExp): HTMLElement {
  return screen.getByRole('region', { name })
}

export function queryAllSectionsNamed(name: string | RegExp): HTMLElement[] {
  return screen.queryAllByRole('region', { name })
}

export async function findBalanceCards(): Promise<HTMLElement[]> {
  return screen.findAllByRole('region', { name: balanceCardName })
}

export async function findOnlyBalanceCard(): Promise<HTMLElement> {
  const cards = await findBalanceCards()
  expect(cards).toHaveLength(1)
  return cards[0]
}

export function rowsOf(container: HTMLElement): HTMLElement[] {
  return within(container).queryAllByRole('listitem')
}

export function textIn(container: HTMLElement, text: string): HTMLElement {
  return within(container).getByText(text, { normalizer: plainSpaces })
}

export function amountParagraphIn(container: HTMLElement, amount: string): HTMLElement {
  const paragraph = textIn(container, amount).closest('p')
  if (paragraph === null) {
    throw new Error(`The amount "${amount}" is not inside a paragraph`)
  }
  return paragraph
}

export function queryTextIn(container: HTMLElement, text: string): HTMLElement | null {
  return within(container).queryByText(text, { normalizer: plainSpaces })
}

export function rowWithText(container: HTMLElement, text: string): HTMLElement {
  const row = rowsOf(container).find((candidate) => queryTextIn(candidate, text) !== null)
  if (row === undefined) {
    throw new Error(`Found no row with the text "${text}" in: ${container.textContent}`)
  }
  return row
}

export function rowTextOf(row: HTMLElement): string {
  return plainSpaces(row.textContent ?? '')
}

export function expectAmountLook(element: HTMLElement, look: 'positive' | 'negative' | 'plain'): void {
  expect(element.classList.contains(positiveClass)).toBe(look === 'positive')
  expect(element.classList.contains(negativeClass)).toBe(look === 'negative')
}

export function expectRowWithAmountBeneath(row: HTMLElement, title: string, amount: string): void {
  const titleElement = textIn(row, title)
  const amountElement = textIn(row, amount)
  expect(titleElement.compareDocumentPosition(amountElement) & Node.DOCUMENT_POSITION_FOLLOWING).not.toBe(0)
}
