const toastLiftProperty = '--toast-lift'

export function toastLiftOnPage(): string {
  return document.documentElement.style.getPropertyValue(toastLiftProperty)
}

export function clearToastLiftOnPage(): void {
  document.documentElement.style.removeProperty(toastLiftProperty)
}

export function rectOfHeight(height: number): DOMRect {
  return DOMRect.fromRect({ x: 0, y: 0, width: 320, height })
}

export interface ElementWithHeight {
  element: HTMLElement
  setHeight: (height: number) => void
}

export function elementWithHeight(initialHeight: number): ElementWithHeight {
  const element = document.createElement('div')
  let height = initialHeight
  element.getBoundingClientRect = () => rectOfHeight(height)
  return {
    element,
    setHeight: (next) => {
      height = next
    },
  }
}
