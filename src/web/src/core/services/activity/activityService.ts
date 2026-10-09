import { apiRequest } from '@/core/api/apiClient'
import { endpoints } from '@/core/api/endpoints'
import { readChanges, type ExpenseChange } from '@/core/services/expenses/expensesService'
import {
  readDate,
  readList,
  readObject,
  readText,
  readTextOrNull,
  type Fields,
} from '@/core/services/readFields'

export interface ActivityEvent {
  id: string
  type: string
  actorUserId: string
  actorName: string
  expenseId: string | null
  memberId: string | null
  changes: ExpenseChange[] | null
  data: Fields | null
  createdAt: string
}

export async function getActivity(groupId: string): Promise<ActivityEvent[]> {
  const what = 'the activity of the group'
  const fields = readObject(await apiRequest(endpoints.groupActivity(groupId)), what)
  return readList(fields, 'events', what).map(parseActivityEvent)
}

function parseActivityEvent(row: unknown): ActivityEvent {
  const what = 'an activity event'
  const fields = readObject(row, what)
  return {
    id: readText(fields, 'id', what),
    type: readText(fields, 'type', what),
    actorUserId: readText(fields, 'actorUserId', what),
    actorName: readText(fields, 'actorName', what),
    expenseId: readTextOrNull(fields, 'expenseId', what),
    memberId: readTextOrNull(fields, 'memberId', what),
    changes: readChanges(fields, what),
    data: fields.data === null ? null : readObject(fields.data, `"data" of ${what}`),
    createdAt: readDate(fields, 'createdAt', what),
  }
}
