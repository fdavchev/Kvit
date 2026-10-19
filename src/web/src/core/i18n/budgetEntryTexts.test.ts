import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import en from './locales/en.json'
import mk from './locales/mk.json'

interface BudgetEntryText {
  key: string
  en: string
  mk: string
}

const latinLetters = /[A-Za-zÀ-ɏ]/
const prefix = 'budget.entries.'

const budgetEntryTexts: BudgetEntryText[] = [
  { key: 'budget.entries.homeItem', en: 'Personal spending', mk: 'Лично трошење' },
  { key: 'budget.entries.pickCategory', en: 'What did you spend on?', mk: 'На што потроши?' },
  { key: 'budget.entries.deleted', en: 'Deleted', mk: 'Избришано' },
  { key: 'budget.entries.restored', en: 'Restored', mk: 'Вратено' },
]

const reusedTexts: BudgetEntryText[] = [
  { key: 'groupSettings.save', en: 'Save', mk: 'Зачувај' },
  { key: 'groupSettings.saved', en: 'Saved', mk: 'Зачувано' },
  { key: 'expense.category', en: 'Category', mk: 'Категорија' },
  { key: 'expense.delete', en: 'Delete', mk: 'Избриши' },
  { key: 'expense.note', en: 'Note', mk: 'Белешка' },
  { key: 'common.undo', en: 'Undo', mk: 'Врати' },
]

function keysUnder(texts: object): string[] {
  return collectTexts(texts)
    .map(([path]) => path)
    .filter((key) => key.startsWith(prefix))
    .sort()
}

describe('the texts of personal spending', () => {
  it.each([...budgetEntryTexts, ...reusedTexts])('has the English wording for $key', ({ key, en: english }) => {
    expect(translated('en', key)).toBe(english)
  })

  it.each([...budgetEntryTexts, ...reusedTexts])('has the draft Macedonian wording for $key', ({ key, mk: macedonian }) => {
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(budgetEntryTexts)('writes the Macedonian text of $key with no Latin letters', ({ key }) => {
    expect(translated('mk', key)).not.toMatch(latinLetters)
  })

  it('has exactly the texts pinned above under budget.entries, in English and in Macedonian', () => {
    const pinnedKeys = budgetEntryTexts.map(({ key }) => key).sort()

    expect(keysUnder(en)).toEqual(pinnedKeys)
    expect(keysUnder(mk)).toEqual(pinnedKeys)
  })
})
