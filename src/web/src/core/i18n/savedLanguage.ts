import type { Language } from './language'

const storageKey = 'kvit.language'

export function readSavedLanguage(): string | null {
  try {
    return window.localStorage.getItem(storageKey)
  } catch (error) {
    console.warn(`Could not read "${storageKey}" from localStorage`, error)
    return null
  }
}

export function saveLanguage(language: Language): void {
  try {
    window.localStorage.setItem(storageKey, language)
  } catch (error) {
    console.warn(`Could not save "${storageKey}" to localStorage`, error)
  }
}
