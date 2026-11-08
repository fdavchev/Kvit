import { describe, expect, it } from 'vitest'
import { testGroupId } from '@/test/groupTestData'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import en from './locales/en.json'
import mk from './locales/mk.json'

const placeholders = /\{\{\w+\}\}/g
const tags = /<\/?\w+>/g
const latinLetters = /[A-Za-zÀ-ɏ]/
const forbiddenMacedonianWords = ['курс', 'врск']
const keyPrefix = 'groupSettings.report'
const bodyKey = 'groupSettings.reportBody'

const reportGroupTexts: [string, string, string][] = [
  ['groupSettings.report', 'Report this group', 'Пријави ја групата'],
  [
    'groupSettings.reportHint',
    "Opens your email app with the group's number filled in.",
    'Ја отвора твојата е-пошта со внесен број на групата.',
  ],
  ['groupSettings.reportSubject', 'Report a Kvit group', 'Пријава на група во Kvit'],
  [
    'groupSettings.reportBody',
    'Group number: {{id}}\n\nWhat is wrong:\n',
    'Број на групата: {{id}}\n\nШто не е во ред:\n',
  ],
]

const textsWithoutPlaceholders = reportGroupTexts.filter(([key]) => key !== bodyKey)

const languageTexts = [
  ['en', en],
  ['mk', mk],
] as const

function keysStartingWith(texts: object, prefix: string): string[] {
  return collectTexts(texts)
    .map(([path]) => path)
    .filter((path) => path.startsWith(prefix))
    .sort()
}

describe('the approved Phase 19 report-a-group texts', () => {
  it.each(reportGroupTexts)('has the approved English and Macedonian wording for %s', (key, english, macedonian) => {
    expect(translated('en', key)).toBe(english)
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(textsWithoutPlaceholders)('has no tags or placeholders in %s', (key) => {
    for (const language of ['en', 'mk'] as const) {
      expect(translated(language, key)).not.toMatch(placeholders)
      expect(translated(language, key)).not.toMatch(tags)
    }
  })

  it.each(languageTexts)('has the placeholder {{id}} exactly once and no other placeholder or tag in the body in %s', (language) => {
    const text = translated(language, bodyKey)

    expect(text.match(placeholders)).toEqual(['{{id}}'])
    expect(text).not.toMatch(tags)
  })

  it.each(languageTexts)('writes the body in %s with real line breaks, a blank line after the group number and a line break at the end', (language) => {
    const text = translated(language, bodyKey)

    expect(text.split('\n')).toHaveLength(4)
    expect(text).toContain('{{id}}\n\n')
    expect(text.endsWith(':\n')).toBe(true)
    expect(text).not.toContain('\\n')
    expect(text).not.toContain('\r')
  })

  it.each(languageTexts)('puts a group id in the body unchanged in %s, because the id is a GUID and must reach the email as it is', (language) => {
    const text = translated(language, bodyKey, { id: testGroupId })

    expect(text).toContain(testGroupId)
    expect(text).not.toMatch(placeholders)
  })

  it.each(reportGroupTexts)('writes the Macedonian text of %s in Cyrillic apart from the name Kvit and the placeholders', (key) => {
    const withoutAllowedLatin = translated('mk', key).replaceAll('Kvit', '').replace(placeholders, '')

    expect(withoutAllowedLatin).not.toMatch(latinLetters)
  })

  it.each(reportGroupTexts)('uses none of the forbidden Macedonian words in %s', (key) => {
    for (const word of forbiddenMacedonianWords) {
      expect(translated('mk', key).toLowerCase()).not.toContain(word)
    }
  })

  it.each(reportGroupTexts)('never writes the singular «Активност» in the Macedonian text of %s', (key) => {
    expect(translated('mk', key)).not.toMatch(/Активност(?!и)/)
  })

  it.each(languageTexts)('has exactly the four pinned texts under groupSettings.report in %s, so every new line is locked', (_language, texts) => {
    const pinnedKeys = reportGroupTexts.map(([key]) => key).sort()

    expect(keysStartingWith(texts, keyPrefix)).toEqual(pinnedKeys)
  })

  it('has the same keys in English and Macedonian', () => {
    expect(keysStartingWith(mk, keyPrefix)).toEqual(keysStartingWith(en, keyPrefix))
  })
})
