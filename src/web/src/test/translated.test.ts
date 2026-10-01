import { describe, expect, it } from 'vitest'
import { createI18n } from '@/core/i18n/i18n'
import { languages } from '@/core/i18n/language'
import { translated } from './translated'

describe('translated', () => {
  it.each(languages)('returns the same text the app shows for a key in %s', async (language) => {
    const i18n = await createI18n(language)

    expect(translated(language, 'common.retry')).toBe(i18n.t('common.retry'))
  })

  it('throws an error naming the key when the key does not exist', () => {
    expect(() => translated('en', 'no.such.key')).toThrow('no.such.key')
  })

  it('throws an error naming the key when the key is a group and not a text', () => {
    expect(() => translated('en', 'welcome')).toThrow('"welcome"')
  })

  it('throws an error naming the key when the text is empty', () => {
    expect(() => translated('mk', 'welcome.taglineTranslation')).toThrow(
      'welcome.taglineTranslation',
    )
  })
})
