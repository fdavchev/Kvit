import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import en from './locales/en.json'
import mk from './locales/mk.json'

interface BudgetCategoryText {
  key: string
  en: string
  mk: string
}

const placeholders = /\{\{\w+\}\}/g
const latinLetters = /[A-Za-zÀ-ɏ]/
const prefix = 'budget.categories.'

const budgetCategoryTexts: BudgetCategoryText[] = [
  { key: 'budget.categories.new', en: 'New', mk: 'Нова' },
  { key: 'budget.categories.editMine', en: 'Edit my categories', mk: 'Уреди ги моите категории' },
  { key: 'budget.categories.mineTitle', en: 'My categories', mk: 'Мои категории' },
  { key: 'budget.categories.newTitle', en: 'New category', mk: 'Нова категорија' },
  { key: 'budget.categories.editTitle', en: 'Edit category', mk: 'Измени категорија' },
  { key: 'budget.categories.name', en: 'Name', mk: 'Име' },
  { key: 'budget.categories.namePlaceholder', en: 'Coffee', mk: 'Кафе' },
  { key: 'budget.categories.emoji', en: 'Emoji', mk: 'Емоџи' },
  { key: 'budget.categories.emojiPicks', en: 'Quick emoji picks', mk: 'Брз избор на емоџи' },
  { key: 'budget.categories.color', en: 'Colour', mk: 'Боја' },
  { key: 'budget.categories.colors.orange', en: 'Orange', mk: 'Портокалова' },
  { key: 'budget.categories.colors.green', en: 'Green', mk: 'Зелена' },
  { key: 'budget.categories.colors.yellow', en: 'Yellow', mk: 'Жолта' },
  { key: 'budget.categories.colors.blue', en: 'Blue', mk: 'Сина' },
  { key: 'budget.categories.colors.pink', en: 'Pink', mk: 'Розова' },
  { key: 'budget.categories.colors.purple', en: 'Purple', mk: 'Виолетова' },
  { key: 'budget.categories.colors.slate', en: 'Slate', mk: 'Сиво-сина' },
  { key: 'budget.categories.colors.red', en: 'Red', mk: 'Црвена' },
  { key: 'budget.categories.colors.teal', en: 'Teal', mk: 'Тиркизна' },
  { key: 'budget.categories.colors.gray', en: 'Grey', mk: 'Сива' },
  { key: 'budget.categories.archive', en: 'Archive', mk: 'Архивирај' },
  { key: 'budget.categories.archivedToast', en: 'Archived: {{name}}', mk: 'Архивирано: {{name}}' },
  { key: 'budget.categories.archivedHeading', en: 'Archived ({{count}})', mk: 'Архивирани ({{count}})' },
  { key: 'budget.categories.bringBack', en: 'Bring back', mk: 'Врати' },
  {
    key: 'budget.categories.emptyMine',
    en: 'You have no categories of your own yet. Tap “+ New” to make one.',
    mk: 'Сѐ уште немаш свои категории. Допри „+ Нова“ за да направиш една.',
  },
]

function keysUnder(texts: object): string[] {
  return collectTexts(texts)
    .map(([path]) => path)
    .filter((key) => key.startsWith(prefix))
    .sort()
}

function placeholdersOf(text: string): string[] {
  return (text.match(placeholders) ?? []).sort()
}

describe('the texts of the personal categories', () => {
  it.each(budgetCategoryTexts)('has the English wording for $key', ({ key, en: english }) => {
    expect(translated('en', key)).toBe(english)
  })

  it.each(budgetCategoryTexts)('has the draft Macedonian wording for $key', ({ key, mk: macedonian }) => {
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(budgetCategoryTexts)('uses the same placeholders in the English and the Macedonian text of $key', ({ key }) => {
    expect(placeholdersOf(translated('mk', key))).toEqual(placeholdersOf(translated('en', key)))
  })

  it.each(budgetCategoryTexts)('writes the Macedonian text of $key with no Latin letters apart from the placeholders', ({ key }) => {
    expect(translated('mk', key).replace(placeholders, '')).not.toMatch(latinLetters)
  })

  it('has exactly the texts pinned above under budget.categories, in English and in Macedonian', () => {
    const pinnedKeys = budgetCategoryTexts.map(({ key }) => key).sort()

    expect(keysUnder(en)).toEqual(pinnedKeys)
    expect(keysUnder(mk)).toEqual(pinnedKeys)
  })

  it('writes «сѐ» in the Macedonian empty text with the Cyrillic ѐ (U+0450)', () => {
    expect(translated('mk', 'budget.categories.emptyMine').toLowerCase()).toContain('сѐ уште')
  })
})
