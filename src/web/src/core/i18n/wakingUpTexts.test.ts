import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import en from './locales/en.json'
import mk from './locales/mk.json'

interface WakingUpText {
  key: string
  en: string
  mk: string
}

const latinLetters = /[A-Za-zÀ-ɏ]/

const wakingUpTexts: WakingUpText[] = [
  { key: 'wakingUp.title', en: 'Waking up the server…', mk: 'Го будиме серверот…' },
  {
    key: 'wakingUp.hint',
    en: 'This takes about a minute the first time.',
    mk: 'Првиот пат ова трае околу една минута.',
  },
]

function wakingUpKeysOf(texts: object): string[] {
  return collectTexts(texts)
    .map(([path]) => path)
    .filter((key) => key.startsWith('wakingUp.'))
    .sort()
}

describe('the texts of the waking-up screen', () => {
  it.each(wakingUpTexts)('has the approved English wording for $key', ({ key, en: english }) => {
    expect(translated('en', key)).toBe(english)
  })

  it.each(wakingUpTexts)('has the draft Macedonian wording for $key', ({ key, mk: macedonian }) => {
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(wakingUpTexts)('writes the Macedonian text of $key with no Latin letters', ({ key }) => {
    expect(translated('mk', key)).not.toMatch(latinLetters)
  })

  it.each(wakingUpTexts)('has no Macedonian text for $key that is still the English one', ({ key }) => {
    expect(translated('mk', key)).not.toBe(translated('en', key))
  })

  it('has exactly the same keys under wakingUp in English and in Macedonian', () => {
    expect(wakingUpKeysOf(mk)).toEqual(wakingUpKeysOf(en))
  })

  it('has at least the texts that are pinned above under wakingUp', () => {
    expect(wakingUpKeysOf(en)).toEqual(expect.arrayContaining(wakingUpTexts.map(({ key }) => key)))
  })
})
