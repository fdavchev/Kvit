import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import en from './locales/en.json'
import mk from './locales/mk.json'

interface LeftCardText {
  key: string
  en: string
}

const latinLetters = /[A-Za-zÀ-ɏ]/

const leftCardTexts: LeftCardText[] = [
  { key: 'budget.screen.leftThisMonth', en: 'Left this month' },
  { key: 'budget.screen.income', en: 'Income' },
  { key: 'budget.screen.spent', en: 'Spent' },
]

function keysUnder(texts: object, prefix: string): string[] {
  return collectTexts(texts)
    .map(([path]) => path)
    .filter((key) => key.startsWith(prefix))
    .sort()
}

describe('the texts of the Left this month card', () => {
  it.each(leftCardTexts)('has the English wording for $key', ({ key, en: english }) => {
    expect(translated('en', key)).toBe(english)
  })

  it.each(leftCardTexts)('has a Macedonian text for $key that is not the English one', ({ key, en: english }) => {
    expect(translated('mk', key)).not.toBe('')
    expect(translated('mk', key)).not.toBe(english)
  })

  it.each(leftCardTexts)('writes the Macedonian text of $key with no Latin letters', ({ key }) => {
    expect(translated('mk', key)).not.toMatch(latinLetters)
  })

  it('has the same keys under budget.screen in both languages', () => {
    expect(keysUnder(mk, 'budget.screen.')).toEqual(keysUnder(en, 'budget.screen.'))
  })
})
