import { describe, expect, it } from 'vitest'
import { languages } from '@/core/i18n/language'
import { translated } from '@/test/translated'
import { legalVersion, supportEmail } from './legalInfo'

describe('legalInfo', () => {
  it('has the contact email of Kvit', () => {
    expect(supportEmail).toBe('kvit.support@gmail.com')
  })

  it('has the version of the legal texts as a date', () => {
    expect(legalVersion).toBe('2026-11-04')
  })

  it.each(['terms', 'privacy'])('writes the version of the legal texts in the last-updated line of %s', (document) => {
    for (const language of languages) {
      expect(translated(language, `${document}.updated`)).toContain(legalVersion)
    }
  })

  it.each(languages)('has the same last-updated line for the terms and the privacy notice (%s)', (language) => {
    expect(translated(language, 'terms.updated')).toBe(translated(language, 'privacy.updated'))
  })
})
