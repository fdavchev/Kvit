import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { licencesTexts } from '@/test/licencesTextsTestData'
import { legalNoticeTexts } from '@/test/legalNoticeTextsTestData'
import { legalTexts } from '@/test/legalTextsTestData'
import { translated } from '@/test/translated'
import en from './locales/en.json'
import mk from './locales/mk.json'

const legalNamespaces = /^(terms|privacy|legal)\./
const placeholders = /\{\{\w+\}\}/g
const claimsOfLegalSafety = /complian|legally safe|GDPR/i

const languageTexts = [
  ['en', en],
  ['mk', mk],
] as const

function keysUnderLegalNamespaces(texts: object): string[] {
  return collectTexts(texts)
    .map(([path]) => path)
    .filter((path) => legalNamespaces.test(path))
    .sort()
}

describe('the approved Phase 19 legal texts', () => {
  it.each(legalTexts)('has the approved English and Macedonian wording for %s', (key, english, macedonian) => {
    expect(translated('en', key)).toBe(english)
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(languageTexts)('has no text under terms, privacy or legal in %s that is not listed above, in legalNoticeTexts.test.ts or in licencesTexts.test.ts, so every line is locked and the old privacy lines are gone', (_language, texts) => {
    const keysListedAbove = [...legalTexts, ...legalNoticeTexts, ...licencesTexts]
      .map(([key]) => key)
      .filter((key) => legalNamespaces.test(key))
      .sort()

    expect(keysUnderLegalNamespaces(texts)).toEqual(keysListedAbove)
  })

  it('has the same keys under terms, privacy and legal in English and Macedonian', () => {
    expect(keysUnderLegalNamespaces(mk)).toEqual(keysUnderLegalNamespaces(en))
  })

  it.each(legalTexts)('uses the same placeholders in the English and the Macedonian text of %s', (_key, english, macedonian) => {
    expect((english.match(placeholders) ?? []).sort()).toEqual((macedonian.match(placeholders) ?? []).sort())
  })

  it.each(languageTexts)('never claims legal safety, compliance or GDPR in any %s text under terms, privacy or legal', (_language, texts) => {
    const offenders = collectTexts(texts)
      .filter(([path, text]) => legalNamespaces.test(path) && typeof text === 'string' && claimsOfLegalSafety.test(text))
      .map(([path]) => path)

    expect(offenders).toEqual([])
  })

  it.each([
    ['is compliant', true],
    ['Kvit is legally safe', true],
    ['Kvit follows the GDPR', true],
    ['follows the gdpr', true],
    ['Kvit keeps a record of who paid', false],
  ])('recognises "%s" as a claim of legal safety: %s', (text, isClaim) => {
    expect(claimsOfLegalSafety.test(text)).toBe(isClaim)
  })
})
