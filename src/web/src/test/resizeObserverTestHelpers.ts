import { vi } from 'vitest'

export interface FakeResizeObservers {
  observedElements: () => Element[]
  disconnectedCount: () => number
  fireFor: (element: Element) => void
}

const contentHeightThatIsNotTheBorderBoxHeight = 1

export function installFakeResizeObservers(): FakeResizeObservers {
  const created: FakeResizeObserver[] = []

  class FakeResizeObserver implements ResizeObserver {
    readonly targets = new Set<Element>()
    isDisconnected = false

    private readonly callback: ResizeObserverCallback

    constructor(callback: ResizeObserverCallback) {
      this.callback = callback
      created.push(this)
    }

    observe(target: Element): void {
      this.targets.add(target)
    }

    unobserve(target: Element): void {
      this.targets.delete(target)
    }

    disconnect(): void {
      this.targets.clear()
      this.isDisconnected = true
    }

    notify(target: Element): void {
      const entry = {
        target,
        contentRect: { height: contentHeightThatIsNotTheBorderBoxHeight },
      } as unknown as ResizeObserverEntry
      this.callback([entry], this)
    }
  }

  vi.stubGlobal('ResizeObserver', FakeResizeObserver)

  return {
    observedElements: () => created.flatMap((observer) => [...observer.targets]),
    disconnectedCount: () => created.filter((observer) => observer.isDisconnected).length,
    fireFor: (element) => {
      created
        .filter((observer) => observer.targets.has(element))
        .forEach((observer) => observer.notify(element))
    },
  }
}
