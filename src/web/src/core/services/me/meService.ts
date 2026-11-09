import { ApiError, apiFileRequest, apiRequest, jsonRequest, unauthorizedStatus } from '@/core/api/apiClient'
import { endpoints } from '@/core/api/endpoints'
import { isLanguage, type Language } from '@/core/i18n/language'
import { groupCurrencies } from '@/core/services/groups/groupsService'
import { readOneOf, readOrNullWhenMissing, readTextOrNull } from '@/core/services/readFields'
import type { Currency } from '@/shared/utils/formatMoney'

export interface Me {
  id: string
  displayName: string
  email: string | null
  language: Language
  timeZone: string
  mustChangePassword: boolean
  hasPassword: boolean
  pictureUrl: string | null
  isGuest: boolean
  isAdmin: boolean
  termsVersion: string | null
  termsUpToDate: boolean
  termsUpdateIsMaterial: boolean
  deletionCancelled: boolean
  isSuspended: boolean
  budgetTotalCurrency: Currency | null
}

export type DeleteAccountNameChoice = 'KeepName' | 'FormerMember'

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
    email: readTextOrNull(fields, 'email', 'the signed-in person'),
    language: readLanguage(fields),
    timeZone: readText(fields, 'timeZone'),
    mustChangePassword: readYesNo(fields, 'mustChangePassword'),
    hasPassword: readYesNo(fields, 'hasPassword'),
    pictureUrl: readTextOrNull(fields, 'pictureUrl', 'the signed-in person'),
    isGuest: readYesNo(fields, 'isGuest'),
    isAdmin: readYesNo(fields, 'isAdmin'),
    termsVersion: readOrNullWhenMissing(fields, 'termsVersion', 'the signed-in person', readTextOrNull),
    termsUpToDate: readYesNoOrWhenMissing(fields, 'termsUpToDate', true),
    termsUpdateIsMaterial: readYesNoOrWhenMissing(fields, 'termsUpdateIsMaterial', false),
    deletionCancelled: readYesNoOrWhenMissing(fields, 'deletionCancelled', false),
    isSuspended: readYesNoOrWhenMissing(fields, 'isSuspended', false),
    budgetTotalCurrency: readBudgetTotalCurrency(fields),
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

export async function acceptTerms(): Promise<void> {
  await apiRequest(endpoints.meTermsAccept, { method: 'POST' })
}

export interface ExportedData {
  blob: Blob
  fileName: string
}

const exportFileNameWhenHeaderMissing = 'kvit-my-data.json'

export async function exportMyData(): Promise<ExportedData> {
  const file = await apiFileRequest(endpoints.meExport)
  return { blob: file.blob, fileName: file.fileName ?? exportFileNameWhenHeaderMissing }
}

export async function deleteMyAccount(nameChoice: DeleteAccountNameChoice): Promise<void> {
  await apiRequest(endpoints.meDelete, jsonRequest('POST', { nameChoice }))
}

export async function changeBudgetTotalCurrency(budgetTotalCurrency: Currency | null): Promise<void> {
  await apiRequest(endpoints.meBudgetCurrency, jsonRequest('PUT', { budgetTotalCurrency }))
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

function readYesNoOrWhenMissing(fields: MeFields, name: keyof Me, whenMissing: boolean): boolean {
  return Object.hasOwn(fields, name) ? readYesNo(fields, name) : whenMissing
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

function readBudgetTotalCurrency(fields: MeFields): Currency | null {
  return fields.budgetTotalCurrency === null
    ? null
    : readOneOf(fields, 'budgetTotalCurrency', groupCurrencies, 'the signed-in person')
}

function describeValue(value: unknown): string {
  return value === undefined ? 'nothing' : JSON.stringify(value)
}
