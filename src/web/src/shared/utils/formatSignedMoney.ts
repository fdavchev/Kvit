import type { Language } from '@/core/i18n/language'
import { formatMoney, type Currency } from './formatMoney'

const minusSign = '−'

export function formatSignedMoney(amountMinor: number, currency: Currency, language: Language): string {
  const unsigned = formatMoney(Math.abs(amountMinor), currency, language)
  if (amountMinor > 0) {
    return `+${unsigned}`
  }
  if (amountMinor < 0) {
    return `${minusSign}${unsigned}`
  }
  return unsigned
}
