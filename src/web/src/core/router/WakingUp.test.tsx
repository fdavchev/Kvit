import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { WakingUp } from './WakingUp'

const topPadding = /^pt-\[max\((\d+)px,env\(safe-area-inset-top\)\)\]$/
const bottomPadding = /^pb-\[calc\(max\((\d+)px,env\(safe-area-inset-bottom\)\)\+(.+)\)\]$/

async function renderedScreenClasses(): Promise<string[]> {
  await renderElementWithProviders(<WakingUp />, '/waking')
  return Array.from(screen.getByRole('main').classList)
}

function matchOf(classes: string[], pattern: RegExp): RegExpExecArray {
  for (const className of classes) {
    const match = pattern.exec(className)
    if (match !== null) {
      return match
    }
  }
  throw new Error(`Expected one class of the screen to match ${pattern}, found: ${classes.join(' ')}`)
}

describe('the Waking up screen', () => {
  it('centres its block in the middle of the screen', async () => {
    const classes = await renderedScreenClasses()

    expect(classes).toEqual(expect.arrayContaining(['items-center', 'justify-center']))
  })

  it('pads the top and the bottom by the same 24 px, so the block sits at the vertical centre', async () => {
    const classes = await renderedScreenClasses()

    const top = matchOf(classes, topPadding)
    const bottom = matchOf(classes, bottomPadding)

    expect(top[1]).toBe('24')
    expect(bottom[1]).toBe(top[1])
  })

  it('adds the outbox bar and the terms pill to the bottom only, so a bar at the bottom does not push the block off the centre of the free space', async () => {
    const classes = await renderedScreenClasses()

    const bottom = matchOf(classes, bottomPadding)

    expect(bottom[2]).toBe('var(--outbox-bar-space)+var(--terms-pill-space)')
    expect(classes.filter((className) => topPadding.test(className))).toHaveLength(1)
  })
})
