import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { legalNoticeTexts } from '@/test/legalNoticeTextsTestData'
import { translated } from '@/test/translated'
import en from './locales/en.json'
import mk from './locales/mk.json'

const placeholders = /\{\{\w+\}\}/g
const tags = /<\/?\w+>/g
const latinLetters = /[A-Za-zÀ-ɏ]/
const forbiddenMacedonianWords = ['курс', 'врск']

const languageTexts = [
  ['en', en],
  ['mk', mk],
] as const

function tagsOf(text: string): string[] {
  return (text.match(tags) ?? []).sort()
}

describe('the approved Phase 19 notice and agree-line texts', () => {
  it.each(legalNoticeTexts)('has the approved English and Macedonian wording for %s', (key, english, macedonian) => {
    expect(translated('en', key)).toBe(english)
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(legalNoticeTexts)('uses the same placeholders and tags in the English and the Macedonian text of %s', (_key, english, macedonian) => {
    expect((english.match(placeholders) ?? []).sort()).toEqual((macedonian.match(placeholders) ?? []).sort())
    expect(tagsOf(english)).toEqual(tagsOf(macedonian))
  })

  it.each(languageTexts)('has the agree line with one Terms tag and one Privacy tag, each opened and closed, in %s', (language) => {
    expect(tagsOf(translated(language, 'legal.agree'))).toEqual(['</privacy>', '</terms>', '<privacy>', '<terms>'])
  })

  it.each(languageTexts)('has the Terms tag before the Privacy tag in the %s agree line', (language) => {
    const text = translated(language, 'legal.agree')

    expect(text.indexOf('<terms>')).toBeLessThan(text.indexOf('<privacy>'))
  })

  it.each(legalNoticeTexts)('has no tags or placeholders in %s other than the agree line', (key, english, macedonian) => {
    if (key === 'legal.agree') {
      return
    }
    expect(english).not.toMatch(/[<>{]/)
    expect(macedonian).not.toMatch(/[<>{]/)
  })

  it.each(legalNoticeTexts)('writes the Macedonian text of %s in Cyrillic apart from the name Kvit', (_key, _english, macedonian) => {
    const withoutKvit = macedonian.replaceAll('Kvit', '').replace(tags, '')

    expect(withoutKvit).not.toMatch(latinLetters)
  })

  it.each(legalNoticeTexts)('uses none of the forbidden Macedonian words in %s', (_key, _english, macedonian) => {
    for (const word of forbiddenMacedonianWords) {
      expect(macedonian.toLowerCase()).not.toContain(word)
    }
  })

  it('has every listed key as a text, so the list and the locale files cannot drift', () => {
    const keysInEnglish = collectTexts(en).map(([path]) => path)
    const keysInMacedonian = collectTexts(mk).map(([path]) => path)

    for (const [key] of legalNoticeTexts) {
      expect(keysInEnglish).toContain(key)
      expect(keysInMacedonian).toContain(key)
    }
  })
})
