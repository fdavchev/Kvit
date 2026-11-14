import { screen } from '@testing-library/react'
import type { Language } from '@/core/i18n/language'
import { translated } from './translated'

export type LegalDocument = 'terms' | 'privacy'

export const supportEmailText = 'kvit.support@gmail.com'
export const supportMailtoHref = `mailto:${supportEmailText}`
export const shortItemCount = 5

export function numbersUpTo(count: number): number[] {
  return Array.from({ length: count }, (_, index) => index + 1)
}

export function shortTitle(language: Language, document: LegalDocument): string {
  return translated(language, `${document}.short.title`)
}

export function shortItems(language: Language, document: LegalDocument): string[] {
  return numbersUpTo(shortItemCount).map((number) =>
    translated(language, `${document}.short.items.${number}`),
  )
}

export function sectionHeading(language: Language, document: LegalDocument, number: number): string {
  return translated(language, `${document}.sections.${number}.heading`)
}

export function sectionBody(language: Language, document: LegalDocument, number: number): string {
  return translated(language, `${document}.sections.${number}.body`)
}

export function emailCountIn(text: string): number {
  return text.split(supportEmailText).length - 1
}

export function countOfEmailsInDocument(
  language: Language,
  document: LegalDocument,
  sectionCount: number,
): number {
  const texts = [
    ...shortItems(language, document),
    ...numbersUpTo(sectionCount).map((number) => sectionBody(language, document, number)),
  ]
  return texts.reduce((count, text) => count + emailCountIn(text), 0)
}

export function elementShowingText(text: string): HTMLElement {
  return screen.getAllByText(
    (_content, element) =>
      element !== null && ['P', 'LI'].includes(element.tagName) && element.textContent === text,
  )[0]
}

export function mailtoLinks(): HTMLElement[] {
  return screen
    .queryAllByRole('link')
    .filter((link) => link.getAttribute('href')?.startsWith('mailto:') === true)
}
