import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import en from './locales/en.json'
import mk from './locales/mk.json'

const placeholders = /\{\{\w+\}\}/g
const tags = /<\/?\w+>/g
const latinLetters = /[A-Za-zÀ-ɏ]/
const forbiddenMacedonianWords = ['курс', 'врск']
const keyPrefix = 'settings.downloadData'

const dataExportTexts: [string, string, string][] = [
  ['settings.downloadData', 'Download my data', 'Преземи ги моите податоци'],
  [
    'settings.downloadDataHint',
    "Your profile, personal budget and groups with their activity, as a file. Other people's emails are not in it.",
    'Твојот профил, личен буџет и групи со нивните активности, како датотека. Туѓи е-пошти нема во неа.',
  ],
  ['settings.downloadDataDone', 'Your data was saved as a file', 'Твоите податоци се зачувани како датотека'],
]

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

describe('the approved Phase 19 data export texts', () => {
  it.each(dataExportTexts)('has the approved English and Macedonian wording for %s', (key, english, macedonian) => {
    expect(translated('en', key)).toBe(english)
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(dataExportTexts)('has no tags or placeholders in %s', (key) => {
    for (const language of ['en', 'mk'] as const) {
      expect(translated(language, key)).not.toMatch(placeholders)
      expect(translated(language, key)).not.toMatch(tags)
    }
  })

  it.each(dataExportTexts)('writes the Macedonian text of %s in Cyrillic', (key) => {
    expect(translated('mk', key)).not.toMatch(latinLetters)
  })

  it.each(dataExportTexts)('uses none of the forbidden Macedonian words in %s', (key) => {
    for (const word of forbiddenMacedonianWords) {
      expect(translated('mk', key).toLowerCase()).not.toContain(word)
    }
  })

  it.each(dataExportTexts)('never writes the singular «Активност» in the Macedonian text of %s', (key) => {
    expect(translated('mk', key)).not.toMatch(/Активност(?!и)/)
  })

  it.each(languageTexts)('has exactly the three pinned texts under settings.downloadData in %s, so every new line is locked', (_language, texts) => {
    const pinnedKeys = dataExportTexts.map(([key]) => key).sort()

    expect(keysStartingWith(texts, keyPrefix)).toEqual(pinnedKeys)
  })

  it('has the same keys in English and Macedonian', () => {
    expect(keysStartingWith(mk, keyPrefix)).toEqual(keysStartingWith(en, keyPrefix))
  })

  it.each(languageTexts)('names the button in the privacy notice exactly as the Settings row does in %s', (language) => {
    const privacyBody = translated(language, 'privacy.sections.13.body')

    expect(privacyBody).toContain(translated(language, 'settings.downloadData'))
  })
})
