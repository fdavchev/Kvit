import { apiRequest, jsonRequest } from '@/core/api/apiClient'
import { endpoints } from '@/core/api/endpoints'
import type { Language } from '@/core/i18n/language'
import { parseMe, type Me } from '@/core/services/me/meService'

export interface RegisterInput {
  displayName: string
  email: string
  password: string
  language: Language
}

export interface LogInInput {
  email: string
  password: string
}

export interface ChangePasswordInput {
  currentPassword: string
  newPassword: string
}

export async function register(input: RegisterInput): Promise<Me> {
  const body = await apiRequest(
    endpoints.register,
    jsonRequest('POST', { ...input, timeZone: readDeviceTimeZone() }),
  )
  return parseMe(body)
}

export async function logIn(input: LogInInput): Promise<Me> {
  const body = await apiRequest(
    endpoints.logIn,
    jsonRequest('POST', { ...input, timeZone: readDeviceTimeZone() }),
  )
  return parseMe(body)
}

export async function logOut(): Promise<void> {
  await apiRequest(endpoints.logOut, { method: 'POST' })
}

export async function changePassword(input: ChangePasswordInput): Promise<void> {
  await apiRequest(endpoints.changePassword, jsonRequest('POST', input))
}

function readDeviceTimeZone(): string {
  return new Intl.DateTimeFormat().resolvedOptions().timeZone ?? ''
}
