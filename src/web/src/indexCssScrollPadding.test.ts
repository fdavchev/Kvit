import { describe, expect, it } from 'vitest'
import indexCss from '@/index.css?raw'
import { blockAfter } from '@/test/indexCssColors'

function scrollPaddingBottomOfThePage(): string {
  const htmlBlock = blockAfter(blockAfter(indexCss, '@layer base {'), '  html {')
  const match = /scroll-padding-bottom:\s*([^;]+);/.exec(htmlBlock)
  if (match === null) {
    throw new Error(`The html rule of the base layer has no scroll-padding-bottom: ${htmlBlock}`)
  }
  return match[1].trim().replace(/\s+/g, ' ')
}

describe('index.css room for the floating bars when the page scrolls to a focused element', () => {
  it('keeps a focused element above the bottom tab bar', () => {
    expect(scrollPaddingBottomOfThePage()).toContain('var(--bottom-bar-height)')
  })

  it('keeps a focused element above the outbox bar too', () => {
    expect(scrollPaddingBottomOfThePage()).toContain('var(--outbox-bar-space)')
  })

  it('keeps a focused element above the terms-changed pill too', () => {
    expect(scrollPaddingBottomOfThePage()).toContain('var(--terms-pill-space)')
  })

  it('adds the three heights in one calc, so it is no room at all on screens without the bars', () => {
    expect(scrollPaddingBottomOfThePage()).toBe('calc(var(--bottom-bar-height) + var(--outbox-bar-space) + var(--terms-pill-space))')
  })
})
