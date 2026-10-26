import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import en from './locales/en.json'
import mk from './locales/mk.json'

interface BudgetSettingsText {
  key: string
  en: string
  mk: string
}

const latinLetters = /[A-Za-zÀ-ɏ]/
const placeholders = /\{\{\w+\}\}/g
const settingsPrefix = 'settings.budget.'
const groupSettingsPrefix = 'groupSettings.'

const settingsTexts: BudgetSettingsText[] = [
  { key: 'settings.budget.title', en: 'Budget', mk: 'Буџет' },
  { key: 'settings.budget.showOneTotal', en: 'Show one total', mk: 'Прикажи еден збир' },
  { key: 'settings.budget.apart', en: 'No, keep MKD and EUR apart', mk: 'Не, одвоено денари и евра' },
  { key: 'settings.budget.inMkd', en: 'In MKD', mk: 'Во денари' },
  { key: 'settings.budget.inEur', en: 'In EUR', mk: 'Во евра' },
  {
    key: 'settings.budget.hint',
    en: 'Euro amounts are converted with the rate saved on each entry.',
    mk: 'Износите во евра се претвораат со цената на еврото зачувана на секој внес.',
  },
]

const groupSettingsTexts: BudgetSettingsText[] = [
  { key: 'groupSettings.countInBudget', en: 'Count my shares in my budget', mk: 'Сметај ги моите делови во мојот буџет' },
  {
    key: 'groupSettings.countInBudgetHint',
    en: 'From now on. Past months stay as they were.',
    mk: 'Од сега натаму. Минатите месеци остануваат како што биле.',
  },
]

const newTexts = [...settingsTexts, ...groupSettingsTexts]

function keysUnder(texts: object, prefix: string): string[] {
  return collectTexts(texts)
    .map(([path]) => path)
    .filter((key) => key.startsWith(prefix))
    .sort()
}

function placeholdersOf(text: string): string[] {
  return (text.match(placeholders) ?? []).sort()
}

describe('the texts of the budget settings', () => {
  it.each(newTexts)('has the English wording for $key', ({ key, en: english }) => {
    expect(translated('en', key)).toBe(english)
  })

  it.each(newTexts)('has the draft Macedonian wording for $key', ({ key, mk: macedonian }) => {
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(newTexts)('writes the Macedonian text of $key with no Latin letters', ({ key }) => {
    expect(translated('mk', key)).not.toMatch(latinLetters)
  })

  it.each(newTexts)('uses the same placeholders in the English and the Macedonian text of $key', ({ key }) => {
    expect(placeholdersOf(translated('mk', key))).toEqual(placeholdersOf(translated('en', key)))
  })

  it('has exactly the settings texts pinned above under settings.budget, in English and in Macedonian', () => {
    const pinnedKeys = settingsTexts.map(({ key }) => key).sort()

    expect(keysUnder(en, settingsPrefix)).toEqual(pinnedKeys)
    expect(keysUnder(mk, settingsPrefix)).toEqual(pinnedKeys)
  })

  it('has the same keys under groupSettings in English and in Macedonian', () => {
    expect(keysUnder(mk, groupSettingsPrefix)).toEqual(keysUnder(en, groupSettingsPrefix))
  })

  it('has the two switch texts under groupSettings in both languages', () => {
    const pinnedKeys = groupSettingsTexts.map(({ key }) => key)

    expect(keysUnder(en, groupSettingsPrefix)).toEqual(expect.arrayContaining(pinnedKeys))
    expect(keysUnder(mk, groupSettingsPrefix)).toEqual(expect.arrayContaining(pinnedKeys))
  })
})
