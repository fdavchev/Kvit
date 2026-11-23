import { describe, expect, it } from 'vitest'
import { translated } from '@/test/translated'

const latinLettersApartFromKvit = /[A-Za-zÀ-ɏ]/

describe('the Fix 21 Step 9 text', () => {
  it('has the English and the draft Macedonian wording for a page whose code could not be loaded', () => {
    expect(translated('en', 'errors.pageLoadFailed')).toBe("Couldn't load this page. Check your connection and try again.")
    expect(translated('mk', 'errors.pageLoadFailed')).toBe(
      'Не можевме да ја вчитаме страницата. Провери ја врската и обиди се повторно.',
    )
  })

  it('writes the Macedonian text with Cyrillic letters only', () => {
    expect(translated('mk', 'errors.pageLoadFailed')).not.toMatch(latinLettersApartFromKvit)
  })

  it('tells a page that could not be loaded apart from the generic text in both languages', () => {
    expect(translated('en', 'errors.pageLoadFailed')).not.toBe(translated('en', 'errors.generic'))
    expect(translated('mk', 'errors.pageLoadFailed')).not.toBe(translated('mk', 'errors.generic'))
  })
})
