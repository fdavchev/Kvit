import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { outboxTexts } from '@/test/outboxTextsTestData'
import { translated } from '@/test/translated'
import { createI18n } from './i18n'
import en from './locales/en.json'
import mk from './locales/mk.json'

const latinLetters = /[A-Za-zÀ-ɏ]/
const placeholders = /\{\{\w+\}\}/g

const pluralKeys: string[] = ['waiting', 'failed', 'notCounted', 'logOutBody']

const pluralCounts: [number, string, string][] = [
  [1, 'waiting', '1 change waiting'],
  [2, 'waiting', '2 changes waiting'],
  [5, 'waiting', '5 changes waiting'],
  [1, 'failed', "1 change couldn't be saved"],
  [3, 'failed', "3 changes couldn't be saved"],
  [1, 'notCounted', '1 change waiting, not counted yet'],
  [2, 'notCounted', '2 changes waiting, not counted yet'],
  [1, 'logOutBody', "1 change hasn't been sent yet. If you log out now it is lost."],
  [4, 'logOutBody', "4 changes haven't been sent yet. If you log out now they are lost."],
]

const macedonianPluralCounts: [number, string, string][] = [
  [1, 'waiting', '1 промена чека'],
  [2, 'waiting', '2 промени чекаат'],
  [11, 'waiting', '11 промени чекаат'],
  [21, 'waiting', '21 промена чека'],
  [22, 'waiting', '22 промени чекаат'],
  [1, 'failed', '1 промена не можеше да се зачува'],
  [5, 'failed', '5 промени не можеа да се зачуваат'],
  [1, 'notCounted', '1 промена чека, сѐ уште не се брои'],
  [3, 'notCounted', '3 промени чекаат, сѐ уште не се бројат'],
  [21, 'logOutBody', '21 промена не е испратена. Ако се одјавиш сега, се губи.'],
  [12, 'logOutBody', '12 промени не се испратени. Ако се одјавиш сега, се губат.'],
]

function outboxKeysOf(texts: object): string[] {
  return collectTexts(texts)
    .map(([path]) => path)
    .filter((key) => key.startsWith('outbox.'))
    .sort()
}

describe('the texts of the outbox', () => {
  it.each(outboxTexts)('has the approved English wording for $key', ({ key, en: english }) => {
    expect(translated('en', key)).toBe(english)
  })

  it.each(outboxTexts)('has the draft Macedonian wording for $key', ({ key, mk: macedonian }) => {
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(outboxTexts)('writes the Macedonian text of $key with no Latin letters apart from the placeholders', ({ key }) => {
    expect(translated('mk', key).replace(placeholders, '')).not.toMatch(latinLetters)
  })

  it.each(outboxTexts)('has no Macedonian text for $key that is still the English one', ({ key }) => {
    expect(translated('mk', key)).not.toBe(translated('en', key))
  })

  it.each(outboxTexts)('uses the same placeholders in the English and the Macedonian text of $key', ({ key }) => {
    const english = translated('en', key).match(placeholders) ?? []
    const macedonian = translated('mk', key).match(placeholders) ?? []

    expect(macedonian.sort()).toEqual(english.sort())
  })

  it('has exactly the same keys under outbox in English and in Macedonian', () => {
    expect(outboxKeysOf(mk)).toEqual(outboxKeysOf(en))
  })

  it('has exactly the texts that are pinned under outbox and no other', () => {
    expect(outboxKeysOf(en)).toEqual(outboxTexts.map(({ key }) => key).sort())
  })

  it.each(pluralKeys)('has both a one and an other form for outbox.%s in English and Macedonian, each with the number in it', (key) => {
    for (const language of ['en', 'mk'] as const) {
      expect(translated(language, `outbox.${key}_one`)).toContain('{{count}}')
      expect(translated(language, `outbox.${key}_other`)).toContain('{{count}}')
    }
  })

  it.each(pluralCounts)('writes %i of outbox.%s in English as "%s"', async (count, key, english) => {
    const englishI18n = await createI18n('en')

    expect(englishI18n.t(`outbox.${key}`, { count })).toBe(english)
  })

  it.each(macedonianPluralCounts)('writes %i of outbox.%s in Macedonian as «%s»', async (count, key, macedonian) => {
    const macedonianI18n = await createI18n('mk')

    expect(macedonianI18n.t(`outbox.${key}`, { count })).toBe(macedonian)
  })

  it('never says «курс» for the euro price in any Macedonian outbox text', () => {
    const offenders = outboxKeysOf(mk).filter((key) => translated('mk', key).toLowerCase().includes('курс'))

    expect(offenders).toEqual([])
  })

  it('says «цената на еврото» in the Macedonian text that tells about the euro price', () => {
    expect(translated('mk', 'outbox.rateWhenSent').toLowerCase()).toContain('цената на еврото')
  })

  it('never uses «врск» in a Macedonian outbox text, which is kept for the two texts about the internet connection', () => {
    const offenders = outboxKeysOf(mk).filter((key) => translated('mk', key).toLowerCase().includes('врск'))

    expect(offenders).toEqual([])
  })
})
