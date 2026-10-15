import { describe, expect, it } from 'vitest'
import {
  closingActivityTexts,
  closingTexts,
  finishedBarText,
  type ClosingText,
} from '@/test/closingTestData'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import en from './locales/en.json'
import mk from './locales/mk.json'

const placeholders = /\{\{\w+\}\}/g
const latinLetters = /[A-Za-zÀ-ɏ]/

const pinnedTexts: ClosingText[] = [
  ...Object.values(closingTexts),
  ...Object.values(closingActivityTexts),
  finishedBarText,
]

const closingActivityKeyPattern = /^activity\.(closing|groupFinished|groupReopened)/

function keysOf(texts: object, belongsTo: (key: string) => boolean): string[] {
  return collectTexts(texts)
    .map(([path]) => path)
    .filter(belongsTo)
    .sort()
}

const closingKeysInEnglish = keysOf(en, (key) => key.startsWith('closing.'))
const closingKeysInMacedonian = keysOf(mk, (key) => key.startsWith('closing.'))
const closingActivityKeysInEnglish = keysOf(en, (key) => closingActivityKeyPattern.test(key))
const closingActivityKeysInMacedonian = keysOf(mk, (key) => closingActivityKeyPattern.test(key))
const allNewKeys = [...closingKeysInEnglish, ...closingActivityKeysInEnglish]

function placeholdersOf(text: string): string[] {
  return (text.match(placeholders) ?? []).sort()
}

function withoutPlaceholders(text: string): string {
  return text.replace(placeholders, '')
}

describe('the texts for closing a group', () => {
  it.each(pinnedTexts)('has the approved English wording for $key', ({ key, en: english }) => {
    expect(translated('en', key)).toBe(english)
  })

  it.each(pinnedTexts)('has the draft Macedonian wording for $key', ({ key, mk: macedonian }) => {
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(pinnedTexts)('writes the Macedonian text of $key with Cyrillic letters only apart from the placeholders, no Latin look-alikes', ({ key }) => {
    expect(withoutPlaceholders(translated('mk', key))).not.toMatch(latinLetters)
  })

  it.each(pinnedTexts)('uses the same placeholders in the English and the Macedonian text of $key', ({ key }) => {
    expect(placeholdersOf(translated('mk', key))).toEqual(placeholdersOf(translated('en', key)))
  })

  it.each(pinnedTexts)('has a Macedonian text for $key that is not the English one', ({ key }) => {
    expect(translated('mk', key)).not.toBe(translated('en', key))
  })

  it('has at least the texts that are pinned above under closing', () => {
    const pinnedClosingKeys = Object.values(closingTexts).map(({ key }) => key)

    expect(closingKeysInEnglish).toEqual(expect.arrayContaining(pinnedClosingKeys))
  })

  it('has exactly the same keys under closing in English and in Macedonian', () => {
    expect(closingKeysInMacedonian).toEqual(closingKeysInEnglish)
  })

  it('has exactly the same closing sentences in the activity in English and in Macedonian', () => {
    expect(closingActivityKeysInMacedonian).toEqual(closingActivityKeysInEnglish)
  })

  it('has exactly the seven closing sentences in the activity: the six events and the objection with a reason', () => {
    expect(closingActivityKeysInEnglish).toEqual(
      Object.values(closingActivityTexts)
        .map(({ key }) => key)
        .sort(),
    )
  })

  it.each(allNewKeys)('has a Macedonian text for %s', (key) => {
    expect(translated('mk', key)).not.toBe('')
  })

  it.each(allNewKeys)('uses the same placeholders in the English and the Macedonian text of %s', (key) => {
    expect(placeholdersOf(translated('mk', key))).toEqual(placeholdersOf(translated('en', key)))
  })

  it.each(allNewKeys)('writes the Macedonian text of %s with no Latin letters apart from the placeholders', (key) => {
    expect(withoutPlaceholders(translated('mk', key))).not.toMatch(latinLetters)
  })

  it.each(allNewKeys)('has no Macedonian text under %s that is still the English one', (key) => {
    expect(translated('mk', key)).not.toBe(translated('en', key))
  })

  it('uses the word «квит» for kvit in the Macedonian title of the question to close the group', () => {
    expect(translated('mk', closingTexts.readyTitle.key)).toContain('квит')
  })

  it('writes the objection button and the objection sentences with the same word, «приговор»', () => {
    const objectionTexts = [
      translated('mk', closingTexts.object.key),
      translated('mk', closingTexts.objectTitle.key),
      translated('mk', closingActivityTexts.closingObjected.key),
      translated('mk', closingActivityTexts.closingObjectedWithReason.key),
    ]

    for (const text of objectionTexts) {
      expect(text.toLowerCase()).toContain('приговор')
    }
  })

  it('writes the reason in Macedonian quotes and in English quotes', () => {
    const params = { name: 'Ana', reason: 'Not yet' }

    expect(translated('en', closingActivityTexts.closingObjectedWithReason.key, params)).toBe(
      'Ana objected to the closing: “Not yet”',
    )
    expect(translated('mk', closingActivityTexts.closingObjectedWithReason.key, { name: 'Ана', reason: 'Уште не' })).toBe(
      'Ана приговори на затворањето: „Уште не“',
    )
  })

  it('does not put the name of a person in the sentence of the finished group, because the 24 hours finish it without anyone acting', () => {
    expect(translated('en', closingActivityTexts.groupFinished.key)).not.toContain('{{name}}')
    expect(translated('mk', closingActivityTexts.groupFinished.key)).not.toContain('{{name}}')
  })
})
