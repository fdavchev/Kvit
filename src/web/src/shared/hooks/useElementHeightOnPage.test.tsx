import { renderHook } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { installFakeResizeObservers, type FakeResizeObservers } from '@/test/resizeObserverTestHelpers'
import { elementWithHeight } from '@/test/toastLiftTestHelpers'
import { useElementHeightOnPage } from './useElementHeightOnPage'

const firstProperty = '--first-space'
const secondProperty = '--second-space'

function propertyOnPage(property: string): string {
  return document.documentElement.style.getPropertyValue(property)
}

function mountedOnPage(element: HTMLElement, property: string, gapPx: number) {
  return renderHook(() => useElementHeightOnPage({ current: element }, property, gapPx))
}

describe('useElementHeightOnPage', () => {
  let observers: FakeResizeObservers

  beforeEach(() => {
    observers = installFakeResizeObservers()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    document.documentElement.style.removeProperty(firstProperty)
    document.documentElement.style.removeProperty(secondProperty)
  })

  it('writes the height of the element plus the gap to the property it is given', () => {
    const { element } = elementWithHeight(54)

    mountedOnPage(element, firstProperty, 10)

    expect(propertyOnPage(firstProperty)).toBe('64px')
  })

  it('keeps two properties apart, so removing one leaves the other in place', () => {
    const { element: first } = elementWithHeight(54)
    const { element: second } = elementWithHeight(100)
    const firstMount = mountedOnPage(first, firstProperty, 10)
    mountedOnPage(second, secondProperty, 12)

    firstMount.unmount()

    expect(propertyOnPage(firstProperty)).toBe('')
    expect(propertyOnPage(secondProperty)).toBe('112px')
  })

  describe('with two writers for the same property', () => {
    function twoMounted() {
      const { element: first } = elementWithHeight(100)
      const { element: second, setHeight: setSecondHeight } = elementWithHeight(50)
      const firstMount = mountedOnPage(first, firstProperty, 10)
      const secondMount = mountedOnPage(second, firstProperty, 10)
      return { first, second, setSecondHeight, firstMount, secondMount }
    }

    it('writes the height of the latest writer', () => {
      twoMounted()

      expect(propertyOnPage(firstProperty)).toBe('60px')
    })

    it('hands the property back to the earlier writer when the latest one closes', () => {
      const { secondMount } = twoMounted()

      secondMount.unmount()

      expect(propertyOnPage(firstProperty)).toBe('110px')
    })

    it('removes the property when the last writer closes after the latest one', () => {
      const { firstMount, secondMount } = twoMounted()

      secondMount.unmount()
      firstMount.unmount()

      expect(propertyOnPage(firstProperty)).toBe('')
    })

    it('keeps the latest height when the earlier writer closes first, and removes it when the latest closes', () => {
      const { firstMount, secondMount } = twoMounted()

      firstMount.unmount()
      expect(propertyOnPage(firstProperty)).toBe('60px')
      secondMount.unmount()

      expect(propertyOnPage(firstProperty)).toBe('')
    })

    it('follows the size of the latest writer and ignores the size of the earlier one', () => {
      const { first, second, setSecondHeight } = twoMounted()
      setSecondHeight(80)

      observers.fireFor(first)
      expect(propertyOnPage(firstProperty)).toBe('60px')
      observers.fireFor(second)

      expect(propertyOnPage(firstProperty)).toBe('90px')
    })

    it('measures the earlier writer again when the latest one closes, not the height it had when it opened', () => {
      const { element: first, setHeight: setFirstHeight } = elementWithHeight(100)
      const { element: second } = elementWithHeight(50)
      mountedOnPage(first, firstProperty, 10)
      const secondMount = mountedOnPage(second, firstProperty, 10)
      setFirstHeight(300)

      secondMount.unmount()

      expect(propertyOnPage(firstProperty)).toBe('310px')
    })

    it('hands back to the writer that opened before the latest one when three are open and the latest closes', () => {
      const { element: first } = elementWithHeight(100)
      const { element: second } = elementWithHeight(200)
      const { element: third } = elementWithHeight(300)
      mountedOnPage(first, firstProperty, 0)
      mountedOnPage(second, firstProperty, 0)
      const thirdMount = mountedOnPage(third, firstProperty, 0)

      thirdMount.unmount()

      expect(propertyOnPage(firstProperty)).toBe('200px')
    })
  })
})
