import { cleanup } from '@testing-library/react'
import { afterEach, vi } from 'vitest'
import { makeIntlLikeChromeWithoutMacedonian } from './chromeWithoutMacedonian'
import { desktopQuery } from '@/shared/hooks/useIsDesktop'
import { fakeMatchMedia, reducedMotionQuery } from './matchMediaTestHelpers'

vi.mock('esm-env', async (importOriginal) => ({ ...(await importOriginal<typeof import('esm-env')>()), BROWSER: true }))

makeIntlLikeChromeWithoutMacedonian()

class ResizeObserverWithoutLayout implements ResizeObserver {
  observe(): void {}
  unobserve(): void {}
  disconnect(): void {}
}

globalThis.ResizeObserver = ResizeObserverWithoutLayout

window.matchMedia = fakeMatchMedia({ [reducedMotionQuery]: true, [desktopQuery]: false })

afterEach(() => {
  cleanup()
  vi.unstubAllEnvs()
})
