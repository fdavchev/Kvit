import type { TFunction } from 'i18next'
import type { Language } from './language'

export type PluralForm = 'one' | 'other'

export function pluralForm(count: number, language: Language): PluralForm {
  if (!Number.isSafeInteger(count) || count < 0) {
    throw new Error(`Expected a whole count of 0 or more to pick a plural form, got ${count}`)
  }
  if (language === 'en') {
    return count === 1 ? 'one' : 'other'
  }
  return count % 10 === 1 && count % 100 !== 11 ? 'one' : 'other'
}

export function countedText(t: TFunction, key: string, count: number, language: Language): string {
  return t(`${key}_${pluralForm(count, language)}`, { replace: { count } })
}
