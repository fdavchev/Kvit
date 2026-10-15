import { describe, expect, it } from 'vitest'
import { translated } from '@/test/translated'

const latinLetters = /[A-Za-zÀ-ɏ]/

describe('the text of the toast region label', () => {
  it('has the approved English wording for toasts.region', () => {
    expect(translated('en', 'toasts.region')).toBe('Notifications')
  })

  it('has the draft Macedonian wording for toasts.region', () => {
    expect(translated('mk', 'toasts.region')).toBe('Известувања')
  })

  it('writes the Macedonian text of toasts.region with no Latin letters', () => {
    expect(translated('mk', 'toasts.region')).not.toMatch(latinLetters)
  })
})
