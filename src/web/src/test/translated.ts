import type { Language } from '@/core/i18n/language'
import en from '@/core/i18n/locales/en.json'
import mk from '@/core/i18n/locales/mk.json'

const texts: Record<Language, unknown> = { en, mk }

export function translated(
  language: Language,
  key: string,
  params: Record<string, string> = {},
): string {
  const text = key
    .split('.')
    .reduce<unknown>(
      (node, part) =>
        typeof node === 'object' && node !== null
          ? Reflect.get(node, part)
          : undefined,
      texts[language],
    )
  if (typeof text !== 'string' || text === '') {
    throw new Error(
      `The ${language} locale has no text for the key "${key}" (missing, empty, or not a text)`,
    )
  }
  return Object.entries(params).reduce(
    (result, [name, value]) => result.replaceAll(`{{${name}}}`, value),
    text,
  )
}
