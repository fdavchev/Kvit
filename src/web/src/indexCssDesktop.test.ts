import { describe, expect, it } from 'vitest'
import desktopRailLayout from '@/core/router/DesktopRailLayout.tsx?raw'
import indexCss from '@/index.css?raw'
import { blockAfter } from '@/test/indexCssColors'

function lgBlockInside(header: string): string {
  return blockAfter(blockAfter(indexCss, header), '@variant lg {').trim().replace(/\s+/g, ' ')
}

describe('index.css on desktop', () => {
  it('sets the lg breakpoint to 64 rem', () => {
    expect(blockAfter(indexCss, '@theme {')).toMatch(/--breakpoint-lg:\s*64rem;/)
  })

  it('grows the text a little from the lg breakpoint with a clamp, never below the size the person chose', () => {
    expect(lgBlockInside('  html {')).toBe('font-size: clamp(100%, 87.5% + 0.25vw, 112.5%);')
  })

  it('gives the screens more room around them from the lg breakpoint with clamps, keeping the safe areas', () => {
    const lgPadding = lgBlockInside('@utility screen-padding {')

    for (const side of ['top', 'right', 'bottom', 'left']) {
      expect(lgPadding).toContain(`padding-${side}: max(clamp(`)
    }
    expect(lgPadding).toContain('env(safe-area-inset-top)')
    expect(lgPadding).toContain('env(safe-area-inset-left)')
    expect(lgPadding).toContain('env(safe-area-inset-right)')
  })

  it('keeps space for the bottom bar only while the bar is on the page', () => {
    expect(indexCss).toMatch(/:root:has\(\[data-bottom-bar\]\) \{\s*--bottom-bar-height:/)
    expect(blockAfter(indexCss, ':root {')).toMatch(/--bottom-bar-height: 0px;/)
  })

  it('has no rail width by default, so the phone layout has no side menu to make room for', () => {
    expect(blockAfter(indexCss, ':root {')).toMatch(/--rail-width:\s*0px;/)
  })

  it('sets the rail width from the lg breakpoint to the width of the side menu', () => {
    expect(lgBlockInside(':root {')).toContain('--rail-width: clamp(11rem, 17vw, 16rem);')
  })

  it('lets the desktop layout take the width of its side menu column from --rail-width, so the width is written once', () => {
    expect(desktopRailLayout).toContain('var(--rail-width)')
    expect(desktopRailLayout).not.toContain('clamp(11rem,17vw,16rem)')
  })

  describe('the toast area next to the side menu', () => {
    const railToasterRule = "html:has([data-kvit-rail]) [data-sonner-toaster][data-x-position='center']"

    it('moves the centred toast area to the middle of the content column, next to the side menu, from the lg breakpoint', () => {
      expect(lgBlockInside(railToasterRule)).toBe('left: calc(50% + var(--rail-width) / 2);')
    })

    it('changes nothing below the lg breakpoint, where there is no side menu', () => {
      const rule = blockAfter(indexCss, `${railToasterRule} {`).trim()

      expect(rule.startsWith('@variant lg {')).toBe(true)
      expect(rule.endsWith('}')).toBe(true)
      expect(rule.slice('@variant lg {'.length, -1)).not.toContain('{')
    })
  })
})
