import { describe, expect, it } from 'vitest'
import { kvitChipLook } from '@/shared/components/kvitChipLook'
import { contrastBetween, hasColorToken, type ThemeName } from '@/test/indexCssColors'
import { personalEntryChipLook } from './personalEntryChipLook'

const classes = personalEntryChipLook.split(/\s+/)
const lockedClasses = classes.filter((className) => className.startsWith('aria-disabled:'))
const themes: ThemeName[] = ['light', 'dark']

function colorTokenOf(utility: 'text' | 'bg' | 'border', candidates: readonly string[]): string {
  const token = candidates
    .filter((className) => className.startsWith(`${utility}-`))
    .map((className) => className.slice(utility.length + 1))
    .filter(hasColorToken)
    .at(-1)
  if (token === undefined) {
    throw new Error(`The chip look has no ${utility} colour among: ${candidates.join(' ')}`)
  }
  return token
}

const lockedLook = lockedClasses.map((className) => className.slice('aria-disabled:'.length))
const lockedText = colorTokenOf('text', [...kvitChipLook.split(/\s+/), ...lockedLook])
const lockedBackground = colorTokenOf('bg', [...kvitChipLook.split(/\s+/), ...lockedLook])
const lockedBorder = colorTokenOf('border', [...kvitChipLook.split(/\s+/), ...lockedLook])

describe('the locked chip look (a date that cannot change)', () => {
  it('does not fade the chip, so its text stays readable', () => {
    expect(lockedClasses.filter((className) => className.startsWith('aria-disabled:opacity-'))).toEqual([])
  })

  it('keeps the normal field text colour', () => {
    expect(lockedClasses.filter((className) => className.startsWith('aria-disabled:text-'))).toEqual([])
    expect(lockedText).toBe('field-foreground')
  })

  it('looks different from a tappable chip: a dashed edge on the muted background', () => {
    expect(lockedClasses).toEqual(expect.arrayContaining(['aria-disabled:border-dashed', 'aria-disabled:bg-muted']))
    expect(lockedBackground).toBe('muted')
  })

  it('ignores taps and never shrinks as if pressed', () => {
    expect(lockedClasses).toEqual(expect.arrayContaining(['aria-disabled:pointer-events-none', 'aria-disabled:active:scale-none']))
  })

  it.each(themes)('has at least 4.5:1 text contrast on its background (%s)', (theme) => {
    expect(contrastBetween(lockedText, lockedBackground, theme)).toBeGreaterThanOrEqual(4.5)
  })

  it.each(themes)('has at least 3:1 contrast for its dashed edge on its background (%s)', (theme) => {
    expect(contrastBetween(lockedBorder, lockedBackground, theme)).toBeGreaterThanOrEqual(3)
  })

  it('still fades a chip that is disabled while the sheet is busy saving', () => {
    expect(classes).toEqual(expect.arrayContaining(['disabled:pointer-events-none', 'disabled:opacity-50']))
  })
})
