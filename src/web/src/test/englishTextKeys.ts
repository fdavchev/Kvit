import en from '@/core/i18n/locales/en.json'
import { collectTexts } from './localeTexts'
import { translated } from './translated'

export function keyOfEnglishText(english: string): string {
  const found = collectTexts(en).find(([, text]) => text === english)
  if (found === undefined) {
    throw new Error(`No text in en.json is exactly "${english}"`)
  }
  return found[0]
}

export function macedonianOf(english: string): string {
  return translated('mk', keyOfEnglishText(english))
}
