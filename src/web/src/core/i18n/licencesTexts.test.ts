import { describe, expect, it } from 'vitest'
import { licencesTexts } from '@/test/licencesTextsTestData'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import en from './locales/en.json'
import mk from './locales/mk.json'

const latinLetters = /[A-Za-zÀ-ɏ]/
const markup = /[<>{]/

describe('the approved third-party licences link text', () => {
  it.each(licencesTexts)('has the approved English and Macedonian wording for %s', (key, english, macedonian) => {
    expect(translated('en', key)).toBe(english)
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(licencesTexts)('has no tags or placeholders in %s', (_key, english, macedonian) => {
    expect(english).not.toMatch(markup)
    expect(macedonian).not.toMatch(markup)
  })

  it.each(licencesTexts)('writes the Macedonian text of %s in Cyrillic', (_key, _english, macedonian) => {
    expect(macedonian).not.toMatch(latinLetters)
  })

  it('has every listed key as a text in English and in Macedonian, so the list and the locale files cannot drift', () => {
    const keysInEnglish = collectTexts(en).map(([path]) => path)
    const keysInMacedonian = collectTexts(mk).map(([path]) => path)

    for (const [key] of licencesTexts) {
      expect(keysInEnglish).toContain(key)
      expect(keysInMacedonian).toContain(key)
    }
  })
})
