import { describe, expect, it } from 'vitest'
import { translated } from '@/test/translated'

const placeholders = /\{\{\w+\}\}/g

const fix21Step6Texts: [string, string, string][] = [
  ['oneBill.nameTaken', 'Someone called “{{name}}” is already in this bill.', 'Некој по име „{{name}}“ е веќе во сметката.'],
  ['group.shareText', 'Join “{{name}}” on Kvit.', 'Придружи се на групата „{{name}}“ во Kvit.'],
]

describe('the Fix 21 Step 6 texts', () => {
  it.each(fix21Step6Texts)('has the English and the draft Macedonian wording for %s', (key, english, macedonian) => {
    expect(translated('en', key)).toBe(english)
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(fix21Step6Texts)('uses the same placeholders in the English and the Macedonian text of %s', (key) => {
    expect((translated('en', key).match(placeholders) ?? []).sort()).toEqual(
      (translated('mk', key).match(placeholders) ?? []).sort(),
    )
  })

  it('writes the Macedonian name-taken text of a bill with Cyrillic letters only apart from the placeholder', () => {
    expect(translated('mk', 'oneBill.nameTaken').replace(placeholders, '')).not.toMatch(/[A-Za-zÀ-ɏ]/)
  })

  it('puts the group name into the share text in Macedonian quotes „…“ and in English quotes “…”', () => {
    expect(translated('mk', 'group.shareText', { name: 'Охрид' })).toBe('Придружи се на групата „Охрид“ во Kvit.')
    expect(translated('en', 'group.shareText', { name: 'Ohrid' })).toBe('Join “Ohrid” on Kvit.')
  })
})
