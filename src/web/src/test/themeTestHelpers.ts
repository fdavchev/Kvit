import type { Language } from '@/core/i18n/language'
import type { ThemeChoice } from '@/core/theme/theme'
import { translated } from './translated'

export type ColorScheme = 'light' | 'dark'

export const themeColors: Record<ColorScheme, string> = {
  light: '#ffd9a8',
  dark: '#351f1b',
}

const themeTextKeys: Record<ThemeChoice, string> = {
  system: 'settings.themeSystem',
  light: 'settings.themeLight',
  dark: 'settings.themeDark',
}

export function resetThemeDocument(): void {
  document.documentElement.removeAttribute('data-theme')
  document
    .querySelectorAll('meta[name="theme-color"]')
    .forEach((meta) => meta.remove())
  for (const scheme of ['light', 'dark'] as const) {
    const meta = document.createElement('meta')
    meta.name = 'theme-color'
    meta.content = themeColors[scheme]
    meta.setAttribute('media', `(prefers-color-scheme: ${scheme})`)
    document.head.appendChild(meta)
  }
}

export function themeColorOf(scheme: ColorScheme): string | undefined {
  return document
    .querySelector(`meta[name="theme-color"][media="(prefers-color-scheme: ${scheme})"]`)
    ?.getAttribute('content')
    ?.toLowerCase()
}

export function themeColorTagCount(): number {
  return document.querySelectorAll('meta[name="theme-color"]').length
}

export function appliedTheme(): string | undefined {
  return document.documentElement.dataset.theme
}

export function themeButtonName(language: Language, choice: ThemeChoice): string {
  return translated(language, themeTextKeys[choice])
}

export function themeGroupName(language: Language): string {
  return translated(language, 'settings.theme')
}
