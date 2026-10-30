import { within } from '@testing-library/react'
import type { Language } from '@/core/i18n/language'
import { readableText } from './budgetLeftCardTestHelpers'
import { translated } from './translated'

export function nearWordsText(language: Language = 'en'): string {
  return translated(language, 'budget.limits.nearWords')
}

export function nearWordsOf(row: HTMLElement, language: Language = 'en'): HTMLElement | null {
  const words = nearWordsText(language)
  const matches = Array.from(row.querySelectorAll<HTMLElement>('*')).filter((element) => readableText(element) === words)
  return matches.at(-1) ?? null
}

export function requireNearWords(row: HTMLElement, language: Language = 'en'): HTMLElement {
  const words = nearWordsOf(row, language)
  if (words === null) {
    throw new Error(`The limit row has no line reading "${nearWordsText(language)}". Its text is: "${readableText(row)}"`)
  }
  return words
}

export function rowButtonDescribedByNearWords(row: HTMLElement, language: Language = 'en'): boolean {
  return within(row).queryByRole('button', { description: (description) => description.includes(nearWordsText(language)) }) !== null
}
