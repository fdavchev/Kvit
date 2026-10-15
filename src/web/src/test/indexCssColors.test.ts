import { describe, expect, it } from 'vitest'
import { colorOfToken, colorTokenOfClasses, contrastBetween, hasColorToken } from './indexCssColors'

describe('indexCssColors', () => {
  it('reads a plain hex token of the light theme', () => {
    expect(colorOfToken('pill', 'light')).toEqual([255, 255, 255])
  })

  it('reads a token that points to another token', () => {
    expect(colorOfToken('card', 'light')).toEqual(colorOfToken('brand-50', 'light'))
  })

  it('takes the dark value of a token that the dark theme replaces', () => {
    expect(colorOfToken('danger', 'light')).toEqual([183, 31, 31])
    expect(colorOfToken('danger', 'dark')).toEqual([255, 138, 128])
  })

  it('mixes a color-mix token in srgb, so the dark card is the night color pulled 18% to orange', () => {
    expect(colorOfToken('card', 'dark')).toEqual([89, 55, 33])
  })

  it('measures the white pill on the peach page as 1.33:1, the number in the BACKLOG', () => {
    expect(contrastBetween('pill', 'background', 'light')).toBeCloseTo(1.33, 2)
  })

  it('measures the dark danger color on the dark card as 4.62:1', () => {
    expect(contrastBetween('danger', 'card', 'dark')).toBeCloseTo(4.62, 2)
  })

  it('gives the same ratio whichever color is named first', () => {
    expect(contrastBetween('card', 'danger', 'dark')).toBeCloseTo(contrastBetween('danger', 'card', 'dark'), 10)
  })

  it('says a token exists only when index.css has it', () => {
    expect(hasColorToken('field-border')).toBe(true)
    expect(hasColorToken('sm')).toBe(false)
  })

  it('throws a clear error for a token index.css does not have', () => {
    expect(() => colorOfToken('no-such-token', 'light')).toThrow('index.css has no colour token "--color-no-such-token"')
  })

  describe('colorTokenOfClasses', () => {
    const classes = ['text-sm', 'text-link', 'dark:text-primary', 'hover:text-foreground', 'border', 'border-field-border']

    it('takes the plain color class in the light theme and ignores sizes and variants', () => {
      expect(colorTokenOfClasses(classes, 'text', 'light')).toBe('link')
    })

    it('lets the dark: class win in the dark theme', () => {
      expect(colorTokenOfClasses(classes, 'text', 'dark')).toBe('primary')
    })

    it('reads the border color and not the border width', () => {
      expect(colorTokenOfClasses(classes, 'border', 'light')).toBe('field-border')
    })

    it('answers null when there is no color class for the utility', () => {
      expect(colorTokenOfClasses(['text-sm'], 'bg', 'light')).toBeNull()
    })
  })
})
