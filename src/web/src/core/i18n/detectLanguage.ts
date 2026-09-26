import { isLanguage, type Language } from './language'

export function detectLanguage(
  savedLanguage: string | null,
  browserLanguages: readonly string[],
): Language {
  if (savedLanguage !== null && isLanguage(savedLanguage)) {
    return savedLanguage
  }
  for (const browserLanguage of browserLanguages) {
    const baseLanguage = browserLanguage.toLowerCase().split('-')[0]
    if (isLanguage(baseLanguage)) {
      return baseLanguage
    }
  }
  return 'en'
}
