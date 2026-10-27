import { apiRequest, jsonRequest } from '@/core/api/apiClient'
import { endpoints } from '@/core/api/endpoints'

export async function setMonthLimit(limitMinor: number): Promise<void> {
  await apiRequest(endpoints.personalLimitsMonth, jsonRequest('PUT', { limitMinor }))
}

export async function setCategoryLimit(categoryId: string, limitMinor: number): Promise<void> {
  await apiRequest(endpoints.personalLimitsCategory(categoryId), jsonRequest('PUT', { limitMinor }))
}

export async function removeMonthLimit(): Promise<void> {
  await apiRequest(endpoints.personalLimitsMonth, { method: 'DELETE' })
}

export async function removeCategoryLimit(categoryId: string): Promise<void> {
  await apiRequest(endpoints.personalLimitsCategory(categoryId), { method: 'DELETE' })
}
