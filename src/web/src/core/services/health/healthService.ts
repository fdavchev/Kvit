import { apiRequest } from '@/core/api/apiClient'
import { endpoints } from '@/core/api/endpoints'

export async function pingHealth(): Promise<string> {
  const body = await apiRequest(endpoints.health)
  if (typeof body !== 'string') {
    throw new Error(
      `Expected a text answer from ${endpoints.health}, got ${typeof body}`,
    )
  }
  return body
}
