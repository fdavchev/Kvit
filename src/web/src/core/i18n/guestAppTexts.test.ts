import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import en from './locales/en.json'
import mk from './locales/mk.json'

interface GuestAppText {
  key: string
  en: string
  mk: string
}

const appName = /Kvit/g
const placeholders = /\{\{\w+\}\}/g
const latinLetters = /[A-Za-zÀ-ɏ]/

const guestAppTexts: GuestAppText[] = [
  { key: 'members.guest', en: 'Guest', mk: 'Гостин' },
  { key: 'members.letInAgain', en: 'Let in again', mk: 'Пушти повторно' },
  {
    key: 'members.letInAgainHint',
    en: 'Use this when {{name}} lost their phone. The name becomes free to tap on the new one.',
    mk: 'Користи го ова кога {{name}} го загубил/а телефонот. Името станува слободно за допир на новиот.',
  },
  { key: 'members.letInAgainDone', en: '{{name}} can tap their name again', mk: '{{name}} може повторно да го допре своето име' },
  { key: 'settings.guest.title', en: "You're a guest", mk: 'Ти си гостин' },
  {
    key: 'settings.guest.body',
    en: 'Make an account to use Kvit on any phone and to create your own groups.',
    mk: 'Направи профил за да го користиш Kvit на секој телефон и за да правиш свои групи.',
  },
  { key: 'settings.guest.makeAccount', en: 'Make an account', mk: 'Направи профил' },
  { key: 'settings.guest.logOutTitle', en: 'Log out as a guest?', mk: 'Да се одјавиш како гостин?' },
  {
    key: 'settings.guest.logOutBody',
    en: "After logging out you can't get back in by yourself. Ask the group owner to let you in again. Or make an account first.",
    mk: 'Откако ќе се одјавиш, не можеш сам/а да се вратиш. Побарај од сопственикот на групата да те пушти повторно. Или прво направи профил.',
  },
  { key: 'settings.guest.logOutAnyway', en: 'Log out anyway', mk: 'Сепак одјави се' },
  { key: 'auth.makeAccount.keeps', en: 'You keep all your groups and expenses.', mk: 'Ги задржуваш сите групи и трошоци.' },
  { key: 'auth.makeAccount.done', en: 'Your account is ready', mk: 'Профилот е готов' },
  {
    key: 'groups.emptyGuest',
    en: 'You have no groups yet. Open a link a friend sent you.',
    mk: 'Сѐ уште немаш групи. Отвори линк што ти го испратил пријател.',
  },
  {
    key: 'home.noGroupsHintGuest',
    en: 'Open an invite link a friend sent you, then add your first expense.',
    mk: 'Отвори линк за покана што ти го испратил пријател, па додај го првиот трошок.',
  },
]

function keysUnder(texts: object, prefix: string): string[] {
  return collectTexts(texts)
    .map(([path]) => path)
    .filter((key) => key.startsWith(prefix))
    .sort()
}

function pinnedKeysUnder(prefix: string): string[] {
  return guestAppTexts.map(({ key }) => key).filter((key) => key.startsWith(prefix)).sort()
}

function placeholdersOf(text: string): string[] {
  return (text.match(placeholders) ?? []).sort()
}

describe('the texts of a guest inside the app', () => {
  it.each(guestAppTexts)('has the English wording of the mockup for $key', ({ key, en: english }) => {
    expect(translated('en', key)).toBe(english)
  })

  it.each(guestAppTexts)('has the draft Macedonian wording for $key', ({ key, mk: macedonian }) => {
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(guestAppTexts)('uses the same placeholders in the English and the Macedonian text of $key', ({ key }) => {
    expect(placeholdersOf(translated('mk', key))).toEqual(placeholdersOf(translated('en', key)))
  })

  it.each(guestAppTexts)('writes the Macedonian text of $key with no Latin letters apart from the placeholders and the app name', ({ key }) => {
    expect(translated('mk', key).replace(placeholders, '').replace(appName, '')).not.toMatch(latinLetters)
  })

  it.each(['settings.guest.', 'auth.makeAccount.'])('has exactly the texts pinned above under %s, in English and in Macedonian', (prefix) => {
    expect(keysUnder(en, prefix)).toEqual(pinnedKeysUnder(prefix))
    expect(keysUnder(mk, prefix)).toEqual(pinnedKeysUnder(prefix))
  })

  it('writes «сѐ» in the Macedonian empty Groups text for a guest with the Cyrillic ѐ (U+0450)', () => {
    expect(translated('mk', 'groups.emptyGuest').toLowerCase()).toContain('сѐ уште')
  })
})
