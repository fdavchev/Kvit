import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import en from './locales/en.json'
import mk from './locales/mk.json'

const placeholders = /\{\{\w+\}\}/g
const tags = /<\/?\w+>/g
const latinLetters = /[A-Za-zÀ-ɏ]/
const forbiddenMacedonianWords = ['курс', 'врск']
const allowedLatinInMacedonian = ['Kvit', 'kvit.support@gmail.com']
const supportEmail = 'kvit.support@gmail.com'

const ownerToolsTexts: [string, string, string][] = [
  ['settings.ownerTools', 'Owner tools', 'Алатки за сопственикот'],
  ['ownerTools.title', 'Owner tools', 'Алатки за сопственикот'],
  ['ownerTools.tabs.people', 'People', 'Луѓе'],
  ['ownerTools.tabs.groups', 'Groups', 'Групи'],
  ['ownerTools.tabs.log', 'Log', 'Дневник'],
  ['ownerTools.people.searchLabel', 'Find by email, name or number', 'Најди по е-пошта, име или број'],
  ['ownerTools.people.search', 'Search', 'Барај'],
  ['ownerTools.people.empty', 'Nobody found.', 'Никој не е пронајден.'],
  ['ownerTools.people.groupCount_one', '{{count}} group', '{{count}} група'],
  ['ownerTools.people.groupCount_other', '{{count}} groups', '{{count}} групи'],
  ['ownerTools.badge.suspended', 'Suspended', 'Суспендиран'],
  ['ownerTools.badge.guest', 'Guest', 'Гостин'],
  ['ownerTools.badge.admin', 'Owner of Kvit', 'Сопственик на Kvit'],
  ['ownerTools.badge.deletionPending', 'Deleting', 'Се брише'],
  ['ownerTools.badge.deleted', 'Deleted', 'Избришан'],
  ['ownerTools.suspend', 'Suspend', 'Суспендирај'],
  ['ownerTools.unsuspend', 'Unsuspend', 'Отстрани суспензија'],
  ['ownerTools.eraseName', 'Erase name', 'Избриши го името'],
  ['ownerTools.reasonLabel', 'Reason (saved in the log)', 'Причина (се чува во дневникот)'],
  ['ownerTools.confirm', 'Confirm', 'Потврди'],
  ['ownerTools.sheet.suspend.title', 'Suspend {{name}}?', 'Да се суспендира {{name}}?'],
  [
    'ownerTools.sheet.suspend.body',
    'They can still log in but see only a suspended screen. Everything else is refused until you unsuspend.',
    'Лицето сѐ уште може да се најави, но гледа само екран за суспендиран профил. Сѐ друго е одбиено додека не ја отстраниш суспензијата.',
  ],
  ['ownerTools.sheet.unsuspend.title', 'Unsuspend {{name}}?', 'Да се отстрани суспензијата на {{name}}?'],
  ['ownerTools.sheet.unsuspend.body', 'They can use Kvit again.', 'Лицето повторно може да го користи Kvit.'],
  ['ownerTools.sheet.eraseName.title', 'Erase the name of {{name}}?', 'Да се избрише името на {{name}}?'],
  [
    'ownerTools.sheet.eraseName.body',
    'This deletes the account now and shows “Former member” everywhere. It cannot be undone.',
    'Профилот се брише веднаш, а насекаде се покажува „Поранешен член“. Ова не може да се врати.',
  ],
  ['ownerTools.sheet.closeGroup.title', 'Close {{name}}?', 'Да се затвори „{{name}}“?'],
  [
    'ownerTools.sheet.closeGroup.body',
    'The group becomes read-only for everyone and its owner cannot unlock it.',
    'Групата станува само за читање за сите, а нејзиниот сопственик не може да ја отклучи.',
  ],
  ['ownerTools.sheet.reopenGroup.title', 'Reopen {{name}}?', 'Да се отвори повторно „{{name}}“?'],
  ['ownerTools.sheet.reopenGroup.body', 'The group works normally again.', 'Групата повторно работи нормално.'],
  ['ownerTools.done.suspend', '{{name}} is suspended', 'Суспендирано: {{name}}'],
  ['ownerTools.done.unsuspend', '{{name}} can use Kvit again', '{{name}} повторно може да го користи Kvit'],
  ['ownerTools.done.eraseName', 'The name is erased', 'Името е избришано'],
  ['ownerTools.done.closeGroup', 'Group closed', 'Групата е затворена'],
  ['ownerTools.done.reopenGroup', 'Group reopened', 'Групата е отворена повторно'],
  [
    'ownerTools.groups.searchLabel',
    'Group number (from a report email)',
    'Број на групата (од е-пошта за пријава)',
  ],
  ['ownerTools.groups.search', 'Find', 'Најди'],
  ['ownerTools.groups.members_one', '{{count}} member', '{{count}} член'],
  ['ownerTools.groups.members_other', '{{count}} members', '{{count}} члена'],
  ['ownerTools.groups.owner', 'Owner: {{name}}', 'Сопственик: {{name}}'],
  ['ownerTools.groups.status.open', 'Open', 'Отворена'],
  ['ownerTools.groups.status.closing', 'Closing', 'Се затвора'],
  ['ownerTools.groups.status.finished', 'Finished', 'Завршена'],
  ['ownerTools.groups.status.closedByOwner', 'Closed by the owner of Kvit', 'Затворена од сопственикот на Kvit'],
  ['ownerTools.groups.status.deleted', 'Deleted', 'Избришана'],
  ['ownerTools.groups.close', 'Close group', 'Затвори ја групата'],
  ['ownerTools.groups.reopen', 'Reopen group', 'Отвори ја групата повторно'],
  ['ownerTools.log.empty', 'Nothing done yet.', 'Сѐ уште ништо не е направено.'],
  ['ownerTools.log.line', '{{admin}} · {{action}} · {{target}}', '{{admin}} · {{action}} · {{target}}'],
  ['ownerTools.log.action.Suspended', 'Suspended', 'Суспендираше'],
  ['ownerTools.log.action.Unsuspended', 'Unsuspended', 'Ја отстрани суспензијата'],
  ['ownerTools.log.action.GroupClosed', 'Closed a group', 'Затвори група'],
  ['ownerTools.log.action.GroupReopened', 'Reopened a group', 'Отвори група повторно'],
  ['ownerTools.log.action.NameErased', 'Erased a name', 'Избриша име'],
  ['suspended.title', 'Your account is suspended', 'Твојот профил е суспендиран'],
  ['suspended.body', 'Contact kvit.support@gmail.com.', 'Пиши на kvit.support@gmail.com.'],
  [
    'group.closedByAdmin',
    '🔒 Closed by the owner of Kvit · read-only',
    '🔒 Затворена од сопственикот на Kvit · само за читање',
  ],
  [
    'errors.ACCOUNT_SUSPENDED',
    'Your account is suspended. Contact kvit.support@gmail.com.',
    'Твојот профил е суспендиран. Пиши на kvit.support@gmail.com.',
  ],
  [
    'errors.GROUP_CLOSED_BY_ADMIN',
    'This group was closed by the owner of Kvit.',
    'Оваа група ја затвори сопственикот на Kvit.',
  ],
  [
    'errors.OWNER_REASON_INVALID',
    'Write a reason (up to 500 characters).',
    'Напиши причина (најмногу 500 знаци).',
  ],
  ['errors.OWNER_PERSON_NOT_FOUND', "This person doesn't exist.", 'Оваа личност не постои.'],
  ['errors.OWNER_GROUP_NOT_FOUND', "This group doesn't exist.", 'Оваа група не постои.'],
  [
    'errors.OWNER_CANNOT_ACT_ON_ADMIN',
    "You can't do this to an owner of Kvit.",
    'Ова не може да се направи на сопственик на Kvit.',
  ],
]

const lockedPrefixes = ['ownerTools.', 'suspended.', 'settings.ownerTools', 'group.closedByAdmin']

const pinnedErrorKeys = [
  'errors.ACCOUNT_SUSPENDED',
  'errors.GROUP_CLOSED_BY_ADMIN',
  'errors.OWNER_REASON_INVALID',
  'errors.OWNER_PERSON_NOT_FOUND',
  'errors.OWNER_GROUP_NOT_FOUND',
  'errors.OWNER_CANNOT_ACT_ON_ADMIN',
]

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

function placeholdersOf(text: string): string[] {
  return (text.match(placeholders) ?? []).sort()
}

function macedonianWithoutAllowedLatin(text: string): string {
  return allowedLatinInMacedonian
    .reduce((rest, allowed) => rest.replaceAll(allowed, ''), text)
    .replace(placeholders, '')
}

describe('the Phase 19 owner tools, suspended and closed-group texts', () => {
  it.each(ownerToolsTexts)('has the English and Macedonian wording for %s', (key, english, macedonian) => {
    expect(translated('en', key)).toBe(english)
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(ownerToolsTexts)('uses the same placeholders in English and Macedonian for %s', (key) => {
    expect(placeholdersOf(translated('mk', key))).toEqual(placeholdersOf(translated('en', key)))
  })

  it.each(ownerToolsTexts)('has no tags in %s', (key) => {
    for (const language of ['en', 'mk'] as const) {
      expect(translated(language, key)).not.toMatch(tags)
    }
  })

  it.each(ownerToolsTexts)('writes the Macedonian text of %s in Cyrillic apart from the name Kvit, the email and the placeholders', (key) => {
    expect(macedonianWithoutAllowedLatin(translated('mk', key))).not.toMatch(latinLetters)
  })

  it.each(ownerToolsTexts)('uses none of the forbidden Macedonian words in %s', (key) => {
    for (const word of forbiddenMacedonianWords) {
      expect(translated('mk', key).toLowerCase()).not.toContain(word)
    }
  })

  it.each(ownerToolsTexts)('never writes the singular «Активност» in the Macedonian text of %s', (key) => {
    expect(translated('mk', key)).not.toMatch(/Активност(?!и)/)
  })

  it.each(ownerToolsTexts.filter(([, english]) => english.includes('“')))('uses the quotes „ “ in the Macedonian text of %s where English uses “ ”', (key) => {
    expect(translated('mk', key)).toContain('„')
    expect(translated('mk', key)).not.toContain('"')
  })

  it.each(languageTexts)('has exactly the pinned keys under each locked group of keys in %s, so every new line is locked', (_language, texts) => {
    const pinnedKeys = ownerToolsTexts.map(([key]) => key)

    for (const prefix of lockedPrefixes) {
      const pinnedUnderPrefix = pinnedKeys.filter((key) => key.startsWith(prefix)).sort()

      expect(keysStartingWith(texts, prefix), prefix).toEqual(pinnedUnderPrefix)
    }
  })

  it.each(lockedPrefixes)('has the same keys under %s in English and in Macedonian', (prefix) => {
    expect(keysStartingWith(mk, prefix)).toEqual(keysStartingWith(en, prefix))
  })

  it.each(pinnedErrorKeys)('has %s in both languages', (key) => {
    expect(translated('en', key)).not.toBe('')
    expect(translated('mk', key)).not.toBe('')
  })

  it('keeps the email in the suspended body and in the suspended error exactly as the mailto link will write it', () => {
    for (const key of ['suspended.body', 'errors.ACCOUNT_SUSPENDED']) {
      for (const language of ['en', 'mk'] as const) {
        expect(translated(language, key).split(supportEmail), `${language} ${key}`).toHaveLength(2)
      }
    }
  })

  it('gives the suspended body no tags, because the email link is made from the plain address', () => {
    for (const language of ['en', 'mk'] as const) {
      expect(translated(language, 'suspended.body')).not.toMatch(tags)
    }
  })
})

describe('the plural forms of the owner tools texts', () => {
  const pluralKeys = ['ownerTools.people.groupCount', 'ownerTools.groups.members']

  it.each(pluralKeys)('has a singular and a plural form of %s with the count in English and in Macedonian', (key) => {
    for (const language of ['en', 'mk'] as const) {
      expect(translated(language, `${key}_one`)).toContain('{{count}}')
      expect(translated(language, `${key}_other`)).toContain('{{count}}')
    }
  })

  it.each(languageTexts)('has only the plural forms one and other under ownerTools in %s, as the app picks them', (_language, texts) => {
    const suffixes = keysStartingWith(texts, 'ownerTools.')
      .filter((key) => /_[a-z]+$/.test(key.split('.').at(-1) ?? ''))
      .map((key) => key.split('_').at(-1))

    expect([...new Set(suffixes)].sort()).toEqual(['one', 'other'])
  })

  it.each(languageTexts)('has a matching _other for every _one under ownerTools in %s', (_language, texts) => {
    const keys = keysStartingWith(texts, 'ownerTools.')
    const ones = keys.filter((key) => key.endsWith('_one')).map((key) => key.slice(0, -'_one'.length))
    const others = keys.filter((key) => key.endsWith('_other')).map((key) => key.slice(0, -'_other'.length))

    expect(others).toEqual(ones)
  })
})
