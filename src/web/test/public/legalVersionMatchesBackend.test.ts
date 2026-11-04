import { readFileSync } from 'node:fs'
import path from 'node:path'
import { describe, expect, it } from 'vitest'
import { legalVersion } from '@/core/legal/legalInfo'

const termsVersionPath = path.resolve(
  import.meta.dirname,
  '../../../api/Kvit.Domain/Accounts/TermsVersion.cs',
)
const currentDeclaration = /public const string Current = "([^"]*)";/

function readBackendVersion(): string {
  const source = readFileSync(termsVersionPath, 'utf8')
  const found = currentDeclaration.exec(source)
  if (found === null) {
    throw new Error(`Found no 'public const string Current = "..."' in ${termsVersionPath}`)
  }
  return found[1]
}

describe('the version of the legal texts', () => {
  it('reads a date from TermsVersion.cs, so a broken pattern cannot pass silently', () => {
    expect(readBackendVersion()).toMatch(/^\d{4}-\d{2}-\d{2}$/)
  })

  it('is the same in the website (legalInfo.ts) and in the API (TermsVersion.Current)', () => {
    expect(legalVersion).toBe(readBackendVersion())
  })
})
