import { useEffect, type RefObject } from 'react'

const heightWritersOfProperty = new Map<string, Array<() => void>>()

type Measure = (element: HTMLElement) => number

function heightOf(element: HTMLElement): number {
  return element.getBoundingClientRect().height
}

function spaceFromTopToWindowBottomOf(element: HTMLElement): number {
  return window.innerHeight - element.getBoundingClientRect().top
}

export function useElementHeightOnPage(ref: RefObject<HTMLElement | null>, property: string, gapPx: number): void {
  useMeasureOnPage(ref, property, gapPx, heightOf)
}

export function useSpaceFromElementTopOnPage(ref: RefObject<HTMLElement | null>, property: string, gapPx: number): void {
  useMeasureOnPage(ref, property, gapPx, spaceFromTopToWindowBottomOf)
}

function useMeasureOnPage(ref: RefObject<HTMLElement | null>, property: string, gapPx: number, measure: Measure): void {
  useEffect(() => {
    const element = ref.current
    if (element === null) {
      return
    }

    const pageStyle = document.documentElement.style

    const writeHeight = (): void => {
      pageStyle.setProperty(property, `${measure(element) + gapPx}px`)
    }

    const writers = heightWritersOfProperty.get(property) ?? []
    writers.push(writeHeight)
    heightWritersOfProperty.set(property, writers)
    writeHeight()

    const isLatestWriter = (): boolean => writers.at(-1) === writeHeight

    const writeIfLatest = (): void => {
      if (isLatestWriter()) {
        writeHeight()
      }
    }
    const observer = new ResizeObserver(writeIfLatest)
    observer.observe(element)
    window.addEventListener('resize', writeIfLatest)

    return () => {
      observer.disconnect()
      window.removeEventListener('resize', writeIfLatest)
      const wasLatestWriter = isLatestWriter()
      writers.splice(writers.indexOf(writeHeight), 1)
      const previousWriter = writers.at(-1)
      if (previousWriter === undefined) {
        heightWritersOfProperty.delete(property)
        pageStyle.removeProperty(property)
        return
      }
      if (wasLatestWriter) {
        previousWriter()
      }
    }
  }, [ref, property, gapPx, measure])
}
