import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { colorOfToken, colorTokenOfClasses, contrastBetween, type ThemeName } from '@/test/indexCssColors'
import { KvitProgressBar } from './KvitProgressBar'

const minimumFillContrast = 3

const spentForEachState = [
  { state: 'ok', spentMinor: 1000 },
  { state: 'near', spentMinor: 9000 },
  { state: 'over', spentMinor: 12000 },
] as const

function backgroundTokenOf(element: Element, theme: ThemeName): string {
  const token = colorTokenOfClasses(element.className.split(/\s+/), 'bg', theme)
  if (token === null) {
    throw new Error(`Expected a background colour class in ${theme} mode, found: "${element.className}"`)
  }
  return token
}

function fillOf(bar: HTMLElement): Element {
  const fill = bar.querySelector('[data-limit-fill]')
  if (fill === null) {
    throw new Error(`The progress bar has no fill: ${bar.innerHTML}`)
  }
  return fill
}

describe('KvitProgressBar colours', () => {
  it.each<ThemeName>(['light', 'dark'])('draws the empty part in the card tint, not in the card colour, in %s mode', (theme) => {
    render(<KvitProgressBar label="Food" spentMinor={1000} limitMinor={10000} />)

    const track = backgroundTokenOf(screen.getByRole('progressbar'), theme)

    expect(track).toBe('card-tint')
    expect(colorOfToken(track, theme)).not.toEqual(colorOfToken('card', theme))
  })

  describe.each<ThemeName>(['light', 'dark'])('in %s mode', (theme) => {
    it.each(spentForEachState)('keeps the $state fill at least 3:1 against the empty part', ({ state, spentMinor }) => {
      render(<KvitProgressBar label="Food" spentMinor={spentMinor} limitMinor={10000} />)
      const bar = screen.getByRole('progressbar')

      const contrast = contrastBetween(backgroundTokenOf(fillOf(bar), theme), backgroundTokenOf(bar, theme), theme)

      expect(bar.dataset.limitState).toBe(state)
      expect(contrast).toBeGreaterThanOrEqual(minimumFillContrast)
    })
  })
})
