import type { Language } from '@/core/i18n/language'
import { translated } from './translated'

const termsTag = /<terms>(.*?)<\/terms>/
const privacyTag = /<privacy>(.*?)<\/privacy>/
const anyTag = /<\/?\w+>/g

export interface AgreeLineLabels {
  terms: string
  privacy: string
}

export function agreeLineText(language: Language = 'en'): string {
  return translated(language, 'legal.agree').replace(anyTag, '')
}

export function agreeLineLabels(language: Language = 'en'): AgreeLineLabels {
  const text = translated(language, 'legal.agree')
  const terms = termsTag.exec(text)?.[1]
  const privacy = privacyTag.exec(text)?.[1]
  if (terms === undefined || privacy === undefined) {
    throw new Error(`The ${language} text of legal.agree has no <terms> or no <privacy> tag: ${text}`)
  }
  return { terms, privacy }
}

export function agreeLines(language: Language = 'en'): HTMLElement[] {
  const text = agreeLineText(language)
  return Array.from(document.body.querySelectorAll<HTMLElement>('*')).filter(
    (element) =>
      element.textContent === text &&
      !Array.from(element.children).some((child) => child.textContent === text),
  )
}

export function agreeLine(language: Language = 'en'): HTMLElement {
  const lines = agreeLines(language)
  if (lines.length !== 1) {
    throw new Error(`Expected exactly one agree line on the screen, found ${lines.length}`)
  }
  return lines[0]
}

export function isBefore(first: Node, second: Node): boolean {
  return (first.compareDocumentPosition(second) & Node.DOCUMENT_POSITION_FOLLOWING) !== 0
}
