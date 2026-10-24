import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import en from './locales/en.json'
import mk from './locales/mk.json'

interface BudgetScreenText {
  key: string
  en: string
  mk: string
}

const latinLetters = /[A-Za-zÀ-ɏ]/
const prefix = 'budget.screen.'

const budgetScreenTexts: BudgetScreenText[] = [
  { key: 'budget.screen.spentThisMonth', en: 'Spent this month', mk: 'Потрошено овој месец' },
  { key: 'budget.screen.emptyTitle', en: 'Nothing spent this month', mk: 'Овој месец нема трошоци' },
  { key: 'budget.screen.add', en: 'Add', mk: 'Додај' },
  { key: 'budget.screen.months', en: 'Month', mk: 'Месец' },
  { key: 'budget.screen.categoryTotals', en: 'Totals by category', mk: 'Вкупно по категории' },
  { key: 'budget.screen.entries', en: 'Entries', mk: 'Внесови' },
]

const leftCardKeys = ['budget.screen.leftThisMonth', 'budget.screen.income', 'budget.screen.spent']

const navTexts: BudgetScreenText[] = [{ key: 'nav.budget', en: 'Budget', mk: 'Буџет' }]

const reusedTexts: BudgetScreenText[] = [
  { key: 'expenses.emptyHint', en: 'Tap + to add the first one.', mk: 'Допри + за да додадеш.' },
  { key: 'expenses.today', en: 'Today', mk: 'Денес' },
  { key: 'expenses.yesterday', en: 'Yesterday', mk: 'Вчера' },
  { key: 'common.retry', en: 'Try again', mk: 'Обиди се повторно' },
]

const newTexts = [...budgetScreenTexts, ...navTexts]

function keysUnder(texts: object, keyPrefix: string): string[] {
  return collectTexts(texts)
    .map(([path]) => path)
    .filter((key) => key.startsWith(keyPrefix))
    .sort()
}

describe('the texts of the Budget tab', () => {
  it.each([...newTexts, ...reusedTexts])('has the English wording for $key', ({ key, en: english }) => {
    expect(translated('en', key)).toBe(english)
  })

  it.each([...newTexts, ...reusedTexts])('has the draft Macedonian wording for $key', ({ key, mk: macedonian }) => {
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(newTexts)('writes the Macedonian text of $key with no Latin letters', ({ key }) => {
    expect(translated('mk', key)).not.toMatch(latinLetters)
  })

  it('has exactly the texts pinned above and the three of the Left card under budget.screen, in English and in Macedonian', () => {
    const pinnedKeys = [...budgetScreenTexts.map(({ key }) => key), ...leftCardKeys].sort()

    expect(keysUnder(en, prefix)).toEqual(pinnedKeys)
    expect(keysUnder(mk, prefix)).toEqual(pinnedKeys)
  })

  it('has the four tab names under nav, in English and in Macedonian', () => {
    const navKeys = ['nav.budget', 'nav.groups', 'nav.home', 'nav.settings']

    expect(keysUnder(en, 'nav.')).toEqual(navKeys)
    expect(keysUnder(mk, 'nav.')).toEqual(navKeys)
  })
})
