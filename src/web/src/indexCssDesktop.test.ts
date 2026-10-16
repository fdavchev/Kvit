import { describe, expect, it } from 'vitest'
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
})
