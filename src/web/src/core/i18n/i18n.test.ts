import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { createI18n } from './i18n'
import en from './locales/en.json'
import mk from './locales/mk.json'

function collectKeys(node: object): string[] {
  return collectTexts(node).map(([path]) => path)
}

describe('i18n', () => {
  it.each([
    [1, 'one'],
    [2, 'other'],
    [11, 'other'],
    [21, 'one'],
    [101, 'one'],
    [111, 'other'],
  ])('uses the Macedonian plural form for %i: %s', async (count, form) => {
    const i18n = await createI18n('mk')
    i18n.addResourceBundle('mk', 'translation', {
      pluralTest_one: 'one',
      pluralTest_other: 'other',
    })

    expect(i18n.t('pluralTest', { count })).toBe(form)
  })

  it('has exactly the same keys in en.json and mk.json', () => {
    expect(collectKeys(mk).sort()).toEqual(collectKeys(en).sort())
  })

  it.each([
    ['en', en, []],
    ['mk', mk, ['welcome.taglineTranslation']],
  ])('has no empty text in %s.json except the allowed ones', (_name, locale, allowedEmptyKeys) => {
    const emptyKeys = collectTexts(locale)
      .filter(([, text]) => text === '')
      .map(([path]) => path)

    expect(emptyKeys).toEqual(allowedEmptyKeys)
  })

  it('translates a Welcome string in each language', async () => {
    const english = await createI18n('en')
    const macedonian = await createI18n('mk')

    expect(english.t('welcome.haveAccount')).toBe('I already have an account')
    expect(macedonian.t('welcome.haveAccount')).toBe('Веќе имам профил')
  })
})
