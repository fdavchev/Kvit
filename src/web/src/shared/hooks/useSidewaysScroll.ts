import { useEffect, type RefObject } from 'react'

const pixelsPerWheelLine = 16
const roundingSlackPx = 2

function wheelDistanceInPixels(event: WheelEvent, strip: HTMLElement): number {
  if (event.deltaMode === WheelEvent.DOM_DELTA_LINE) {
    return event.deltaY * pixelsPerWheelLine
  }
  if (event.deltaMode === WheelEvent.DOM_DELTA_PAGE) {
    return event.deltaY * strip.clientWidth
  }
  return event.deltaY
}

export function useSidewaysScroll(stripRef: RefObject<HTMLElement | null>): void {
  useEffect(() => {
    const strip = stripRef.current
    if (strip === null) {
      return
    }

    const lastScrollLeft = (): number => strip.scrollWidth - strip.clientWidth
    const hasMoreBefore = (): boolean => strip.scrollLeft > roundingSlackPx
    const hasMoreAfter = (): boolean => strip.scrollLeft < lastScrollLeft() - roundingSlackPx

    const markEdges = (): void => {
      strip.toggleAttribute('data-more-before', hasMoreBefore())
      strip.toggleAttribute('data-more-after', hasMoreAfter())
    }

    const scrollSidewaysWithWheel = (event: WheelEvent): void => {
      if (event.ctrlKey || Math.abs(event.deltaY) <= Math.abs(event.deltaX)) {
        return
      }
      const canMoveInWheelDirection = event.deltaY > 0 ? hasMoreAfter() : hasMoreBefore()
      if (!canMoveInWheelDirection) {
        return
      }
      event.preventDefault()
      strip.scrollLeft = Math.min(lastScrollLeft(), Math.max(0, strip.scrollLeft + wheelDistanceInPixels(event, strip)))
      markEdges()
    }

    markEdges()
    strip.addEventListener('scroll', markEdges, { passive: true })
    strip.addEventListener('wheel', scrollSidewaysWithWheel, { passive: false })
    const observer = new ResizeObserver(markEdges)
    observer.observe(strip)

    return () => {
      strip.removeEventListener('scroll', markEdges)
      strip.removeEventListener('wheel', scrollSidewaysWithWheel)
      observer.disconnect()
    }
  }, [stripRef])
}
