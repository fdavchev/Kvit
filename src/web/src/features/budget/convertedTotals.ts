import type { Currency } from '@/shared/utils/formatMoney'

export const inCurrencyKeys: Record<Currency, string> = { MKD: 'budget.card.inMkd', EUR: 'budget.card.inEur' }
export const includesConvertedKeys: Record<Currency, string> = {
  MKD: 'budget.card.includesConvertedEuro',
  EUR: 'budget.card.includesConvertedDenars',
}

export function convertedTotalsInSetting<Totals extends { currency: Currency }>(
  convertedTotals: Totals | null,
  budgetTotalCurrency: Currency | null,
): Totals | null {
  return convertedTotals !== null && convertedTotals.currency === budgetTotalCurrency ? convertedTotals : null
}
