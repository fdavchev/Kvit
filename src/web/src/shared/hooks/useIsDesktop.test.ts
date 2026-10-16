import { act, renderHook } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import indexCss from '@/index.css?raw'
import { stubDesktopScreen } from '@/test/matchMediaTestHelpers'
import { desktopQuery, useIsDesktop } from './useIsDesktop'

interface ChangingScreen {
  becomeDesktop: (isDesktop: boolean) => void
}

function stubScreenThatCanChange(isDesktopAtStart: boolean): ChangingScreen {
  let matches = isDesktopAtStart
  const changes = new EventTarget()
  vi.stubGlobal('matchMedia', (query: string): MediaQueryList => {
    if (query !== desktopQuery) {
      throw new Error(`This test screen only answers "${desktopQuery}", but matchMedia got "${query}"`)
    }
    return {
      matches,
      media: query,
      onchange: null,
      addListener: () => {},
      removeListener: () => {},
      addEventListener: changes.addEventListener.bind(changes),
      removeEventListener: changes.removeEventListener.bind(changes),
      dispatchEvent: changes.dispatchEvent.bind(changes),
    }
  })
  return {
    becomeDesktop: (isDesktop) => {
      matches = isDesktop
      changes.dispatchEvent(new Event('change'))
    },
  }
}

describe('useIsDesktop', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('asks for a window at least 64 rem wide, in rem and not in pixels', () => {
    expect(desktopQuery).toBe('(min-width: 64rem)')
  })

  it('uses the same width as the lg breakpoint of index.css, so the classes and the hook switch together', () => {
    const breakpoint = /--breakpoint-lg:\s*([^;]+);/.exec(indexCss)?.[1].trim()

    expect(breakpoint).toBe('64rem')
    expect(desktopQuery).toBe(`(min-width: ${breakpoint})`)
  })

  it('answers false on a phone-sized test screen', () => {
    const { result } = renderHook(() => useIsDesktop())

    expect(result.current).toBe(false)
  })

  it('answers true on a desktop-sized test screen', () => {
    stubDesktopScreen()

    const { result } = renderHook(() => useIsDesktop())

    expect(result.current).toBe(true)
  })

  it('follows the window when it grows past the breakpoint and shrinks back', () => {
    const screen = stubScreenThatCanChange(false)
    const { result } = renderHook(() => useIsDesktop())

    act(() => screen.becomeDesktop(true))
    expect(result.current).toBe(true)

    act(() => screen.becomeDesktop(false))
    expect(result.current).toBe(false)
  })
})
