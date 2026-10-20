import { fireEvent, render, screen } from '@testing-library/react'
import { useRef } from 'react'
import { describe, expect, it } from 'vitest'
import { blockAfter } from '@/test/indexCssColors'
import indexCss from '@/index.css?raw'
import { useSidewaysScroll } from './useSidewaysScroll'

const stripWidth = 300
const contentWidth = 1000
const lastScrollLeft = contentWidth - stripWidth

function Strip() {
  const stripRef = useRef<HTMLDivElement>(null)
  useSidewaysScroll(stripRef)
  return <div ref={stripRef} data-testid="strip" />
}

function renderStrip(scrollLeft: number, widths: { content: number; strip: number } = { content: contentWidth, strip: stripWidth }) {
  const rendered = render(<Strip />)
  const strip = screen.getByTestId('strip')
  Object.defineProperty(strip, 'scrollWidth', { configurable: true, value: widths.content })
  Object.defineProperty(strip, 'clientWidth', { configurable: true, value: widths.strip })
  Object.defineProperty(strip, 'scrollLeft', { configurable: true, writable: true, value: scrollLeft })
  fireEvent.scroll(strip)
  return { strip, unmount: rendered.unmount }
}

function turnWheel(strip: HTMLElement, init: WheelEventInit): boolean {
  return !fireEvent.wheel(strip, init)
}

describe('useSidewaysScroll', () => {
  describe('the mouse wheel', () => {
    it('scrolls the strip sideways when the wheel turns down, instead of the page', () => {
      const { strip } = renderStrip(300)

      const isPageScrollStopped = turnWheel(strip, { deltaY: 100 })

      expect(strip.scrollLeft).toBe(400)
      expect(isPageScrollStopped).toBe(true)
    })

    it('scrolls the strip back when the wheel turns up', () => {
      const { strip } = renderStrip(300)

      turnWheel(strip, { deltaY: -100 })

      expect(strip.scrollLeft).toBe(200)
    })

    it('stops at the end of the strip instead of going past it', () => {
      const { strip } = renderStrip(lastScrollLeft - 30)

      turnWheel(strip, { deltaY: 100 })

      expect(strip.scrollLeft).toBe(lastScrollLeft)
    })

    it.each([
      ['at the end, turning down', lastScrollLeft, 100],
      ['at the start, turning up', 0, -100],
    ])('lets the page scroll when the strip cannot move further (%s)', (_name, scrollLeft, deltaY) => {
      const { strip } = renderStrip(scrollLeft)

      const isPageScrollStopped = turnWheel(strip, { deltaY })

      expect(strip.scrollLeft).toBe(scrollLeft)
      expect(isPageScrollStopped).toBe(false)
    })

    it('lets the page scroll down when a fractional strip width keeps the end just short of the rounded last position', () => {
      const { strip } = renderStrip(lastScrollLeft - 1)
      const realLastScrollLeft = lastScrollLeft - 0.4
      let position = lastScrollLeft - 1
      Object.defineProperty(strip, 'scrollLeft', {
        configurable: true,
        get: () => Math.round(position),
        set: (value: number) => {
          position = Math.min(realLastScrollLeft, Math.max(0, value))
        },
      })

      const isPageScrollStopped = turnWheel(strip, { deltaY: 100 })

      expect(isPageScrollStopped).toBe(false)
    })

    it.each([
      ['at the end, turning down', lastScrollLeft, 100],
      ['at the start, turning up', 1, -100],
    ])(
      'lets the page scroll when the strip only jiggles by a pixel on every wheel turn (%s)',
      (_name, scrollLeft, deltaY) => {
        const { strip } = renderStrip(scrollLeft)
        let position = scrollLeft
        Object.defineProperty(strip, 'scrollLeft', {
          configurable: true,
          get: () => position,
          set: () => {
            position = position === scrollLeft ? scrollLeft - Math.sign(deltaY) : scrollLeft
          },
        })

        const turns = [turnWheel(strip, { deltaY }), turnWheel(strip, { deltaY }), turnWheel(strip, { deltaY })]

        expect(turns).toEqual([false, false, false])
      },
    )

    it('lets the page scroll when everything fits and there is nothing to scroll', () => {
      const { strip } = renderStrip(0, { content: stripWidth, strip: stripWidth })

      const isPageScrollStopped = turnWheel(strip, { deltaY: 100 })

      expect(isPageScrollStopped).toBe(false)
    })

    it('leaves a sideways swipe of a trackpad or a tilted wheel to the browser', () => {
      const { strip } = renderStrip(300)

      const isPageScrollStopped = turnWheel(strip, { deltaX: 60, deltaY: 10 })

      expect(strip.scrollLeft).toBe(300)
      expect(isPageScrollStopped).toBe(false)
    })

    it('leaves zooming with Ctrl and the wheel to the browser', () => {
      const { strip } = renderStrip(300)

      const isPageScrollStopped = turnWheel(strip, { deltaY: 100, ctrlKey: true })

      expect(strip.scrollLeft).toBe(300)
      expect(isPageScrollStopped).toBe(false)
    })

    it('turns a wheel that counts in lines into pixels', () => {
      const { strip } = renderStrip(300)

      turnWheel(strip, { deltaY: 3, deltaMode: WheelEvent.DOM_DELTA_LINE })

      expect(strip.scrollLeft).toBe(348)
    })

    it('stops listening once the strip is gone', () => {
      const { strip, unmount } = renderStrip(300)
      unmount()

      const isPageScrollStopped = turnWheel(strip, { deltaY: 100 })

      expect(isPageScrollStopped).toBe(false)
    })
  })

  describe('the faded edges', () => {
    it.each([
      ['at the start', 0, false, true],
      ['in the middle', 300, true, true],
      ['at the end', lastScrollLeft, true, false],
    ])('marks only the sides that have more to scroll to (%s)', (_name, scrollLeft, hasMoreBefore, hasMoreAfter) => {
      const { strip } = renderStrip(scrollLeft)

      expect(strip.hasAttribute('data-more-before')).toBe(hasMoreBefore)
      expect(strip.hasAttribute('data-more-after')).toBe(hasMoreAfter)
    })

    it('marks no side when everything fits', () => {
      const { strip } = renderStrip(0, { content: stripWidth, strip: stripWidth })

      expect(strip.hasAttribute('data-more-before')).toBe(false)
      expect(strip.hasAttribute('data-more-after')).toBe(false)
    })

    it('updates the marks after the wheel moved the strip', () => {
      const { strip } = renderStrip(0)

      turnWheel(strip, { deltaY: 100 })

      expect(strip.hasAttribute('data-more-before')).toBe(true)
    })
  })

  describe('the shared look in index.css', () => {
    const sidewaysScroll = blockAfter(indexCss, '@utility sideways-scroll')

    it('keeps a sideways swipe inside the strip, so it never turns into going back a page', () => {
      expect(sidewaysScroll).toContain('overscroll-behavior-x: contain;')
    })

    it('hides the scrollbar in every browser, so no bar hangs below a rounded strip', () => {
      expect(sidewaysScroll).toContain('scrollbar-width: none;')
      expect(blockAfter(sidewaysScroll, '&::-webkit-scrollbar')).toContain('display: none;')
    })

    it('fades only the sides that have more to scroll to', () => {
      expect(sidewaysScroll).toContain('mask-image')
      expect(sidewaysScroll).toContain('&[data-more-before]')
      expect(sidewaysScroll).toContain('&[data-more-after]')
    })
  })
})
