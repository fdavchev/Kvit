import { vi } from 'vitest'
import { desktopQuery } from '@/shared/hooks/useIsDesktop'

export const reducedMotionQuery = '(prefers-reduced-motion: reduce)'

export type DeviceMotion = 'reduced' | 'allowed'

export function fakeMatchMedia(answers: Readonly<Record<string, boolean>>): (query: string) => MediaQueryList {
  return (query) => {
    const matches = answers[query]
    if (matches === undefined) {
      const known = Object.keys(answers)
        .map((answered) => `"${answered}"`)
        .join(' and ')
      throw new Error(`The test device only answers ${known}, but matchMedia got "${query}"`)
    }
    return {
      matches,
      media: query,
      onchange: null,
      addListener: () => {},
      removeListener: () => {},
      addEventListener: () => {},
      removeEventListener: () => {},
      dispatchEvent: () => false,
    }
  }
}

export function stubDeviceMotion(motion: DeviceMotion): void {
  vi.stubGlobal('matchMedia', fakeMatchMedia({ [reducedMotionQuery]: motion === 'reduced', [desktopQuery]: false }))
}

export function stubDesktopScreen(): void {
  vi.stubGlobal('matchMedia', fakeMatchMedia({ [reducedMotionQuery]: true, [desktopQuery]: true }))
}
