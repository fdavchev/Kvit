import { ApiError, apiRequest, jsonRequest, unauthorizedStatus } from '@/core/api/apiClient'
import { endpoints } from '@/core/api/endpoints'
import { isLanguage, type Language } from '@/core/i18n/language'

export interface Me {
  id: string
  displayName: string
  email: string
  language: Language
  timeZone: string
  mustChangePassword: boolean
  hasPassword: boolean
}

type MeFields = Record<string, unknown>

export function parseMe(body: unknown): Me {
  if (typeof body !== 'object' || body === null) {
    throw new Error(
      `Expected the signed-in person as a JSON object, got ${describeValue(body)}`,
    )
  }
  const fields = body as MeFields
  return {
    id: readText(fields, 'id'),
    displayName: readText(fields, 'displayName'),
    email: readText(fields, 'email'),
    language: readLanguage(fields),
    timeZone: readText(fields, 'timeZone'),
    mustChangePassword: readYesNo(fields, 'mustChangePassword'),
    hasPassword: readYesNo(fields, 'hasPassword'),
  }
}

export async function getMe(): Promise<Me | null> {
  let body: unknown
  try {
    body = await apiRequest(endpoints.me)
  } catch (error) {
    if (error instanceof ApiError && error.httpStatus === unauthorizedStatus) {
      return null
    }
    throw error
  }
  return parseMe(body)
}

export async function changeLanguage(language: Language): Promise<void> {
  await apiRequest(endpoints.meLanguage, jsonRequest('PUT', { language }))
}

function readText(fields: MeFields, name: keyof Me): string {
  const value = fields[name]
  if (typeof value !== 'string') {
    throw new Error(
      `Expected the signed-in person's "${name}" to be a text, got ${describeValue(value)}`,
    )
  }
  return value
}

function readYesNo(fields: MeFields, name: keyof Me): boolean {
  const value = fields[name]
  if (typeof value !== 'boolean') {
    throw new Error(
      `Expected the signed-in person's "${name}" to be true or false, got ${describeValue(value)}`,
    )
  }
  return value
}

function readLanguage(fields: MeFields): Language {
  const value = fields.language
  if (typeof value !== 'string' || !isLanguage(value)) {
    throw new Error(
      `Expected the signed-in person's "language" to be a supported language, got ${describeValue(value)}`,
    )
  }
  return value
}

function describeValue(value: unknown): string {
  return value === undefined ? 'nothing' : JSON.stringify(value)
}
