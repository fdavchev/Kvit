import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import en from './locales/en.json'
import mk from './locales/mk.json'

interface SummaryText {
  key: string
  en: string
  mk: string
}

const latinLetters = /[A-Za-zÀ-ɏ]/
const placeholders = /\{\{\w+\}\}/g
const summaryPrefix = 'budget.summary.'

const summaryTexts: SummaryText[] = [
  { key: 'budget.summary.title', en: '{{month}} vs {{previousMonth}}', mk: '{{month}} наспроти {{previousMonth}}' },
  { key: 'budget.summary.row', en: 'Summary', mk: 'Резиме' },
  { key: 'budget.summary.was', en: 'was', mk: 'беше' },
  { key: 'budget.summary.up', en: 'up {{percent}} percent', mk: 'повеќе за {{percent}} отсто' },
  { key: 'budget.summary.down', en: 'down {{percent}} percent', mk: 'помалку за {{percent}} отсто' },
  { key: 'budget.summary.same', en: 'no change', mk: 'без промена' },
  { key: 'budget.summary.new', en: 'new', mk: 'ново' },
  { key: 'budget.summary.percent', en: '{{percent}} %', mk: '{{percent}} %' },
  { key: 'budget.summary.empty', en: 'Nothing to compare yet', mk: 'Засега нема што да се спореди' },
  { key: 'budget.summary.toBudget', en: 'Go to Budget', mk: 'Оди на Буџет' },
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

describe('the texts of the monthly summary', () => {
  it.each(summaryTexts)('has the English wording for $key', ({ key, en: english }) => {
    expect(translated('en', key)).toBe(english)
  })

  it.each(summaryTexts)('has the draft Macedonian wording for $key', ({ key, mk: macedonian }) => {
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(summaryTexts)('writes the Macedonian text of $key with no Latin letters outside its placeholders', ({ key }) => {
    expect(withoutPlaceholders(translated('mk', key))).not.toMatch(latinLetters)
  })

  it.each(summaryTexts)('does not use the word «курс» in the Macedonian text of $key', ({ key }) => {
    expect(translated('mk', key).toLowerCase()).not.toContain('курс')
  })

  it.each(summaryTexts)('uses the same placeholders in the English and the Macedonian text of $key', ({ key }) => {
    expect(placeholdersOf(translated('mk', key))).toEqual(placeholdersOf(translated('en', key)))
  })

  it('has the same keys under budget.summary in English and in Macedonian', () => {
    expect(keysUnder(mk, summaryPrefix)).toEqual(keysUnder(en, summaryPrefix))
  })

  it('has exactly the pinned keys under budget.summary in both languages, so no unpinned text sneaks in', () => {
    const pinnedKeys = summaryTexts.map(({ key }) => key).sort()

    expect(keysUnder(en, summaryPrefix)).toEqual(pinnedKeys)
    expect(keysUnder(mk, summaryPrefix)).toEqual(pinnedKeys)
  })

  it('writes "percent" in the sentences for a screen reader and not only the symbol', () => {
    expect(translated('en', 'budget.summary.up')).toContain('percent')
    expect(translated('en', 'budget.summary.down')).toContain('percent')
    expect(translated('mk', 'budget.summary.up')).toContain('отсто')
    expect(translated('mk', 'budget.summary.down')).toContain('отсто')
  })

  it('writes the percent in the Macedonian sentences with a form that does not change with the number', () => {
    for (const key of ['budget.summary.up', 'budget.summary.down']) {
      expect(translated('mk', key)).not.toMatch(/процент/)
    }
  })

  it('keeps the summary texts out of budget.screen, whose keys are pinned exactly by another test', () => {
    expect(keysUnder(en, 'budget.screen.').filter((key) => /summary/i.test(key))).toEqual([])
  })

  it('reuses the existing texts for Spent, Income, the category list, "in MKD", "in EUR", Back and Try again instead of adding new ones', () => {
    for (const key of [
      'budget.screen.spent',
      'budget.screen.income',
      'budget.screen.categoryTotals',
      'budget.card.inMkd',
      'budget.card.inEur',
      'common.back',
      'common.retry',
    ]) {
      expect(translated('en', key)).not.toBe('')
      expect(translated('mk', key)).not.toBe('')
    }
    expect(keysUnder(en, summaryPrefix).filter((key) => /spent|income|categor|inMkd|inEur|back|retry/i.test(key))).toEqual([])
  })
})
