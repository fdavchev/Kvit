import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import en from './locales/en.json'
import mk from './locales/mk.json'

interface BudgetIncomeText {
  key: string
  en: string
}

const latinLetters = /[A-Za-zÀ-ɏ]/

const addTexts: BudgetIncomeText[] = [
  { key: 'budget.add.title', en: 'What do you add?' },
  { key: 'budget.add.spending', en: 'Spending' },
  { key: 'budget.add.income', en: 'Income' },
]

const incomeTexts: BudgetIncomeText[] = [
  { key: 'budget.income.title', en: 'New income' },
  { key: 'budget.income.listTitle', en: 'Income' },
  { key: 'budget.income.editTitle', en: 'Income' },
]

const newTexts = [...addTexts, ...incomeTexts]

function keysUnder(texts: object, prefix: string): string[] {
  return collectTexts(texts)
    .map(([path]) => path)
    .filter((key) => key.startsWith(prefix))
    .sort()
}

describe('the texts of adding income', () => {
  it.each(newTexts)('has the English wording for $key', ({ key, en: english }) => {
    expect(translated('en', key)).toBe(english)
  })

  it.each(newTexts)('has a Macedonian text for $key that is not the English one', ({ key, en: english }) => {
    expect(translated('mk', key)).not.toBe('')
    expect(translated('mk', key)).not.toBe(english)
  })

  it.each(newTexts)('writes the Macedonian text of $key with no Latin letters', ({ key }) => {
    expect(translated('mk', key)).not.toMatch(latinLetters)
  })

  it('has exactly the three choice texts under budget.add, in English and in Macedonian', () => {
    const pinnedKeys = addTexts.map(({ key }) => key).sort()

    expect(keysUnder(en, 'budget.add.')).toEqual(pinnedKeys)
    expect(keysUnder(mk, 'budget.add.')).toEqual(pinnedKeys)
  })

  it('has at least the pinned texts under budget.income, in English and in Macedonian', () => {
    const pinnedKeys = incomeTexts.map(({ key }) => key)

    expect(keysUnder(en, 'budget.income.')).toEqual(expect.arrayContaining(pinnedKeys))
    expect(keysUnder(mk, 'budget.income.')).toEqual(expect.arrayContaining(pinnedKeys))
  })

  it('has the same keys under budget.add and budget.income in both languages', () => {
    expect(keysUnder(mk, 'budget.add.')).toEqual(keysUnder(en, 'budget.add.'))
    expect(keysUnder(mk, 'budget.income.')).toEqual(keysUnder(en, 'budget.income.'))
  })
})
