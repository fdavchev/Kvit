import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import en from './locales/en.json'
import mk from './locales/mk.json'

interface TotalsText {
  key: string
  en: string
  mk: string
}

const latinLetters = /[A-Za-zÀ-ɏ]/
const cyrillicLetters = /[Ѐ-ӿ]/
const placeholders = /\{\{\w+\}\}/g
const totalsPrefix = 'groupTotals.'

const totalsTexts: TotalsText[] = [
  { key: 'groupTotals.tab', en: 'Totals', mk: 'Вкупно' },
  { key: 'groupTotals.title', en: 'Totals by category', mk: 'Збирови по категорија' },
  { key: 'groupTotals.emptyTitle', en: 'No spending yet', mk: 'Сѐ уште нема трошоци' },
  {
    key: 'groupTotals.emptyHint',
    en: 'Totals by category show up here once someone adds an expense.',
    mk: 'Збировите по категорија ќе се појават тука кога некој ќе додаде трошок.',
  },
  { key: 'groupTotals.noCategory', en: 'No category', mk: 'Без категорија' },
]

function keysUnder(texts: object, prefix: string): string[] {
  return collectTexts(texts)
    .map(([path]) => path)
    .filter((key) => key.startsWith(prefix))
    .sort()
}

function placeholdersOf(text: string): string[] {
  return (text.match(placeholders) ?? []).sort()
}

function withoutPlaceholders(text: string): string {
  return text.replace(placeholders, '')
}

describe('the texts of the Totals tab of a group', () => {
  it.each(totalsTexts)('has the English wording for $key', ({ key, en: english }) => {
    expect(translated('en', key)).toBe(english)
  })

  it.each(totalsTexts)('has the draft Macedonian wording for $key', ({ key, mk: macedonian }) => {
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(totalsTexts)('writes the Macedonian text of $key in Cyrillic with no Latin letters outside its placeholders', ({ key }) => {
    const macedonian = translated('mk', key)

    expect(macedonian).toMatch(cyrillicLetters)
    expect(withoutPlaceholders(macedonian)).not.toMatch(latinLetters)
  })

  it.each(totalsTexts)('does not use the word «курс» in the Macedonian text of $key', ({ key }) => {
    expect(translated('mk', key).toLowerCase()).not.toContain('курс')
  })

  it.each(totalsTexts)('uses the same placeholders in the English and the Macedonian text of $key', ({ key }) => {
    expect(placeholdersOf(translated('mk', key))).toEqual(placeholdersOf(translated('en', key)))
  })

  it('has the same keys under groupTotals in English and in Macedonian', () => {
    expect(keysUnder(mk, totalsPrefix)).toEqual(keysUnder(en, totalsPrefix))
  })

  it('has exactly the pinned keys under groupTotals in both languages, so no unpinned text sneaks in', () => {
    const pinnedKeys = totalsTexts.map(({ key }) => key).sort()

    expect(keysUnder(en, totalsPrefix)).toEqual(pinnedKeys)
    expect(keysUnder(mk, totalsPrefix)).toEqual(pinnedKeys)
  })

  it('writes the empty message with no full stop and the hint with one, in both languages', () => {
    for (const language of ['en', 'mk'] as const) {
      expect(translated(language, 'groupTotals.emptyTitle').endsWith('.')).toBe(false)
      expect(translated(language, 'groupTotals.emptyHint').endsWith('.')).toBe(true)
    }
  })

  it('keeps the name of the tab a single short word in both languages, so four tabs fit on a 360 px phone', () => {
    expect(translated('en', 'groupTotals.tab')).not.toContain(' ')
    expect(translated('mk', 'groupTotals.tab')).not.toContain(' ')
  })

  it('keeps the name of the new tab different from the names of the other three tabs', () => {
    for (const language of ['en', 'mk'] as const) {
      const others = ['expenses.title', 'balances.title', 'activity.title'].map((key) => translated(language, key))

      expect(others).not.toContain(translated(language, 'groupTotals.tab'))
    }
  })
})
