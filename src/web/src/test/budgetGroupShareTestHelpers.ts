import { screen, within } from '@testing-library/react'
import type { Language } from '@/core/i18n/language'
import { readableText } from './budgetLeftCardTestHelpers'
import { translated } from './translated'

export interface ShareRowText {
  title: string
  group: string
  details: string
  amount: string
}

export function findGroupSharesBlock(language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('region', { name: translated(language, 'budget.groupShares.title') })
}

export function queryGroupSharesBlock(language: Language = 'en'): HTMLElement | null {
  return screen.queryByRole('region', { name: translated(language, 'budget.groupShares.title') })
}

export function shareRows(block: HTMLElement): HTMLElement[] {
  return within(block).getAllByRole('listitem')
}

function partOf(row: HTMLElement, attribute: string): HTMLElement {
  const part = row.querySelector<HTMLElement>(`[${attribute}]`)
  if (part === null) {
    throw new Error(`The group share row has no element with the attribute ${attribute}`)
  }
  return part
}

export function shareAmountOf(row: HTMLElement): HTMLElement {
  return partOf(row, 'data-share-amount')
}

export function shareGroupLabelOf(row: HTMLElement): HTMLElement {
  return partOf(row, 'data-share-group')
}

export function shareTextColumnOf(row: HTMLElement): HTMLElement {
  return partOf(row, 'data-share-text')
}

export function shareTitleOf(row: HTMLElement): HTMLElement {
  return partOf(row, 'data-share-title')
}

export function shareRowText(row: HTMLElement): ShareRowText {
  return {
    title: readableText(shareTitleOf(row)),
    group: readableText(shareGroupLabelOf(row)),
    details: readableText(partOf(row, 'data-share-details')),
    amount: readableText(shareAmountOf(row)),
  }
}
