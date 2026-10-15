import { cleanup } from '@testing-library/react'
import { afterEach, vi } from 'vitest'
import { makeIntlLikeChromeWithoutMacedonian } from './chromeWithoutMacedonian'

makeIntlLikeChromeWithoutMacedonian()

class ResizeObserverWithoutLayout implements ResizeObserver {
  observe(): void {}
  unobserve(): void {}
  disconnect(): void {}
}

globalThis.ResizeObserver = ResizeObserverWithoutLayout

afterEach(() => {
  cleanup()
  vi.unstubAllEnvs()
})
