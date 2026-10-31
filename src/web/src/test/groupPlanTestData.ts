import type { Group } from '@/core/services/groups/groupsService'
import type { Currency } from '@/shared/utils/formatMoney'
import { groupPath } from './expenseTestHelpers'
import { mkdOnlySpending } from './groupSpendingTestData'
import { groupOf } from './groupTestData'

export const planPath = `${groupPath}/plan`
export const planPut = `PUT ${planPath}`
export const planDelete = `DELETE ${planPath}`

export const tenThousandDenarsMinor = 1000000

export interface PlanJson {
  amountMinor: number
  currency: Currency
  spentMinor: number
  perPersonMinor: number
  overMinor: number
  includedOtherCurrencyMinor: number
}

export function planOf(changes: Partial<PlanJson> = {}): PlanJson {
  return {
    amountMinor: tenThousandDenarsMinor,
    currency: 'MKD',
    spentMinor: 0,
    perPersonMinor: tenThousandDenarsMinor,
    overMinor: 0,
    includedOtherCurrencyMinor: 0,
    ...changes,
  }
}

export function spendingWithPlanOf(changes: Partial<PlanJson> = {}) {
  return { ...mkdOnlySpending, plan: planOf(changes) }
}

export function spendingOfPlanSpent(spentMinor: number, amountMinor = tenThousandDenarsMinor, currency: Currency = 'MKD') {
  return spendingWithPlanOf({
    amountMinor,
    currency,
    spentMinor,
    perPersonMinor: amountMinor,
    overMinor: Math.max(0, spentMinor - amountMinor),
  })
}

export function groupWithPlan(plan: Pick<PlanJson, 'amountMinor' | 'currency'>, changes: Partial<Group> = {}): Group {
  return groupOf({ planAmountMinor: plan.amountMinor, planCurrency: plan.currency, ...changes })
}

export const mkdPlanOfTwoThousand = planOf({
  amountMinor: 200000,
  spentMinor: 135000,
  perPersonMinor: 200000,
})

export const eurPlanOfSixHundred = planOf({
  amountMinor: 60000,
  currency: 'EUR',
  spentMinor: 15000,
  perPersonMinor: 60000,
})

export const groupWithMkdPlan: Group = groupWithPlan(mkdPlanOfTwoThousand)
export const groupWithEurPlan: Group = groupWithPlan(eurPlanOfSixHundred)

export interface BoundCase {
  label: string
  text: string
  currency: Currency
  isValid: boolean
}

export const mkdAmountCases: readonly BoundCase[] = [
  { label: 'a whole number of denars', text: '1500', currency: 'MKD', isValid: true },
  { label: 'the biggest allowed amount', text: '9999999999', currency: 'MKD', isValid: true },
  { label: 'one denar above the bound', text: '10000000000', currency: 'MKD', isValid: false },
  { label: 'zero', text: '0', currency: 'MKD', isValid: false },
  { label: 'zeros only', text: '000', currency: 'MKD', isValid: false },
  { label: 'a number with deni', text: '12.5', currency: 'MKD', isValid: false },
  { label: 'a number with a decimal comma', text: '12,5', currency: 'MKD', isValid: false },
  { label: 'letters', text: 'abc', currency: 'MKD', isValid: false },
  { label: 'a minus sign', text: '-5', currency: 'MKD', isValid: false },
  { label: 'a number with spaces inside', text: '1 500', currency: 'MKD', isValid: false },
]

export const eurAmountCases: readonly BoundCase[] = [
  { label: 'a whole number of euros', text: '600', currency: 'EUR', isValid: true },
  { label: 'euros and cents', text: '600.50', currency: 'EUR', isValid: true },
  { label: 'euros and one digit of cents', text: '12.5', currency: 'EUR', isValid: true },
  { label: 'euros and cents with a comma', text: '12,50', currency: 'EUR', isValid: true },
  { label: 'the biggest allowed amount', text: '9999999999.99', currency: 'EUR', isValid: true },
  { label: 'one euro above the bound', text: '10000000000', currency: 'EUR', isValid: false },
  { label: 'zero', text: '0', currency: 'EUR', isValid: false },
  { label: 'zero cents', text: '0.00', currency: 'EUR', isValid: false },
  { label: 'three digits of cents', text: '12.345', currency: 'EUR', isValid: false },
  { label: 'letters', text: 'abc', currency: 'EUR', isValid: false },
  { label: 'a minus sign', text: '-5', currency: 'EUR', isValid: false },
]
