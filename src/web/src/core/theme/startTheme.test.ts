import { beforeEach, describe, expect, it } from 'vitest'
import {
  appliedTheme,
  resetThemeDocument,
  themeColorOf,
  themeColors,
} from '@/test/themeTestHelpers'
import { startTheme } from './startTheme'

describe('startTheme', () => {
  beforeEach(() => {
    window.localStorage.clear()
    resetThemeDocument()
  })

  it.each(['light', 'dark'] as const)('applies the saved choice %s to the page and both theme-color tags', (choice) => {
    window.localStorage.setItem('kvit.theme', choice)

    startTheme()

    expect(appliedTheme()).toBe(choice)
    expect(themeColorOf('light')).toBe(themeColors[choice])
    expect(themeColorOf('dark')).toBe(themeColors[choice])
  })

  it('leaves the page on system when nothing is saved', () => {
    startTheme()

    expect(appliedTheme()).toBeUndefined()
    expect(themeColorOf('light')).toBe(themeColors.light)
    expect(themeColorOf('dark')).toBe(themeColors.dark)
  })

  it('leaves the page on system when the saved text is not a valid choice', () => {
    window.localStorage.setItem('kvit.theme', 'purple')

    startTheme()

    expect(appliedTheme()).toBeUndefined()
  })
})
