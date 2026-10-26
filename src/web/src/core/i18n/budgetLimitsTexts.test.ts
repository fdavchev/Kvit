import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import en from './locales/en.json'
import mk from './locales/mk.json'

interface LimitsText {
  key: string
  en: string
  mk: string
}

const latinLetters = /[A-Za-zÀ-ɏ]/
const placeholders = /\{\{\w+\}\}/g
const limitsPrefix = 'budget.limits.'

const limitsTexts: LimitsText[] = [
  { key: 'budget.limits.title', en: 'Limits', mk: 'Лимити' },
  { key: 'budget.limits.wholeMonth', en: 'Whole month', mk: 'Цел месец' },
  { key: 'budget.limits.setLimit', en: 'Set a limit', mk: 'Постави лимит' },
  { key: 'budget.limits.chooseTitle', en: 'Set a limit for', mk: 'Постави лимит за' },
  { key: 'budget.limits.sheetTitle', en: '{{name}} · monthly limit', mk: '{{name}} · месечен лимит' },
  {
    key: 'budget.limits.appliesFromNow',
    en: 'Applies from this month on. Earlier months keep their limit.',
    mk: 'Важи од овој месец натаму. Претходните месеци го задржуваат својот лимит.',
  },
  {
    key: 'budget.limits.viewingPast',
    en: 'You are looking at {{month}}. The change starts this month.',
    mk: 'Гледате {{month}}. Промената почнува овој месец.',
  },
  { key: 'budget.limits.remove', en: 'Remove limit', mk: 'Отстрани го лимитот' },
  { key: 'budget.limits.removed', en: 'Limit removed', mk: 'Лимитот е отстранет' },
  { key: 'budget.limits.over', en: '{{amount}} over', mk: '{{amount}} над лимитот' },
  {
    key: 'budget.limits.includesConverted',
    en: 'includes {{amount}} converted to MKD',
    mk: 'вклучува {{amount}} претворени во денари',
  },
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

describe('the texts of the limits on the Budget tab', () => {
  it.each(limitsTexts)('has the English wording for $key', ({ key, en: english }) => {
    expect(translated('en', key)).toBe(english)
  })

  it.each(limitsTexts)('has the draft Macedonian wording for $key', ({ key, mk: macedonian }) => {
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(limitsTexts)('writes the Macedonian text of $key with no Latin letters outside its placeholders', ({ key }) => {
    expect(withoutPlaceholders(translated('mk', key))).not.toMatch(latinLetters)
  })

  it.each(limitsTexts)('does not use the word «курс» in the Macedonian text of $key', ({ key }) => {
    expect(translated('mk', key).toLowerCase()).not.toContain('курс')
  })

  it.each(limitsTexts)('uses the same placeholders in the English and the Macedonian text of $key', ({ key }) => {
    expect(placeholdersOf(translated('mk', key))).toEqual(placeholdersOf(translated('en', key)))
  })

  it('has the same keys under budget.limits in English and in Macedonian', () => {
    expect(keysUnder(mk, limitsPrefix)).toEqual(keysUnder(en, limitsPrefix))
  })

  it('has exactly the pinned keys under budget.limits in both languages, so no unpinned text sneaks in', () => {
    const pinnedKeys = limitsTexts.map(({ key }) => key).sort()

    expect(keysUnder(en, limitsPrefix)).toEqual(pinnedKeys)
    expect(keysUnder(mk, limitsPrefix)).toEqual(pinnedKeys)
  })

  it('keeps the limits texts out of budget.screen, whose keys are pinned exactly by another test', () => {
    expect(keysUnder(en, 'budget.screen.').filter((key) => /limit/i.test(key))).toEqual([])
  })

  it('reuses the existing texts for Save, the amount label and the "Saved" toast instead of adding new ones', () => {
    for (const key of ['groupSettings.save', 'groupSettings.saved', 'expense.amount', 'common.retry']) {
      expect(translated('en', key)).not.toBe('')
      expect(translated('mk', key)).not.toBe('')
    }
    expect(keysUnder(en, limitsPrefix).filter((key) => /save|saved|amount$|retry/i.test(key))).toEqual([])
  })

  it('has the texts of the server code that is refused for the amount, in both languages', () => {
    expect(translated('en', 'errors.BUDGET_LIMIT_AMOUNT_INVALID')).toBe('Enter a limit in whole denars, more than 0.')
    expect(translated('mk', 'errors.BUDGET_LIMIT_AMOUNT_INVALID')).toBe('Внеси лимит во цели денари, поголем од 0.')
  })
})
