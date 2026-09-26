import { describe, expect, it } from 'vitest'
import { detectLanguage } from './detectLanguage'

describe('detectLanguage', () => {
  it('uses the saved choice over the phone languages', () => {
    expect(detectLanguage('en', ['mk-MK'])).toBe('en')
    expect(detectLanguage('mk', ['en-US'])).toBe('mk')
  })

  it('ignores a saved value that is not a supported language', () => {
    expect(detectLanguage('de', ['mk-MK'])).toBe('mk')
  })

  it('picks Macedonian for a Macedonian phone', () => {
    expect(detectLanguage(null, ['mk-MK', 'en-US'])).toBe('mk')
    expect(detectLanguage(null, ['mk'])).toBe('mk')
  })

  it('picks the first supported language in the phone list', () => {
    expect(detectLanguage(null, ['de-DE', 'mk-MK'])).toBe('mk')
    expect(detectLanguage(null, ['en-GB', 'mk-MK'])).toBe('en')
  })

  it('falls back to English when no phone language is supported', () => {
    expect(detectLanguage(null, ['de-DE', 'sr-RS'])).toBe('en')
    expect(detectLanguage(null, [])).toBe('en')
  })
})
