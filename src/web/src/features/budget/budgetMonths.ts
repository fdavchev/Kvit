import { monthOfDate, recentMonths } from '@/shared/utils/calendarMonth'

const budgetMonthCount = 24

export function budgetMonths(currentMonth: string): string[] {
  return recentMonths(currentMonth, budgetMonthCount)
}

export function earliestBudgetDate(today: string): string {
  return `${budgetMonths(monthOfDate(today))[0]}-01`
}
