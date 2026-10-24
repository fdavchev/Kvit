import type { Language } from '@/core/i18n/language'
import { formatGroupedDecimal, groupedDecimalPieces } from './formatNumber'

export type Currency = 'MKD' | 'EUR'

export interface MoneyParts {
  language: Language
  sign: string
  currencyBefore: string
  unsignedDecimal: string
  currencyAfter: string
}

const minorUnitsPerMajor = 100n
const nonBreakingSpace = ' '
const denarSuffix: Record<Language, string> = { en: 'MKD', mk: 'ден.' }

export function formatMoney(
  amountMinor: number,
  currency: Currency,
  language: Language,
): string {
  return moneyText(moneyParts(amountMinor, currency, language))
}

export function currencyLabel(currency: Currency, language: Language): string {
  return currency === 'MKD' ? denarSuffix[language] : currency
}

export function moneyText(parts: MoneyParts): string {
  const number = formatGroupedDecimal(parts.unsignedDecimal, parts.language)
  return `${parts.sign}${parts.currencyBefore}${number}${parts.currencyAfter}`
}

export function moneyNumberPieces(parts: MoneyParts): string[] {
  return groupedDecimalPieces(parts.unsignedDecimal, parts.language).map((piece, index) =>
    index === 0 ? `${parts.sign}${parts.currencyBefore}${piece}` : piece,
  )
}

export function moneyParts(
  amountMinor: number,
  currency: Currency,
  language: Language,
): MoneyParts {
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
    return {
      language,
      sign,
      currencyBefore: '',
      unsignedDecimal: whole.toString(),
      currencyAfter: `${nonBreakingSpace}${denarSuffix[language]}`,
    }
  }

  return {
    language,
    sign,
    currencyBefore: '€',
    unsignedDecimal: `${whole}.${fraction.toString().padStart(2, '0')}`,
    currencyAfter: '',
  }
}
