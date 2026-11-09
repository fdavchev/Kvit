import { describe, expect, it } from 'vitest'
import indexCss from '@/index.css?raw'
import { blockAfter } from '@/test/indexCssColors'

const termsPillSpaceProperty = '--terms-pill-space'
const termsPillSpaceVariable = `var(${termsPillSpaceProperty})`
const outboxSpaceVariable = 'var(--outbox-bar-space)'
const bottomBarBlockHeader = ':root:has([data-bottom-bar]) {'

function declarationValues(block: string, property: string): string[] {
  return [...block.matchAll(new RegExp(`${property}:\\s*([^;]+);`, 'g'))].map((match) => match[1].trim().replace(/\s+/g, ' '))
}

function countOf(text: string, part: string): number {
  return text.split(part).length - 1
}

function onlyDeclarationValue(block: string, property: string): string {
  const values = declarationValues(block, property)
  if (values.length !== 1) {
    throw new Error(`Expected one declaration of ${property} in the block, found ${values.length}: ${block}`)
  }
  return values[0]
}

describe('index.css room for the terms-changed pill', () => {
  it('has no room for the pill by default, until the pill measures itself', () => {
    expect(onlyDeclarationValue(blockAfter(indexCss, ':root {'), termsPillSpaceProperty)).toBe('0px')
  })

  it('adds the room to the offset of the toasts, so no toast lands on the pill', () => {
    expect(onlyDeclarationValue(blockAfter(indexCss, ':root {'), '--toast-offset-bottom')).toContain(termsPillSpaceVariable)
  })

  it('adds the room to the offset of the toasts also when the bottom tab bar is there', () => {
    expect(onlyDeclarationValue(blockAfter(indexCss, bottomBarBlockHeader), '--toast-offset-bottom')).toContain(termsPillSpaceVariable)
  })

  it('adds the room to every bottom padding of the screens that already makes room for the outbox bar', () => {
    const paddings = declarationValues(blockAfter(indexCss, '@utility screen-padding {'), 'padding-bottom')

    expect(paddings).toHaveLength(3)
    for (const padding of paddings) {
      expect(countOf(padding, termsPillSpaceVariable)).toBe(countOf(padding, outboxSpaceVariable))
    }
  })
})
