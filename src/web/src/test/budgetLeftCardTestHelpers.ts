import { screen } from '@testing-library/react'
import type { Language } from '@/core/i18n/language'
import { formatMoney, type Currency } from '@/shared/utils/formatMoney'
import { formatSignedMoney } from '@/shared/utils/formatSignedMoney'
import { plainSpaces } from './expenseTestHelpers'
import { translated } from './translated'

export function findLeftCard(language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('region', { name: translated(language, 'budget.screen.leftThisMonth') })
}

export function money(amountMinor: number, currency: Currency, language: Language = 'en'): string {
  return plainSpaces(formatMoney(amountMinor, currency, language))
}

export function incomeMoney(amountMinor: number, currency: Currency, language: Language = 'en'): string {
  return plainSpaces(formatSignedMoney(amountMinor, currency, language))
}

export function asciiMinus(text: string): string {
  return text.replaceAll('−', '-')
}

export function readableText(element: Element): string {
  const copy = element.cloneNode(true)
  if (!(copy instanceof Element)) {
    throw new Error('Expected an element to read')
  }
  for (const hidden of copy.querySelectorAll('[aria-hidden="true"]')) {
    hidden.remove()
  }
  return asciiMinus(plainSpaces(copy.textContent ?? ''))
}

export function amountElements(card: HTMLElement, attribute: string): HTMLElement[] {
  return Array.from(card.querySelectorAll<HTMLElement>(`[${attribute}]`))
}

export function amountTexts(card: HTMLElement, attribute: string): string[] {
  return amountElements(card, attribute).map((element) => asciiMinus(plainSpaces((element.querySelector('.sr-only') ?? element).textContent ?? '')))
}

export const leftAttribute = 'data-total-amount'
export const incomeAttribute = 'data-income-amount'
export const spentAttribute = 'data-spent-amount'

function kindOf(element: HTMLElement): string {
  if (element.hasAttribute(leftAttribute)) {
    return 'left'
  }
  if (element.hasAttribute(incomeAttribute)) {
    return 'income'
  }
  return 'spent'
}

export function amountLinesInOrder(card: HTMLElement): string[] {
  const selector = `[${leftAttribute}], [${incomeAttribute}], [${spentAttribute}]`
  return Array.from(card.querySelectorAll<HTMLElement>(selector)).map(
    (element) => `${kindOf(element)}:${asciiMinus(plainSpaces((element.querySelector('.sr-only') ?? element).textContent ?? ''))}`,
  )
}

export function classesOf(element: Element): string[] {
  return element.className.split(/\s+/)
}

export function amountAt(card: HTMLElement, attribute: string, index: number): HTMLElement {
  const element = amountElements(card, attribute)[index]
  if (element === undefined) {
    throw new Error(`The card has no element number ${index + 1} with the attribute ${attribute}`)
  }
  return element
}
