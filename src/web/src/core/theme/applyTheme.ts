import type { ColorScheme } from './colorScheme'
import type { ThemeChoice } from './theme'

const colorSchemes: readonly ColorScheme[] = ['light', 'dark']

const statusBarColors: Record<ColorScheme, string> = {
  light: '#ffd9a8',
  dark: '#351f1b',
}

export function applyTheme(choice: ThemeChoice): void {
  const root = document.documentElement
  if (choice === 'system') {
    delete root.dataset.theme
  } else {
    root.dataset.theme = choice
  }
  for (const scheme of colorSchemes) {
    themeColorTag(scheme).content = statusBarColors[choice === 'system' ? scheme : choice]
  }
}

function themeColorTag(scheme: ColorScheme): HTMLMetaElement {
  const tag = document.querySelector<HTMLMetaElement>(
    `meta[name="theme-color"][media="(prefers-color-scheme: ${scheme})"]`,
  )
  if (tag === null) {
    throw new Error(`The page has no theme-color tag for the ${scheme} colour scheme`)
  }
  return tag
}
