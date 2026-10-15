import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import en from './locales/en.json'
import mk from './locales/mk.json'

interface HomeText {
  key: string
  en: string
  mk: string
}

const placeholders = /\{\{\w+\}\}/g
const latinLetters = /[A-Za-zÀ-ɏ]/

const dashboardTexts: HomeText[] = [
  { key: 'home.owed', en: 'You are owed', mk: 'Ти должат' },
  { key: 'home.owe', en: 'You owe', mk: 'Должиш' },
  { key: 'home.thingsNeedYou_one', en: 'thing needs you', mk: 'нешто те чека' },
  { key: 'home.thingsNeedYou_other', en: 'things need you', mk: 'нешта те чекаат' },
  { key: 'home.nothingNeedsYou', en: 'Nothing needs you right now ✓', mk: 'Моментално ништо не те чека ✓' },
  { key: 'home.groups', en: 'Your groups', mk: 'Твоите групи' },
  { key: 'home.recentActivity', en: 'Recent activity', mk: 'Скорашна активност' },
  { key: 'home.kvit', en: 'kvit', mk: 'квит' },
  { key: 'home.noGroupsTitle', en: 'No groups yet', mk: 'Сѐ уште нема групи' },
  {
    key: 'home.noGroupsHint',
    en: 'Make a group or a one-time bill, then add your first expense.',
    mk: 'Направи група или една сметка, па додај го првиот трошок.',
  },
  { key: 'home.plus', en: 'Add', mk: 'Додај' },
  { key: 'home.plusAddTo', en: 'Add to {{group}}', mk: 'Додај во {{group}}' },
  { key: 'home.plusNewOneBill', en: 'New One bill', mk: 'Нова една сметка' },
  { key: 'home.plusAnotherGroup', en: 'Another group…', mk: 'Друга група…' },
  { key: 'home.activityExpenseEdited', en: '{{name}} edited an expense', mk: '{{name}} уреди трошок' },
]

const reusedKeys = [
  'home.greeting',
  'groups.newButton',
  'settlements.confirm',
  'settlements.reject',
  'closing.object',
  'closing.prompt',
  'closing.send',
  'balances.saysPaidYou',
]

function homeKeysOf(texts: object): string[] {
  return collectTexts(texts)
    .map(([path]) => path)
    .filter((key) => key.startsWith('home.'))
    .sort()
}

function placeholdersOf(text: string): string[] {
  return (text.match(placeholders) ?? []).sort()
}

function withoutPlaceholders(text: string): string {
  return text.replace(placeholders, '')
}

describe('the texts of the home screen', () => {
  it.each(dashboardTexts)('has the approved English wording for $key', ({ key, en: english }) => {
    expect(translated('en', key)).toBe(english)
  })

  it.each(dashboardTexts)('has the draft Macedonian wording for $key', ({ key, mk: macedonian }) => {
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(dashboardTexts)('uses the same placeholders in the English and the Macedonian text of $key', ({ key }) => {
    expect(placeholdersOf(translated('mk', key))).toEqual(placeholdersOf(translated('en', key)))
  })

  it.each(dashboardTexts)('writes the Macedonian text of $key with no Latin letters apart from the placeholders', ({ key }) => {
    expect(withoutPlaceholders(translated('mk', key))).not.toMatch(latinLetters)
  })

  it.each(dashboardTexts)('has no Macedonian text for $key that is still the English one', ({ key }) => {
    expect(translated('mk', key)).not.toBe(translated('en', key))
  })

  it('has exactly the same keys under home in English and in Macedonian', () => {
    expect(homeKeysOf(mk)).toEqual(homeKeysOf(en))
  })

  it('has at least the texts that are pinned above under home', () => {
    expect(homeKeysOf(en)).toEqual(expect.arrayContaining(dashboardTexts.map(({ key }) => key)))
  })

  it.each(reusedKeys)('still has the existing text %s in both languages, which the home screen reuses', (key) => {
    expect(translated('en', key)).not.toBe('')
    expect(translated('mk', key)).not.toBe('')
  })

  it('no longer has the placeholder line, because the dashboard replaces it', () => {
    expect(homeKeysOf(en)).not.toContain('home.placeholder')
    expect(homeKeysOf(mk)).not.toContain('home.placeholder')
  })

  it('writes the bar text with no number in it, because the number sits in its own circle', () => {
    for (const key of ['home.thingsNeedYou_one', 'home.thingsNeedYou_other']) {
      expect(translated('en', key)).not.toMatch(/\{\{|\d/)
      expect(translated('mk', key)).not.toMatch(/\{\{|\d/)
    }
  })

  it('has the singular and the plural form of the bar text in both languages', () => {
    expect(translated('en', 'home.thingsNeedYou_one')).not.toBe(translated('en', 'home.thingsNeedYou_other'))
    expect(translated('mk', 'home.thingsNeedYou_one')).not.toBe(translated('mk', 'home.thingsNeedYou_other'))
  })

  it('uses the word «квит» for kvit in the Macedonian chip', () => {
    expect(translated('mk', 'home.kvit')).toBe('квит')
  })
})
