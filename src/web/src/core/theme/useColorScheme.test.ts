import { act, renderHook } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { stubDeviceColorScheme, stubDeviceColorSchemeThatCanChange } from '@/test/themeTestHelpers'
import type { ThemeChoice } from './theme'
import { useColorScheme } from './useColorScheme'

describe('useColorScheme', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(['light', 'dark'] as const)('answers the scheme of the device when the choice is system and the device is %s', (device) => {
    stubDeviceColorScheme(device)

    const { result } = renderHook(() => useColorScheme('system'))

    expect(result.current).toBe(device)
  })

  it.each(['light', 'dark'] as const)('answers %s when that is chosen, whatever the device is', (choice) => {
    stubDeviceColorScheme(choice === 'light' ? 'dark' : 'light')

    const { result } = renderHook(() => useColorScheme(choice))

    expect(result.current).toBe(choice)
  })

  it('follows the device when it changes to dark and back to light while mounted, with the choice system', () => {
    const device = stubDeviceColorSchemeThatCanChange('light')
    const { result } = renderHook(() => useColorScheme('system'))

    act(() => device.changeTo('dark'))
    expect(result.current).toBe('dark')

    act(() => device.changeTo('light'))
    expect(result.current).toBe('light')
  })

  it.each(['light', 'dark'] as const)('ignores a change of the device when %s is chosen', (choice) => {
    const device = stubDeviceColorSchemeThatCanChange(choice === 'light' ? 'dark' : 'light')
    const { result } = renderHook(() => useColorScheme(choice))

    act(() => device.changeTo(choice))
    act(() => device.changeTo(choice === 'light' ? 'dark' : 'light'))

    expect(result.current).toBe(choice)
  })

  it('switches from the device to the chosen scheme when the choice changes from system to a fixed one', () => {
    stubDeviceColorScheme('dark')
    const { result, rerender } = renderHook(({ choice }: { choice: ThemeChoice }) => useColorScheme(choice), {
      initialProps: { choice: 'system' },
    })
    expect(result.current).toBe('dark')

    rerender({ choice: 'light' })

    expect(result.current).toBe('light')
  })

  it('stops listening to the device when it is unmounted', () => {
    const device = stubDeviceColorSchemeThatCanChange('light')
    const { unmount } = renderHook(() => useColorScheme('system'))
    expect(device.listenerCount()).toBe(1)

    unmount()

    expect(device.listenerCount()).toBe(0)
  })
})
