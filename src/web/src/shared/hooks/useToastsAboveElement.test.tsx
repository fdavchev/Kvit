import { renderHook } from '@testing-library/react'
import type { RefObject } from 'react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { installFakeResizeObservers, type FakeResizeObservers } from '@/test/resizeObserverTestHelpers'
import { clearToastLiftOnPage, elementWithHeight, toastLiftOnPage } from '@/test/toastLiftTestHelpers'
import { useToastsAboveElement } from './useToastsAboveElement'

interface HookProps {
  elementRef: RefObject<HTMLElement | null>
  gapPx: number | undefined
}

function mountedAbove(element: HTMLElement | null, gapPx?: number) {
  const initialProps: HookProps = { elementRef: { current: element }, gapPx }
  return renderHook(
    ({ elementRef, gapPx: gap }: HookProps) => useToastsAboveElement(elementRef, gap),
    { initialProps },
  )
}

describe('useToastsAboveElement', () => {
  let observers: FakeResizeObservers

  beforeEach(() => {
    observers = installFakeResizeObservers()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    clearToastLiftOnPage()
  })

  it('lifts the toasts by the height of the element plus the default gap of 12px', () => {
    const { element } = elementWithHeight(100)

    mountedAbove(element)

    expect(toastLiftOnPage()).toBe('112px')
  })

  it('lifts the toasts by the height of the element plus the gap that was given', () => {
    const { element } = elementWithHeight(100)

    mountedAbove(element, 20)

    expect(toastLiftOnPage()).toBe('120px')
  })

  it('keeps only the gap when the element has no height', () => {
    const { element } = elementWithHeight(0)

    mountedAbove(element)

    expect(toastLiftOnPage()).toBe('12px')
  })

  it('keeps a fractional height as it is', () => {
    const { element } = elementWithHeight(100.5)

    mountedAbove(element)

    expect(toastLiftOnPage()).toBe('112.5px')
  })

  it('sets nothing while the ref holds no element', () => {
    mountedAbove(null)

    expect(toastLiftOnPage()).toBe('')
  })

  it('watches the size of the element', () => {
    const { element } = elementWithHeight(100)

    mountedAbove(element)

    expect(observers.observedElements()).toEqual([element])
  })

  it('follows the element when it grows', () => {
    const { element, setHeight } = elementWithHeight(100)
    mountedAbove(element)

    setHeight(160)
    observers.fireFor(element)

    expect(toastLiftOnPage()).toBe('172px')
  })

  it('follows the element when it shrinks', () => {
    const { element, setHeight } = elementWithHeight(160)
    mountedAbove(element)

    setHeight(60)
    observers.fireFor(element)

    expect(toastLiftOnPage()).toBe('72px')
  })

  it('removes the lift and stops watching when the component unmounts', () => {
    const { element } = elementWithHeight(100)
    const { unmount } = mountedAbove(element)

    unmount()

    expect(toastLiftOnPage()).toBe('')
    expect(observers.disconnectedCount()).toBe(1)
  })

  it('does not follow the element any more after the component unmounted', () => {
    const { element, setHeight } = elementWithHeight(100)
    const { unmount } = mountedAbove(element)
    unmount()

    setHeight(160)
    observers.fireFor(element)

    expect(toastLiftOnPage()).toBe('')
  })

  it('removes the lift when the ref stops holding an element', () => {
    const { element } = elementWithHeight(100)
    const { rerender } = mountedAbove(element)

    rerender({ elementRef: { current: null }, gapPx: undefined })

    expect(toastLiftOnPage()).toBe('')
  })

  it('measures the new element when the ref holds another element', () => {
    const { element } = elementWithHeight(100)
    const { element: taller } = elementWithHeight(300)
    const { rerender } = mountedAbove(element)

    rerender({ elementRef: { current: taller }, gapPx: undefined })

    expect(toastLiftOnPage()).toBe('312px')
  })

  it('uses the new gap when the gap changes', () => {
    const { element } = elementWithHeight(100)
    const elementRef = { current: element }
    const { rerender } = mountedAbove(element, 12)

    rerender({ elementRef, gapPx: 30 })

    expect(toastLiftOnPage()).toBe('130px')
  })

  describe('with two elements mounted at the same time', () => {
    it('shows the lift of the element that was mounted last', () => {
      const { element: first } = elementWithHeight(100)
      const { element: second } = elementWithHeight(50)

      mountedAbove(first)
      mountedAbove(second)

      expect(toastLiftOnPage()).toBe('62px')
    })

    it('does not remove the lift of the later element when the earlier one unmounts', () => {
      const { element: first } = elementWithHeight(100)
      const { element: second } = elementWithHeight(50)
      const earlier = mountedAbove(first)
      mountedAbove(second)

      earlier.unmount()

      expect(toastLiftOnPage()).toBe('62px')
    })

    it('removes the lift when the element that owns it unmounts', () => {
      const { element: first } = elementWithHeight(100)
      const { element: second } = elementWithHeight(50)
      const earlier = mountedAbove(first)
      const later = mountedAbove(second)

      earlier.unmount()
      later.unmount()

      expect(toastLiftOnPage()).toBe('')
    })
  })
})
