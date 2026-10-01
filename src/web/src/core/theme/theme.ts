export const themeChoices = ['system', 'light', 'dark'] as const

export type ThemeChoice = (typeof themeChoices)[number]

export function isThemeChoice(value: string): value is ThemeChoice {
  return (themeChoices as readonly string[]).includes(value)
}
