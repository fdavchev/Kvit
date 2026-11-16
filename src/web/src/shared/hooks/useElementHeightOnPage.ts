import { useEffect, type RefObject } from 'react'

const heightWritersOfProperty = new Map<string, Array<() => void>>()

export function useElementHeightOnPage(ref: RefObject<HTMLElement | null>, property: string, gapPx: number): void {
  useEffect(() => {
    const element = ref.current
    if (element === null) {
      return
    }

    const pageStyle = document.documentElement.style

    const writeHeight = (): void => {
      pageStyle.setProperty(property, `${element.getBoundingClientRect().height + gapPx}px`)
    }

    const writers = heightWritersOfProperty.get(property) ?? []
    writers.push(writeHeight)
    heightWritersOfProperty.set(property, writers)
    writeHeight()

    const isLatestWriter = (): boolean => writers.at(-1) === writeHeight

    const observer = new ResizeObserver(() => {
      if (isLatestWriter()) {
        writeHeight()
      }
    })
    observer.observe(element)

    return () => {
      observer.disconnect()
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
  }, [ref, property, gapPx])
}
