import { expect } from 'vitest'
import { classesOf } from './budgetLeftCardTestHelpers'

export type BarState = 'ok' | 'near' | 'over'

export interface BarCase {
  label: string
  spentMinor: number
  state: BarState
}

export const barLimitMinor = 1000000

export const barCases: readonly BarCase[] = [
  { label: 'nothing spent', spentMinor: 0, state: 'ok' },
  { label: '79.9 % of the limit', spentMinor: 799000, state: 'ok' },
  { label: '79.99 % of the limit', spentMinor: 799900, state: 'ok' },
  { label: 'exactly 80 % of the limit', spentMinor: 800000, state: 'near' },
  { label: '99 % of the limit', spentMinor: 990000, state: 'near' },
  { label: 'exactly 100 % of the limit', spentMinor: 1000000, state: 'near' },
  { label: '100 % and 1 denar', spentMinor: 1000100, state: 'over' },
  { label: 'twice the limit', spentMinor: 2000000, state: 'over' },
]

export const barCasesInCents: readonly BarCase[] = [
  { label: '80 % less 1 cent', spentMinor: 799999, state: 'ok' },
  { label: 'exactly 80 %', spentMinor: 800000, state: 'near' },
  { label: 'exactly 100 %', spentMinor: 1000000, state: 'near' },
  { label: '100 % and 1 cent', spentMinor: 1000001, state: 'over' },
]

const fillColours: Record<BarState, string> = {
  ok: 'bg-positive',
  near: 'bg-warning',
  over: 'bg-negative',
}

function fillOf(bar: HTMLElement): HTMLElement {
  const fill = bar.firstElementChild
  if (!(fill instanceof HTMLElement)) {
    throw new Error('The progress bar has no element inside it that could draw the fill')
  }
  return fill
}

export function expectBarShows(bar: HTMLElement, spentMinor: number, limitMinor: number, state: BarState): void {
  const cappedMinor = Math.min(spentMinor, limitMinor)
  const fill = fillOf(bar)

  expect(bar.getAttribute('role')).toBe('progressbar')
  expect(bar.getAttribute('aria-valuemin')).toBe('0')
  expect(bar.getAttribute('aria-valuenow')).toBe(String(cappedMinor))
  expect(bar.getAttribute('aria-valuemax')).toBe(String(limitMinor))
  expect(bar.getAttribute('data-limit-state')).toBe(state)
  expect(fill.style.transform).toBe(`scaleX(${cappedMinor / limitMinor})`)
  expect(classesOf(fill)).toContain(fillColours[state])
  for (const otherState of Object.keys(fillColours).filter((name) => name !== state)) {
    expect(classesOf(fill)).not.toContain(fillColours[otherState as BarState])
  }
}

export function expectBarMoves(bar: HTMLElement): void {
  const fill = fillOf(bar)
  const classes = classesOf(fill)

  expect(fill.style.width).toBe('')
  expect(classes).toContain('origin-left')
  expect(classes.filter((name) => name.startsWith('transition'))).toEqual(['transition-transform'])
  expect(classes).toContain('duration-200')
  expect(classes).toContain('motion-reduce:transition-none')
}
