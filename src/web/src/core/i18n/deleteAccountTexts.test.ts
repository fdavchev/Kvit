import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import en from './locales/en.json'
import mk from './locales/mk.json'

const placeholders = /\{\{\w+\}\}/g
const tags = /<\/?\w+>/g
const latinLetters = /[A-Za-zÀ-ɏ]/
const forbiddenMacedonianWords = ['курс', 'врск']
const keyPrefix = 'settings.deleteAccount'

const deleteAccountLockedTexts: [string, string, string][] = [
  ['settings.deleteAccount', 'Delete my account', 'Избриши го мојот профил'],
  ['settings.deleteAccountSheet.title', 'Delete your account?', 'Да го избришеш профилот?'],
  ['settings.deleteAccountSheet.nameLabel', 'Your name in groups', 'Твоето име во групите'],
  ['settings.deleteAccountSheet.keepName', 'Keep my name', 'Задржи го моето име'],
  [
    'settings.deleteAccountSheet.keepNameHint',
    'Your name stays on old expenses, so everyone can still read them.',
    'Твоето име останува на старите трошоци, па сите можат уште да ги читаат.',
  ],
  ['settings.deleteAccountSheet.formerMember', 'Show as “Former member”', 'Прикажи како „Поранешен член“'],
  [
    'settings.deleteAccountSheet.formerMemberHint',
    'Your name is replaced everywhere, also in the activity list.',
    'Твоето име се заменува насекаде, и во листата со активности.',
  ],
  [
    'settings.deleteAccountSheet.whatHappens',
    "Right away you are logged out everywhere and can't use this account. If you log in again within 30 days, the deletion is cancelled. After 30 days your email, login, Google link, personal budget and sessions are removed. Shared expenses stay, because other people's balances need them.",
    'Веднаш ќе те одјавиме насекаде и нема да можеш да го користиш профилот. Ако се најавиш повторно во рок од 30 дена, бришењето се откажува. По 30 дена се отстрануваат твојата е-пошта, најавата, поврзувањето со Google, личниот буџет и сесиите. Заедничките трошоци остануваат, бидејќи од нив зависат салдата на другите.',
  ],
  [
    'settings.deleteAccountSheet.whatHappensGuest',
    "Right away you are logged out and can't use this account. A guest account can't be logged into again, so this can't be undone. After 30 days it is removed. Shared expenses stay, because other people's balances need them.",
    'Веднаш те одјавуваме и нема да можеш да го користиш профилот. Гостинскиот профил нема како повторно да се најави, па ова не може да се врати. По 30 дена се отстранува. Заедничките трошоци остануваат, бидејќи од нив зависат салдата на другите.',
  ],
  ['settings.deleteAccountSheet.confirm', 'Delete my account', 'Избриши го мојот профил'],
  ['settings.deleteAccountDone', 'Deleted. Log in within 30 days to undo.', 'Избришано. Најави се во рок од 30 дена за да го вратиш.'],
  ['settings.deleteAccountDoneGuest', 'Deleted.', 'Избришано.'],
]

const otherDeleteAccountTexts: [string, string, string][] = [
  ['auth.deletionCancelled', 'Deletion cancelled. Your account is back.', 'Бришењето е откажано. Твојот профил е назад.'],
  ['errors.ACCOUNT_DELETE_NAME_CHOICE_INVALID', 'Choose how your name should show.', 'Избери како да се прикажува твоето име.'],
]

const deleteAccountTexts = [...deleteAccountLockedTexts, ...otherDeleteAccountTexts]

const languageTexts = [
  ['en', en],
  ['mk', mk],
] as const

function keysStartingWith(texts: object, prefix: string): string[] {
  return collectTexts(texts)
    .map(([path]) => path)
    .filter((path) => path.startsWith(prefix))
    .sort()
}

describe('the approved Phase 19 delete my account texts', () => {
  it.each(deleteAccountTexts)('has the approved English and Macedonian wording for %s', (key, english, macedonian) => {
    expect(translated('en', key)).toBe(english)
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(deleteAccountTexts)('has no tags or placeholders in %s', (key) => {
    for (const language of ['en', 'mk'] as const) {
      expect(translated(language, key)).not.toMatch(placeholders)
      expect(translated(language, key)).not.toMatch(tags)
    }
  })

  it.each(deleteAccountTexts)('writes the Macedonian text of %s in Cyrillic', (key) => {
    expect(translated('mk', key).replaceAll('Google', '')).not.toMatch(latinLetters)
  })

  it.each(deleteAccountTexts)('uses none of the forbidden Macedonian words in %s', (key) => {
    for (const word of forbiddenMacedonianWords) {
      expect(translated('mk', key).toLowerCase()).not.toContain(word)
    }
  })

  it.each(deleteAccountTexts)('never writes the singular «Активност» in the Macedonian text of %s', (key) => {
    expect(translated('mk', key)).not.toMatch(/Активност(?!и)/)
  })

  it.each(languageTexts)('has exactly the pinned texts under settings.deleteAccount in %s, so every new line is locked', (_language, texts) => {
    const pinnedKeys = deleteAccountLockedTexts.map(([key]) => key).sort()

    expect(keysStartingWith(texts, keyPrefix)).toEqual(pinnedKeys)
  })

  it('has the same keys in English and Macedonian', () => {
    expect(keysStartingWith(mk, keyPrefix)).toEqual(keysStartingWith(en, keyPrefix))
  })

  it('has no key settings.deleteAccountSheet.cancel, because the sheet reuses settlements.cancel', () => {
    expect(keysStartingWith(en, 'settings.deleteAccountSheet.cancel')).toEqual([])
    expect(keysStartingWith(mk, 'settings.deleteAccountSheet.cancel')).toEqual([])
  })

  it('has the Cancel words the sheet reuses at settlements.cancel', () => {
    expect(translated('en', 'settlements.cancel')).toBe('Cancel')
    expect(translated('mk', 'settlements.cancel')).toBe('Откажи')
  })

  it.each(languageTexts)('names the button in the privacy notice exactly as the Settings row does in %s', (language) => {
    const privacyBody = translated(language, 'privacy.sections.14.body')

    expect(privacyBody).toContain(translated(language, 'settings.deleteAccount'))
  })

  it.each(languageTexts)('names the button in the privacy notice exactly as the sheet confirm button does in %s', (language) => {
    const privacyBody = translated(language, 'privacy.sections.14.body')

    expect(privacyBody).toContain(translated(language, 'settings.deleteAccountSheet.confirm'))
  })

  it.each([
    ['en', '“Former member”'],
    ['mk', '„Поранешен член“'],
  ] as const)('names the Former member choice in privacy section 15 as the sheet does in %s', (language, quotedName) => {
    expect(translated(language, 'settings.deleteAccountSheet.formerMember')).toContain(quotedName)
    expect(translated(language, 'privacy.sections.15.body')).toContain(quotedName)
  })
})
