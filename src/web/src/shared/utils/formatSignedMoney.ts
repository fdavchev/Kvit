import type { Language } from '@/core/i18n/language'
import { moneyParts, moneyText, type Currency, type MoneyParts } from './formatMoney'

const minusSign = '−'

export function formatSignedMoney(amountMinor: number, currency: Currency, language: Language): string {
  return moneyText(signedMoneyParts(amountMinor, currency, language))
}

export function signedMoneyParts(amountMinor: number, currency: Currency, language: Language): MoneyParts {
  const unsigned = moneyParts(Math.abs(amountMinor), currency, language)
  return { ...unsigned, sign: signOf(amountMinor) }
}

function signOf(amountMinor: number): string {
  if (amountMinor > 0) {
    return '+'
  }
  if (amountMinor < 0) {
    return minusSign
  }
  return ''
}
