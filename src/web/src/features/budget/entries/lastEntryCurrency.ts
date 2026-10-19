import { groupCurrencies } from '@/core/services/groups/groupsService'
import type { Currency } from '@/shared/utils/formatMoney'

const firstEntryCurrency: Currency = 'MKD'

function storageKeyOf(userId: string): string {
  return `kvit.lastEntryCurrency.${userId}`
}

export function readLastEntryCurrency(userId: string): Currency {
  const storageKey = storageKeyOf(userId)
  let saved: string | null
  try {
    saved = window.localStorage.getItem(storageKey)
  } catch (error) {
    console.warn(`Could not read "${storageKey}" from localStorage`, error)
    return firstEntryCurrency
  }
  return groupCurrencies.find((currency) => currency === saved) ?? firstEntryCurrency
}

export function saveLastEntryCurrency(userId: string, currency: Currency): void {
  const storageKey = storageKeyOf(userId)
  try {
    window.localStorage.setItem(storageKey, currency)
  } catch (error) {
    console.warn(`Could not save "${storageKey}" to localStorage`, error)
  }
}
