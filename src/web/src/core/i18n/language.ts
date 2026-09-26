export const languages = ['en', 'mk'] as const

export type Language = (typeof languages)[number]

export function isLanguage(value: string): value is Language {
  return (languages as readonly string[]).includes(value)
}
