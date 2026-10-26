import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import en from './locales/en.json'
import mk from './locales/mk.json'

interface GroupSharesText {
  key: string
  en: string
  mk: string
}

const latinLetters = /[A-Za-zÀ-ɏ]/
const placeholders = /\{\{\w+\}\}/g
const groupSharesPrefix = 'budget.groupShares.'
const cardPrefix = 'budget.card.'

const groupSharesTexts: GroupSharesText[] = [{ key: 'budget.groupShares.title', en: 'From groups', mk: 'Од групи' }]

const cardTexts: GroupSharesText[] = [
  { key: 'budget.card.inMkd', en: 'in MKD', mk: 'во денари' },
  { key: 'budget.card.inEur', en: 'in EUR', mk: 'во евра' },
  {
    key: 'budget.card.includesConvertedEuro',
    en: 'includes converted euro amounts',
    mk: 'вклучува претворени износи во евра',
  },
  {
    key: 'budget.card.includesConvertedDenars',
    en: 'includes converted denar amounts',
    mk: 'вклучува претворени износи во денари',
  },
]

const newTexts = [...groupSharesTexts, ...cardTexts]

function keysUnder(texts: object, prefix: string): string[] {
  return collectTexts(texts)
    .map(([path]) => path)
    .filter((key) => key.startsWith(prefix))
    .sort()
}

function placeholdersOf(text: string): string[] {
  return (text.match(placeholders) ?? []).sort()
}

describe('the texts of the group shares and the converted total on the Budget tab', () => {
  it.each(newTexts)('has the English wording for $key', ({ key, en: english }) => {
    expect(translated('en', key)).toBe(english)
  })

  it.each(newTexts)('has the draft Macedonian wording for $key', ({ key, mk: macedonian }) => {
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(newTexts)('writes the Macedonian text of $key with no Latin letters', ({ key }) => {
    expect(translated('mk', key)).not.toMatch(latinLetters)
  })

  it.each(newTexts)('does not use the word «курс» in the Macedonian text of $key', ({ key }) => {
    expect(translated('mk', key).toLowerCase()).not.toContain('курс')
  })

  it.each(newTexts)('uses the same placeholders in the English and the Macedonian text of $key', ({ key }) => {
    expect(placeholdersOf(translated('mk', key))).toEqual(placeholdersOf(translated('en', key)))
  })

  it.each([groupSharesPrefix, cardPrefix])('has the same keys under %s in English and in Macedonian', (prefix) => {
    expect(keysUnder(mk, prefix)).toEqual(keysUnder(en, prefix))
  })

  it('has every pinned text under its own prefix in both languages', () => {
    const pinnedKeys = newTexts.map(({ key }) => key)

    expect([...keysUnder(en, groupSharesPrefix), ...keysUnder(en, cardPrefix)]).toEqual(expect.arrayContaining(pinnedKeys))
    expect([...keysUnder(mk, groupSharesPrefix), ...keysUnder(mk, cardPrefix)]).toEqual(expect.arrayContaining(pinnedKeys))
  })

  it('keeps the new texts out of budget.screen, whose keys are pinned exactly by another test', () => {
    expect(keysUnder(en, 'budget.screen.').filter((key) => /groupShares|converted|inMkd|inEur/i.test(key))).toEqual([])
  })
})
