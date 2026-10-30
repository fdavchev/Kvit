import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import en from './locales/en.json'
import mk from './locales/mk.json'

interface PlanText {
  key: string
  en: string
  mk: string
}

const latinLetters = /[A-Za-zÀ-ɏ]/
const cyrillicLetters = /[Ѐ-ӿ]/
const placeholders = /\{\{\w+\}\}/g
const currencyCodes = /MKD|EUR/g
const planPrefix = 'groupPlan.'

const planTexts: PlanText[] = [
  { key: 'groupPlan.title', en: 'Spending plan', mk: 'План за трошоците' },
  { key: 'groupPlan.heading', en: 'Spending plan', mk: 'План за трошоците' },
  { key: 'groupPlan.setRow', en: 'Set a plan', mk: 'Постави план' },
  { key: 'groupPlan.ofAmount', en: '{{spent}} of {{amount}}', mk: '{{spent}} од {{amount}}' },
  { key: 'groupPlan.each_one', en: '{{amount}} each · {{count}} person', mk: '{{amount}} по човек · {{count}} лице' },
  { key: 'groupPlan.each_other', en: '{{amount}} each · {{count}} people', mk: '{{amount}} по човек · {{count}} лица' },
  { key: 'groupPlan.onTrack', en: 'On track', mk: 'Во рамки' },
  { key: 'groupPlan.near', en: 'Near the plan · {{amount}} left', mk: 'Близу до планот · уште {{amount}}' },
  { key: 'groupPlan.over', en: '{{amount}} over', mk: '{{amount}} над планот' },
  {
    key: 'groupPlan.included',
    en: 'includes {{amount}} converted to {{currency}}',
    mk: 'вклучува {{amount}} претворени во {{currency}}',
  },
  { key: 'groupPlan.hint', en: 'Everyone in the group sees the plan.', mk: 'Сите во групата го гледаат планот.' },
  {
    key: 'groupPlan.amountInvalid',
    en: 'Type an amount above 0 (whole denars for MKD).',
    mk: 'Внеси износ поголем од 0 (цели денари за MKD).',
  },
  { key: 'groupPlan.remove', en: 'Remove plan', mk: 'Отстрани го планот' },
  { key: 'groupPlan.removed', en: 'Plan removed', mk: 'Планот е отстранет' },
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

function withoutPlaceholdersAndCurrencyCodes(text: string): string {
  return text.replace(placeholders, '').replace(currencyCodes, '')
}

describe('the texts of the group spending plan', () => {
  it.each(planTexts)('has the English wording for $key', ({ key, en: english }) => {
    expect(translated('en', key)).toBe(english)
  })

  it.each(planTexts)('has the draft Macedonian wording for $key', ({ key, mk: macedonian }) => {
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(planTexts)('writes the Macedonian text of $key in Cyrillic, with no Latin letters outside its placeholders and the currency codes', ({ key }) => {
    const macedonian = translated('mk', key)

    expect(macedonian).toMatch(cyrillicLetters)
    expect(withoutPlaceholdersAndCurrencyCodes(macedonian)).not.toMatch(latinLetters)
  })

  it.each(planTexts)('does not use the word «курс» in the Macedonian text of $key', ({ key }) => {
    expect(translated('mk', key).toLowerCase()).not.toContain('курс')
  })

  it.each(planTexts)('uses the same placeholders in the English and the Macedonian text of $key', ({ key }) => {
    expect(placeholdersOf(translated('mk', key))).toEqual(placeholdersOf(translated('en', key)))
  })

  it('has the same keys under groupPlan in English and in Macedonian', () => {
    expect(keysUnder(mk, planPrefix)).toEqual(keysUnder(en, planPrefix))
  })

  it('has exactly the pinned keys under groupPlan in both languages, so no unpinned text sneaks in', () => {
    const pinnedKeys = planTexts.map(({ key }) => key).sort()

    expect(keysUnder(en, planPrefix)).toEqual(pinnedKeys)
    expect(keysUnder(mk, planPrefix)).toEqual(pinnedKeys)
  })

  it('has no text of its own for "Saved", because the existing text groupSettings.saved is reused', () => {
    expect(keysUnder(en, planPrefix).filter((key) => /saved?$/i.test(key))).toEqual([])
    expect(translated('en', 'groupSettings.saved')).toBe('Saved')
  })

  it('uses the plural keys of the group size line: _one and _other, each with the count', () => {
    for (const language of ['en', 'mk'] as const) {
      expect(translated(language, 'groupPlan.each_one')).toContain('{{count}}')
      expect(translated(language, 'groupPlan.each_other')).toContain('{{count}}')
    }
  })

  it('writes the Macedonian plural forms with the same words as the group size line', () => {
    expect(translated('mk', 'groupPlan.each_one', { amount: '', count: '1' })).toContain(translated('mk', 'groups.people_one', { count: '1' }))
    expect(translated('mk', 'groupPlan.each_other', { amount: '', count: '2' })).toContain(translated('mk', 'groups.people_other', { count: '2' }))
  })

  it('keeps the plan error text of the server, already written in both languages', () => {
    expect(translated('en', 'errors.GROUP_PLAN_AMOUNT_INVALID')).not.toBe('')
    expect(translated('mk', 'errors.GROUP_PLAN_AMOUNT_INVALID')).not.toBe('')
  })
})
