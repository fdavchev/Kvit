import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import en from './locales/en.json'
import mk from './locales/mk.json'

interface GuestJoinText {
  key: string
  en: string
  mk: string
}

const latinLetters = /[A-Za-zÀ-ɏ]/

const guestJoinTexts: GuestJoinText[] = [
  { key: 'join.guest.whoAreYou', en: 'Who are you? Tap your name:', mk: 'Кој си ти? Допри го своето име:' },
  { key: 'join.guest.whatsYourName', en: "What's your name?", mk: 'Како се викаш?' },
  { key: 'join.guest.imNew', en: "I'm new", mk: 'Јас сум нов/а' },
  { key: 'join.guest.haveAccount', en: 'Have an account?', mk: 'Имаш профил?' },
  { key: 'join.guest.logInWithEmail', en: 'Log in with email', mk: 'Најави се со е-пошта' },
  {
    key: 'join.guest.logInNote',
    en: 'After logging in you come back here and join.',
    mk: 'Откако ќе се најавиш, се враќаш тука и се придружуваш.',
  },
]

function guestJoinKeysOf(texts: object): string[] {
  return collectTexts(texts)
    .map(([path]) => path)
    .filter((key) => key.startsWith('join.guest.'))
    .sort()
}

describe('the texts of joining a group as a guest', () => {
  it.each(guestJoinTexts)('has the approved English wording for $key', ({ key, en: english }) => {
    expect(translated('en', key)).toBe(english)
  })

  it.each(guestJoinTexts)('has the draft Macedonian wording for $key', ({ key, mk: macedonian }) => {
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(guestJoinTexts)('writes the Macedonian text of $key with no Latin letters', ({ key }) => {
    expect(translated('mk', key)).not.toMatch(latinLetters)
  })

  it('has exactly the texts pinned above under join.guest, in English and in Macedonian', () => {
    const pinnedKeys = guestJoinTexts.map(({ key }) => key).sort()

    expect(guestJoinKeysOf(en)).toEqual(pinnedKeys)
    expect(guestJoinKeysOf(mk)).toEqual(pinnedKeys)
  })

  it('keeps the approved "No, I\'m new" of the signed-in question apart from the guest "I\'m new"', () => {
    expect(translated('en', 'join.imNew')).toBe("No, I'm new")
    expect(translated('mk', 'join.imNew')).toBe('Не, прв пат сум тука')
  })
})
