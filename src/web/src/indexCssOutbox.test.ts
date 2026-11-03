import { describe, expect, it } from 'vitest'
import indexCss from '@/index.css?raw'
import { blockAfter } from '@/test/indexCssColors'

const outboxSpaceProperty = '--outbox-bar-space'
const outboxSpaceVariable = `var(${outboxSpaceProperty})`
const bottomBarBlockHeader = ':root:has([data-bottom-bar]) {'
const outboxBarBlockHeader = ':root:has([data-outbox-bar]) {'

function declarationValue(block: string, property: string): string {
  const match = new RegExp(`${property}:\\s*([^;]+);`).exec(block)
  if (match === null) {
    throw new Error(`The block has no declaration of ${property}: ${block}`)
  }
  return match[1].trim().replace(/\s+/g, ' ')
}

function keyframesOfOutbox(): string[] {
  return [...indexCss.matchAll(/@keyframes\s+(outbox[\w-]*)/g)].map((match) => match[1])
}

describe('index.css room for the outbox bar', () => {
  it('has no room for the bar by default', () => {
    expect(declarationValue(blockAfter(indexCss, ':root {'), outboxSpaceProperty)).toBe('0px')
  })

  it('gives room for the bar only while the bar is on the page', () => {
    expect(indexCss).toContain(outboxBarBlockHeader)
    expect(declarationValue(blockAfter(indexCss, outboxBarBlockHeader), outboxSpaceProperty)).not.toMatch(/^0(px|rem)?$/)
  })

  it('gives room of a length with a unit or a calc, so it can be added to other lengths', () => {
    const room = declarationValue(blockAfter(indexCss, outboxBarBlockHeader), outboxSpaceProperty)

    expect(room).toMatch(/^(calc\(.+\)|\d+(\.\d+)?(px|rem))$/)
  })

  it('adds the room to the offset of the toasts, so no toast is covered by the bar', () => {
    const defaultOffset = declarationValue(blockAfter(indexCss, ':root {'), '--toast-offset-bottom')

    expect(defaultOffset).toContain(outboxSpaceVariable)
  })

  it('adds the room to the offset of the toasts also when the bottom tab bar is there', () => {
    const offsetWithTabBar = declarationValue(blockAfter(indexCss, bottomBarBlockHeader), '--toast-offset-bottom')

    expect(offsetWithTabBar).toContain(outboxSpaceVariable)
    expect(offsetWithTabBar).toContain('var(--bottom-bar-height)')
  })

  it('keeps the old parts of the toast offset: 16 px and the safe area by default, the tab bar height and 4 px with the tab bar', () => {
    const defaultOffset = declarationValue(blockAfter(indexCss, ':root {'), '--toast-offset-bottom')
    const offsetWithTabBar = declarationValue(blockAfter(indexCss, bottomBarBlockHeader), '--toast-offset-bottom')

    expect(defaultOffset).toContain('16px')
    expect(defaultOffset).toContain('env(safe-area-inset-bottom)')
    expect(offsetWithTabBar).toContain('4px')
  })

  it('moves no animation of the bar by anything other than transform and opacity', () => {
    for (const name of keyframesOfOutbox()) {
      const frames = blockAfter(indexCss, `@keyframes ${name}`)
      const properties = [...frames.matchAll(/([\w-]+)\s*:/g)].map((match) => match[1])

      expect(properties.filter((property) => !['transform', 'opacity', 'translate', 'scale'].includes(property))).toEqual([])
    }
  })
})
