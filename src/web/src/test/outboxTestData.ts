import { ApiError } from '@/core/api/apiClient'
import type { OutboxItem } from '@/core/outbox/outboxItem'

export function testOutboxItem(id: string, overrides: Partial<OutboxItem> = {}): OutboxItem {
  return {
    id,
    kind: 'expense',
    groupId: 'group-1',
    payload: { clientRequestId: id, title: 'Dinner', amountMinor: 120000, currency: 'MKD' },
    display: { title: 'Dinner', amountMinor: 120000, currency: 'MKD' },
    createdAt: '2026-10-31T10:00:00.000Z',
    state: 'waiting',
    failure: null,
    ...overrides,
  }
}

export function testApiError(httpStatus: number | null, errorCode: string | null = null): ApiError {
  return new ApiError(
    httpStatus === null ? 'POST /api/test could not reach the server' : `POST /api/test failed with status ${httpStatus}`,
    { httpStatus, errorCode },
  )
}
