import type { Language } from '@/core/i18n/language'

export type Currency = 'MKD' | 'EUR'

const minorUnitsPerMajor = 100n
const nonBreakingSpace = ' '
const denarSuffix: Record<Language, string> = { en: 'MKD', mk: 'ден.' }

export function formatMoney(
  amountMinor: number,
  currency: Currency,
  language: Language,
): string {
  if (!Number.isSafeInteger(amountMinor)) {
    throw new Error(
      `Money amount must be a safe integer of minor units, got ${amountMinor}`,
    )
  }
  const absoluteMinor = BigInt(Math.abs(amountMinor))
  const whole = absoluteMinor / minorUnitsPerMajor
  const fraction = absoluteMinor % minorUnitsPerMajor
  const sign = amountMinor < 0 ? '-' : ''

  if (currency === 'MKD') {
    if (fraction !== 0n) {
      throw new Error(
        `MKD amount must be whole denars (a multiple of 100 deni), got ${amountMinor}`,
      )
    }
    const number = new Intl.NumberFormat(language, {
      useGrouping: 'always',
    }).format(whole)
    return `${sign}${number}${nonBreakingSpace}${denarSuffix[language]}`
  }

  const exactDecimal = `${whole}.${fraction.toString().padStart(2, '0')}`
  const number = new Intl.NumberFormat(language, {
    useGrouping: 'always',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(exactDecimal as Intl.StringNumericLiteral)
  return `${sign}€${number}`
}
