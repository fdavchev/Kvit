import type { ThemeChoice } from './theme'

export type ColorScheme = 'light' | 'dark'

const darkSchemeQuery = '(prefers-color-scheme: dark)'

export function resolveColorScheme(choice: ThemeChoice): ColorScheme {
  if (choice !== 'system') {
    return choice
  }
  return window.matchMedia(darkSchemeQuery).matches ? 'dark' : 'light'
}
