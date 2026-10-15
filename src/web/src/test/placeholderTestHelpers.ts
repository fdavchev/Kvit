import { expect } from 'vitest'
import { contrastBetween, hasColorToken, type ThemeName } from './indexCssColors'

const minimumPlaceholderContrast = 4.5
const themes: readonly ThemeName[] = ['light', 'dark']

function placeholderUtilityOf(classNames: readonly string[], theme: ThemeName): string | null {
  const prefixes = theme === 'dark' ? ['placeholder:text-', 'dark:placeholder:text-'] : ['placeholder:text-']
  let found: string | null = null
  for (const prefix of prefixes) {
    for (const className of classNames) {
      if (className.startsWith(prefix)) {
        found = className.slice(prefix.length)
      }
    }
  }
  return found
}

export function placeholderTokenOf(input: HTMLElement, theme: ThemeName): string {
  const token = placeholderUtilityOf(Array.from(input.classList), theme)
  if (token === null) {
    throw new Error(`The input has no placeholder:text-<colour> class for ${theme} mode, so the browser default grey is used: "${input.className}"`)
  }
  if (token.includes('/')) {
    throw new Error(`The placeholder colour "${token}" is faded with an opacity in ${theme} mode, which loses contrast: "${input.className}"`)
  }
  if (!hasColorToken(token)) {
    throw new Error(`The placeholder colour "${token}" is not a colour token of index.css: "${input.className}"`)
  }
  return token
}

export function expectPlaceholderLegibleOn(input: HTMLElement, backgroundTokens: readonly string[]): void {
  for (const theme of themes) {
    const token = placeholderTokenOf(input, theme)
    for (const backgroundToken of backgroundTokens) {
      expect(
        contrastBetween(token, backgroundToken, theme),
        `placeholder:text-${token} on ${backgroundToken} in ${theme} mode`,
      ).toBeGreaterThanOrEqual(minimumPlaceholderContrast)
    }
  }
}
