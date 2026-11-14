import { useEffect, type RefObject } from 'react'

const ownerOfProperty = new Map<string, object>()

export function useElementHeightOnPage(ref: RefObject<HTMLElement | null>, property: string, gapPx: number): void {
  useEffect(() => {
    const element = ref.current
    if (element === null) {
      return
    }

    const owner = {}
    const pageStyle = document.documentElement.style

    const writeHeight = (): void => {
      pageStyle.setProperty(property, `${element.getBoundingClientRect().height + gapPx}px`)
    }

    ownerOfProperty.set(property, owner)
    writeHeight()

    const observer = new ResizeObserver(() => {
      if (ownerOfProperty.get(property) === owner) {
        writeHeight()
      }
    })
    observer.observe(element)

    return () => {
      observer.disconnect()
      if (ownerOfProperty.get(property) === owner) {
        ownerOfProperty.delete(property)
        pageStyle.removeProperty(property)
      }
    }
  }, [ref, property, gapPx])
}
