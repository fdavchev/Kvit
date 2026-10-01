import { describe, expect, it } from 'vitest'
import { isThemeChoice, themeChoices } from './theme'

describe('themeChoices', () => {
  it('lists same as device first, then light, then dark', () => {
    expect(themeChoices).toEqual(['system', 'light', 'dark'])
  })
})

describe('isThemeChoice', () => {
  it.each(themeChoices)('accepts the choice %s', (choice) => {
    expect(isThemeChoice(choice)).toBe(true)
  })

  it.each(['purple', '', 'Dark', ' light'])('rejects the text "%s"', (text) => {
    expect(isThemeChoice(text)).toBe(false)
  })
})
