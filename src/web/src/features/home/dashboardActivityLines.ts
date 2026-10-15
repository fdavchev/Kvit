import type { TFunction } from 'i18next'
import type { Language } from '@/core/i18n/language'
import type { DashboardActivityRow } from '@/core/services/dashboard/dashboardService'
import { activityLines } from '@/features/groups/activity/activityLines'

export interface DashboardActivityContext {
  t: TFunction
  language: Language
}

export interface DashboardActivityLine {
  key: string
  groupId: string
  groupName: string
  sentence: string
  createdAt: string
}

const fieldsOnlyEditSentencesRead = { today: '', categories: [], expenses: [] }

export function dashboardActivityLines(
  rows: readonly DashboardActivityRow[],
  context: DashboardActivityContext,
): DashboardActivityLine[] {
  return rows.flatMap((row) => {
    const lineOf = (key: string, sentence: string): DashboardActivityLine => ({
      key,
      groupId: row.groupId,
      groupName: row.groupName,
      sentence,
      createdAt: row.createdAt,
    })
    if (row.type === 'ExpenseEdited') {
      return [lineOf(row.id, context.t('home.activityExpenseEdited', { name: row.actorName }))]
    }
    return activityLines([row], { ...context, ...fieldsOnlyEditSentencesRead }).map((line) => lineOf(line.key, line.sentence))
  })
}
