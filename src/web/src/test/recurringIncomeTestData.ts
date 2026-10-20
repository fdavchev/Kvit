import type { RecurringIncome } from '@/core/services/recurringIncomes/recurringIncomesService'
import type { AnswerFactory } from './requestTestHelpers'

export const recurringIncomesUrl = '/api/personal/recurring-incomes'
export const catchUpUrl = '/api/personal/recurring-incomes/catch-up'

export const recurringIncomesGet = `GET ${recurringIncomesUrl}`
export const recurringIncomesPost = `POST ${recurringIncomesUrl}`
export const catchUpPost = `POST ${catchUpUrl}`

export function recurringIncomeUrl(recurringIncomeId: string): string {
  return `${recurringIncomesUrl}/${recurringIncomeId}`
}

export function recurringIncomePut(recurringIncomeId: string): string {
  return `PUT ${recurringIncomeUrl(recurringIncomeId)}`
}

export function recurringIncomeStopPost(recurringIncomeId: string): string {
  return `POST ${recurringIncomeUrl(recurringIncomeId)}/stop`
}

export function recurringIncomeRestartPost(recurringIncomeId: string): string {
  return `POST ${recurringIncomeUrl(recurringIncomeId)}/restart`
}

export const salaryRecurringIncome: RecurringIncome = {
  id: 'a0000000-0000-4000-8000-000000000001',
  amountMinor: 3000000,
  currency: 'MKD',
  title: 'Salary',
  frequency: 'Monthly',
  dayOfMonth: 1,
  dayOfWeek: null,
  startsOn: '2026-08-01',
  endsOn: null,
  createdAt: '2026-08-01T07:00:00Z',
  nextDate: '2026-11-01',
}

export const flatRentRecurringIncome: RecurringIncome = {
  id: 'a0000000-0000-4000-8000-000000000002',
  amountMinor: 25000,
  currency: 'EUR',
  title: 'Flat rent',
  frequency: 'Monthly',
  dayOfMonth: 5,
  dayOfWeek: null,
  startsOn: '2026-06-05',
  endsOn: null,
  createdAt: '2026-06-05T07:00:00Z',
  nextDate: '2026-11-05',
}

export const tutoringRecurringIncome: RecurringIncome = {
  id: 'a0000000-0000-4000-8000-000000000003',
  amountMinor: 150000,
  currency: 'MKD',
  title: 'Tutoring',
  frequency: 'Weekly',
  dayOfMonth: null,
  dayOfWeek: 2,
  startsOn: '2026-09-01',
  endsOn: '2027-01-20',
  createdAt: '2026-09-01T07:00:00Z',
  nextDate: '2026-10-20',
}

export const untitledRecurringIncome: RecurringIncome = {
  id: 'a0000000-0000-4000-8000-000000000004',
  amountMinor: 4000,
  currency: 'EUR',
  title: null,
  frequency: 'Monthly',
  dayOfMonth: 15,
  dayOfWeek: null,
  startsOn: '2026-01-15',
  endsOn: '2026-10-15',
  createdAt: '2026-01-15T07:00:00Z',
  nextDate: null,
}

export function recurringIncomeAnswerRow(income: RecurringIncome): Record<string, unknown> {
  return { ...income }
}

export function recurringListAnswerBody(incomes: readonly RecurringIncome[]): Record<string, unknown> {
  return { recurringIncomes: incomes.map(recurringIncomeAnswerRow) }
}

export function recurringListAnswer(incomes: readonly RecurringIncome[]): AnswerFactory {
  return () => Response.json(recurringListAnswerBody(incomes))
}

export function catchUpAnswer(created: number): AnswerFactory {
  return () => Response.json({ created })
}

export interface LiveRecurringList {
  answer: AnswerFactory
  set: (incomes: readonly RecurringIncome[]) => void
}

export function liveRecurringList(initial: readonly RecurringIncome[]): LiveRecurringList {
  let current = initial
  return {
    answer: () => Response.json(recurringListAnswerBody(current)),
    set: (incomes) => {
      current = incomes
    },
  }
}
