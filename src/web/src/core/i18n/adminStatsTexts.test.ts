import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import en from './locales/en.json'
import mk from './locales/mk.json'

interface PinnedEnglishText {
  key: string
  en: string
}

const placeholders = /\{\{\w+\}\}/g
const statsPrefix = 'adminStats.'

const pinnedEnglishTexts: PinnedEnglishText[] = [
  { key: 'adminStats.title', en: 'Statistics' },
  { key: 'adminStats.range.days7', en: '7 days' },
  { key: 'adminStats.range.days30', en: '30 days' },
  { key: 'adminStats.range.thisMonth', en: 'This month' },
  { key: 'adminStats.range.months12', en: '12 months' },
  { key: 'adminStats.range.all', en: 'All' },
  { key: 'adminStats.range.custom', en: 'Custom' },
  { key: 'adminStats.custom.from', en: 'From' },
  { key: 'adminStats.custom.to', en: 'To' },
  { key: 'adminStats.by.label', en: 'Group by' },
  { key: 'adminStats.by.day', en: 'Day' },
  { key: 'adminStats.by.week', en: 'Week' },
  { key: 'adminStats.by.month', en: 'Month' },
  { key: 'adminStats.by.year', en: 'Year' },
  { key: 'adminStats.allTime.line', en: 'Since the start: {{accounts}} · {{guests}} · {{groups}} · {{expenses}} · {{payments}}' },
  { key: 'adminStats.count.account_one', en: '{{count}} account' },
  { key: 'adminStats.count.account_other', en: '{{count}} accounts' },
  { key: 'adminStats.count.guest_one', en: '{{count}} guest' },
  { key: 'adminStats.count.guest_other', en: '{{count}} guests' },
  { key: 'adminStats.count.group_one', en: '{{count}} group' },
  { key: 'adminStats.count.group_other', en: '{{count}} groups' },
  { key: 'adminStats.count.expense_one', en: '{{count}} expense' },
  { key: 'adminStats.count.expense_other', en: '{{count}} expenses' },
  { key: 'adminStats.count.payment_one', en: '{{count}} payment' },
  { key: 'adminStats.count.payment_other', en: '{{count}} payments' },
  { key: 'adminStats.card.signUps', en: 'New sign-ups' },
  { key: 'adminStats.card.activePeople', en: 'Active people' },
  { key: 'adminStats.card.joinedViaInvite', en: 'Joined via invite' },
  { key: 'adminStats.card.groupsCreated', en: 'Groups created' },
  { key: 'adminStats.card.expensesAdded', en: 'Expenses added' },
  { key: 'adminStats.card.paymentsConfirmed', en: 'Payments confirmed' },
  { key: 'adminStats.signUps.breakdown', en: 'Email {{email}} · Google {{google}} · Guest {{guest}}' },
  { key: 'adminStats.signUps.upgraded', en: 'Guests who made an account: {{count}}' },
  { key: 'adminStats.active.guests', en: 'of them guests {{count}}' },
  { key: 'adminStats.active.personDays', en: '{{count}} person-days' },
  { key: 'adminStats.compare.up', en: '+{{percent}}% vs the period before' },
  { key: 'adminStats.compare.down', en: '-{{percent}}% vs the period before' },
  { key: 'adminStats.compare.same', en: 'same as the period before' },
  { key: 'adminStats.compare.new', en: 'new, none before' },
]

const keysWithUnpinnedText = ['adminStats.custom.invalid']

const countNouns = ['account', 'guest', 'group', 'expense', 'payment']

const pinnedKeys = [...pinnedEnglishTexts.map(({ key }) => key), ...keysWithUnpinnedText]

function keysUnder(texts: object, prefix: string): string[] {
  return collectTexts(texts)
    .map(([path]) => path)
    .filter((key) => key.startsWith(prefix))
    .sort()
}

function placeholdersOf(text: string): string[] {
  return (text.match(placeholders) ?? []).sort()
}

describe('the texts of the admin statistics page', () => {
  it.each(pinnedEnglishTexts)('has the English wording for $key', ({ key, en: english }) => {
    expect(translated('en', key)).toBe(english)
  })

  it.each(pinnedKeys)('has a Macedonian text for %s', (key) => {
    expect(translated('mk', key)).not.toBe('')
  })

  it.each(keysWithUnpinnedText)('has an English text for %s', (key) => {
    expect(translated('en', key)).not.toBe('')
  })

  it.each(pinnedKeys)('does not use the word «курс» in the Macedonian text of %s', (key) => {
    expect(translated('mk', key).toLowerCase()).not.toContain('курс')
  })

  it.each(pinnedKeys)('uses the same placeholders in the English and the Macedonian text of %s', (key) => {
    expect(placeholdersOf(translated('mk', key))).toEqual(placeholdersOf(translated('en', key)))
  })

  it('has the same keys under adminStats in English and in Macedonian', () => {
    expect(keysUnder(mk, statsPrefix)).toEqual(keysUnder(en, statsPrefix))
  })

  it('has every pinned key under adminStats in both languages', () => {
    expect(keysUnder(en, statsPrefix)).toEqual(expect.arrayContaining(pinnedKeys))
    expect(keysUnder(mk, statsPrefix)).toEqual(expect.arrayContaining(pinnedKeys))
  })

  it.each(countNouns)('has a singular and a plural form of the count of %s in English and in Macedonian', (noun) => {
    for (const language of ['en', 'mk'] as const) {
      expect(translated(language, `adminStats.count.${noun}_one`)).toContain('{{count}}')
      expect(translated(language, `adminStats.count.${noun}_other`)).toContain('{{count}}')
    }
  })

  it.each([
    ['English', en],
    ['Macedonian', mk],
  ])('has only the plural forms one and other for the counts in %s, as the app picks them', (_name, texts) => {
    const suffixes = keysUnder(texts, 'adminStats.count.').map((key) => key.split('_')[1])

    expect([...new Set(suffixes)].sort()).toEqual(['one', 'other'])
  })
})
