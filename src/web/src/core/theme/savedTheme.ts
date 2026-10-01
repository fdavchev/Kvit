import { isThemeChoice, type ThemeChoice } from './theme'

const storageKey = 'kvit.theme'

export function readSavedTheme(): ThemeChoice {
  let saved: string | null
  try {
    saved = window.localStorage.getItem(storageKey)
  } catch (error) {
    console.warn(`Could not read "${storageKey}" from localStorage`, error)
    return 'system'
  }
  return saved !== null && isThemeChoice(saved) ? saved : 'system'
}

export function saveTheme(choice: ThemeChoice): void {
  try {
    window.localStorage.setItem(storageKey, choice)
  } catch (error) {
    console.warn(`Could not save "${storageKey}" to localStorage`, error)
  }
}
