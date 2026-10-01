import { beforeEach, describe, expect, it } from 'vitest'
import {
  appliedTheme,
  resetThemeDocument,
  themeColorOf,
  themeColors,
  themeColorTagCount,
} from '@/test/themeTestHelpers'
import { applyTheme } from './applyTheme'

describe('applyTheme', () => {
  beforeEach(() => {
    resetThemeDocument()
  })

  it.each(['light', 'dark'] as const)('sets data-theme to %s on the page', (choice) => {
    applyTheme(choice)

    expect(appliedTheme()).toBe(choice)
  })

  it.each(['light', 'dark'] as const)('colours both theme-color tags with the %s colour', (choice) => {
    applyTheme(choice)

    expect(themeColorOf('light')).toBe(themeColors[choice])
    expect(themeColorOf('dark')).toBe(themeColors[choice])
  })

  it('removes data-theme when the choice goes back to system', () => {
    applyTheme('dark')

    applyTheme('system')

    expect(appliedTheme()).toBeUndefined()
  })

  it.each(['light', 'dark'] as const)('restores each theme-color tag to its own colour when %s goes back to system', (previous) => {
    applyTheme(previous)

    applyTheme('system')

    expect(themeColorOf('light')).toBe(themeColors.light)
    expect(themeColorOf('dark')).toBe(themeColors.dark)
  })

  it('leaves a page that was never themed without data-theme when system is applied', () => {
    applyTheme('system')

    expect(appliedTheme()).toBeUndefined()
    expect(themeColorOf('light')).toBe(themeColors.light)
    expect(themeColorOf('dark')).toBe(themeColors.dark)
  })

  it('follows a switch from light to system to dark', () => {
    applyTheme('light')
    expect(appliedTheme()).toBe('light')
    expect(themeColorOf('dark')).toBe(themeColors.light)

    applyTheme('system')
    expect(appliedTheme()).toBeUndefined()
    expect(themeColorOf('dark')).toBe(themeColors.dark)

    applyTheme('dark')
    expect(appliedTheme()).toBe('dark')
    expect(themeColorOf('light')).toBe(themeColors.dark)
  })

  it.each(['system', 'light', 'dark'] as const)('is harmless when %s is applied twice: same state and still two theme-color tags', (choice) => {
    applyTheme(choice)
    const themeAfterFirst = appliedTheme()
    const lightColorAfterFirst = themeColorOf('light')
    const darkColorAfterFirst = themeColorOf('dark')

    applyTheme(choice)

    expect(appliedTheme()).toBe(themeAfterFirst)
    expect(themeColorOf('light')).toBe(lightColorAfterFirst)
    expect(themeColorOf('dark')).toBe(darkColorAfterFirst)
    expect(themeColorTagCount()).toBe(2)
  })
})
