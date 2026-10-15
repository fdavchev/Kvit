import { useEffect, type RefObject } from 'react'

const toastLiftProperty = '--toast-lift'

let liftOwner: object | null = null

export function useToastsAboveElement(ref: RefObject<HTMLElement | null>, gapPx: number = 12): void {
  useEffect(() => {
    const element = ref.current
    if (element === null) {
      return
    }

    const owner = {}
    const pageStyle = document.documentElement.style

    const writeLift = (): void => {
      pageStyle.setProperty(toastLiftProperty, `${element.getBoundingClientRect().height + gapPx}px`)
    }

    liftOwner = owner
    writeLift()

    const observer = new ResizeObserver(() => {
      if (liftOwner === owner) {
        writeLift()
      }
    })
    observer.observe(element)

    return () => {
      observer.disconnect()
      if (liftOwner === owner) {
        liftOwner = null
        pageStyle.removeProperty(toastLiftProperty)
      }
    }
  }, [ref, gapPx])
}
