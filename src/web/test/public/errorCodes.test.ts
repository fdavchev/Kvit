import { readFileSync } from 'node:fs'
import path from 'node:path'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { errorMessageKey } from '@/core/api/errors'
import en from '@/core/i18n/locales/en.json'
import mk from '@/core/i18n/locales/mk.json'

const resultCodesPath = path.resolve(
  import.meta.dirname,
  '../../../api/Kvit.Domain/Results/ResultCodes.cs',
)
const minimumExpectedCodeCount = 40
const badRequestStatus = 400
const codeDeclaration = /public const string (\w+) = "([^"]*)";/g

type ResultCode = { name: string; value: string }

function readResultCodes(): ResultCode[] {
  const source = readFileSync(resultCodesPath, 'utf8')
  return [...source.matchAll(codeDeclaration)].map((match) => ({
    name: match[1],
    value: match[2],
  }))
}

function errorTextOf(
  errors: Record<string, unknown>,
  code: string,
): string | undefined {
  const text = errors[code]
  return typeof text === 'string' ? text : undefined
}

const resultCodes = readResultCodes()
const codeNames = resultCodes.map((code) => code.name)

describe('the codes read from ResultCodes.cs', () => {
  it(`finds more than ${minimumExpectedCodeCount} codes, so a broken pattern cannot pass silently`, () => {
    expect(
      resultCodes.length,
      `Only ${resultCodes.length} codes were read from ${resultCodesPath}`,
    ).toBeGreaterThan(minimumExpectedCodeCount)
  })

  it.each(codeNames)('has the same name and value for %s', (name) => {
    const code = resultCodes.find((candidate) => candidate.name === name)

    expect(code?.value, `The value of ${name} differs from its name`).toBe(name)
  })

  it('lists every code once', () => {
    const duplicates = codeNames.filter(
      (name, index) => codeNames.indexOf(name) !== index,
    )

    expect(duplicates).toEqual([])
  })
})

describe('every backend error code', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it.each(codeNames)('%s is in the list that errors.ts translates', (code) => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    const error = new ApiError(`code ${code}`, {
      httpStatus: badRequestStatus,
      errorCode: code,
    })

    const key = errorMessageKey(error)

    expect(
      key,
      `${code} is missing from translatedErrorCodes in errors.ts`,
    ).toBe(`errors.${code}`)
    expect(consoleError).not.toHaveBeenCalled()
  })

  it.each(codeNames)('%s has a non-empty English text', (code) => {
    const text = errorTextOf(en.errors, code)

    expect(
      text,
      `errors.${code} has no English text in en.json`,
    ).toBeTypeOf('string')
    expect(text?.trim(), `errors.${code} is empty in en.json`).not.toBe('')
  })

  it.each(codeNames)('%s has a non-empty Macedonian text', (code) => {
    const text = errorTextOf(mk.errors, code)

    expect(
      text,
      `errors.${code} has no Macedonian text in mk.json`,
    ).toBeTypeOf('string')
    expect(text?.trim(), `errors.${code} is empty in mk.json`).not.toBe('')
  })
})
