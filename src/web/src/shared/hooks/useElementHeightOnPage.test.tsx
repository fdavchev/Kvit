import { renderHook } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { installFakeResizeObservers } from '@/test/resizeObserverTestHelpers'
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
  beforeEach(() => {
    installFakeResizeObservers()
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
})
