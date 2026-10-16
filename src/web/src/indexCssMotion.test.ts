import { describe, expect, it } from 'vitest'
import indexCss from '@/index.css?raw'
import { blockAfter } from '@/test/indexCssColors'

const noPreferenceHeader = '@media (prefers-reduced-motion: no-preference) {'
const toastRemovedCollapsedSelector =
  "html [data-sonner-toast][data-removed='true'][data-front='false'][data-swipe-out='false'][data-expanded='false']"

function declarationsOfRule(css: string, selector: string): string {
  return blockAfter(css, `${selector} {`).trim().replace(/\s+/g, ' ')
}

function motionBlock(): string {
  return blockAfter(indexCss, noPreferenceHeader)
}

describe('index.css motion', () => {
  it('has one block for people who do not prefer reduced motion', () => {
    expect(indexCss.split(noPreferenceHeader)).toHaveLength(2)
  })

  it('changes the motion of the Sonner toasts only inside the no-preference block', () => {
    const outside = indexCss.replace(motionBlock(), '')
    const sonnerDeclarationsOutside = [...outside.matchAll(/[^{}]*data-sonner[^{}]*\{([^}]*)\}/g)].map((rule) => rule[1])

    for (const declarations of sonnerDeclarationsOutside) {
      expect(declarations).not.toMatch(/transition|animation|transform|--y/)
    }
  })

  it('never uses !important there, so the reduced-motion rule of Sonner keeps winning', () => {
    expect(motionBlock()).not.toContain('!important')
  })

  it('moves the toasts, their opacity, height and shadow in 200 ms', () => {
    expect(declarationsOfRule(motionBlock(), 'html [data-sonner-toast]')).toBe(
      'transition: transform 200ms, opacity 200ms, height 200ms, box-shadow 200ms;',
    )
  })

  it('fades what is inside a toast in 200 ms', () => {
    expect(declarationsOfRule(motionBlock(), 'html [data-sonner-toast] > *')).toBe('transition: opacity 200ms;')
  })

  it('lifts the whole toast stack in 200 ms', () => {
    expect(declarationsOfRule(motionBlock(), 'html [data-sonner-toaster]')).toBe('transition: transform 200ms ease;')
  })

  it('lets a toast behind the front one leave in 200 ms', () => {
    expect(declarationsOfRule(motionBlock(), toastRemovedCollapsedSelector)).toBe(
      'transition: transform 200ms, opacity 200ms;',
    )
  })

  it('pops a new toast in from 8 px lower and 90 % size', () => {
    expect(declarationsOfRule(motionBlock(), "html [data-sonner-toast]:not([data-mounted='true'])")).toBe(
      '--y: translateY(8px) scale(0.9);',
    )
  })

  it('ends the pop at its place and full size once the toast is mounted', () => {
    expect(declarationsOfRule(motionBlock(), "html [data-sonner-toast][data-mounted='true']")).toBe(
      '--y: translateY(0) scale(1);',
    )
  })

  it('fades a group tab in by its opacity only', () => {
    expect(declarationsOfRule(indexCss, '@keyframes tab-panel-in')).toBe('from { opacity: 0; }')
  })

  it('fades a group tab in for 150 ms with ease-out', () => {
    expect(indexCss).toContain('--animate-tab-panel-in: tab-panel-in 150ms ease-out;')
  })
})
