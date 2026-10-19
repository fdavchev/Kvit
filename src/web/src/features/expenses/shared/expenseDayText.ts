import type { TFunction } from 'i18next'
import type { Language } from '@/core/i18n/language'
import { addDays, formatExpenseDay } from '@/shared/utils/formatDate'

export function expenseDayText(date: string, today: string, language: Language, t: TFunction): string {
  if (date === today) {
    return t('expenses.today')
  }
  if (date === addDays(today, -1)) {
    return t('expenses.yesterday')
  }
  return formatExpenseDay(date, today, language)
}
