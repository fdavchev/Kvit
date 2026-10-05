import type { Language } from '@/core/i18n/language'

const dayMonthYearFormats: Record<Language, Intl.DateTimeFormat> = {
  en: new Intl.DateTimeFormat('en-GB', { day: 'numeric', month: 'short', year: 'numeric' }),
  mk: new Intl.DateTimeFormat('mk', { day: 'numeric', month: 'long', year: 'numeric' }),
}

const dayMonthYearParts: readonly Intl.DateTimeFormatPartTypes[] = ['day', 'month', 'year']

export function formatDayMonthYear(isoDateTime: string, language: Language): string {
  const date = new Date(isoDateTime)
  if (Number.isNaN(date.getTime())) {
    throw new Error(`Expected a date and time to format, got "${isoDateTime}"`)
  }
  return dayMonthYearFormats[language]
    .formatToParts(date)
    .filter((part) => dayMonthYearParts.includes(part.type))
    .map((part) => part.value)
    .join(' ')
}
