import { routes } from '@/core/router/routes'
import { renderGroupApp, type GroupAppOptions } from './activityTestHelpers'
import {
  eurPlanOfSixHundred,
  groupWithEurPlan,
  groupWithMkdPlan,
  mkdPlanOfTwoThousand,
  spendingWithPlanOf,
} from './groupPlanTestData'
import { findPlanCard, findSetPlanRow, openPlanSheetFrom, planControlOf } from './groupPlanTestHelpers'
import { mkdOnlySpending } from './groupSpendingTestData'
import { groupOf, testGroup } from './groupTestData'

export const groupWithoutPlan = groupOf({})

function renderTotals(options: GroupAppOptions) {
  return renderGroupApp(routes.groupTotals(testGroup.id), options)
}

export async function openNewPlanSheet(options: GroupAppOptions = {}) {
  const rendered = await renderTotals({ group: groupWithoutPlan, spending: mkdOnlySpending, ...options })
  const row = await findSetPlanRow(options.language)
  const sheet = await openPlanSheetFrom(row, options.language)
  return { ...rendered, row, sheet }
}

export async function openExistingPlanSheet(options: GroupAppOptions = {}) {
  const rendered = await renderTotals({
    group: groupWithMkdPlan,
    spending: spendingWithPlanOf(mkdPlanOfTwoThousand),
    ...options,
  })
  const card = await findPlanCard(options.language)
  const control = planControlOf(card)
  if (control === null) {
    throw new Error('The plan card has no control that opens the plan sheet')
  }
  const sheet = await openPlanSheetFrom(control, options.language)
  return { ...rendered, control, sheet }
}

export function openEuroPlanSheet(options: GroupAppOptions = {}) {
  return openExistingPlanSheet({ group: groupWithEurPlan, spending: spendingWithPlanOf(eurPlanOfSixHundred), ...options })
}
