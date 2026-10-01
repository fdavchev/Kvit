import { act } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import {
  appliedTheme,
  resetThemeDocument,
  themeColorOf,
  themeColors,
} from '@/test/themeTestHelpers'
import { themeChoices } from './theme'
import { useTheme } from './useTheme'

const storageKey = 'kvit.theme'

describe('useTheme', () => {
  beforeEach(() => {
    window.localStorage.clear()
    resetThemeDocument()
  })

  it('starts on system when nothing is saved', async () => {
    const { result } = await renderHookWithProviders(() => useTheme())

    expect(result.current.theme).toBe('system')
  })

  it.each(themeChoices)('starts on the saved choice %s', async (choice) => {
    window.localStorage.setItem(storageKey, choice)

    const { result } = await renderHookWithProviders(() => useTheme())

    expect(result.current.theme).toBe(choice)
  })

  it.each(themeChoices)('saves %s when it is chosen', async (choice) => {
    const { result } = await renderHookWithProviders(() => useTheme())

    act(() => {
      result.current.setTheme(choice)
    })

    expect(window.localStorage.getItem(storageKey)).toBe(choice)
  })

  it.each(['light', 'dark'] as const)('applies %s to the page and the theme-color tags when it is chosen', async (choice) => {
    const { result } = await renderHookWithProviders(() => useTheme())

    act(() => {
      result.current.setTheme(choice)
    })

    expect(appliedTheme()).toBe(choice)
    expect(themeColorOf('light')).toBe(themeColors[choice])
    expect(themeColorOf('dark')).toBe(themeColors[choice])
  })

  it('removes data-theme from the page when system is chosen after dark', async () => {
    const { result } = await renderHookWithProviders(() => useTheme())
    act(() => {
      result.current.setTheme('dark')
    })

    act(() => {
      result.current.setTheme('system')
    })

    expect(appliedTheme()).toBeUndefined()
  })

  it.each(themeChoices)('reports %s as the current theme once it is chosen', async (choice) => {
    window.localStorage.setItem(storageKey, choice === 'dark' ? 'light' : 'dark')
    const { result } = await renderHookWithProviders(() => useTheme())

    act(() => {
      result.current.setTheme(choice)
    })

    expect(result.current.theme).toBe(choice)
  })
})
