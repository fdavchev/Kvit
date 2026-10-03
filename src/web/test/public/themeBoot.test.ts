import { readFileSync } from 'node:fs'
import path from 'node:path'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  appliedTheme,
  resetThemeDocument,
  themeColorOf,
  themeColors,
} from '@/test/themeTestHelpers'

const themeBootSource = readFileSync(
  path.resolve(import.meta.dirname, '../../public/theme-boot.js'),
  'utf8',
)

function runThemeBoot(): void {
  new Function(themeBootSource)()
}

function stubSavedTheme(saved: string | null): void {
  vi.stubGlobal('localStorage', {
    getItem: (key: string): string | null => (key === 'kvit.theme' ? saved : null),
  })
}

describe('public/theme-boot.js', () => {
  beforeEach(() => {
    resetThemeDocument()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it.each(['light', 'dark'] as const)('applies the saved choice %s to the page and both theme-color tags', (choice) => {
    stubSavedTheme(choice)

    runThemeBoot()

    expect(appliedTheme()).toBe(choice)
    expect(themeColorOf('light')).toBe(themeColors[choice])
    expect(themeColorOf('dark')).toBe(themeColors[choice])
  })

  it.each([
    ['nothing is saved', null],
    ['the saved text is not a valid choice', 'purple'],
    ['the saved text is "system"', 'system'],
  ])('changes nothing when %s', (_name, saved) => {
    stubSavedTheme(saved)

    runThemeBoot()

    expect(appliedTheme()).toBeUndefined()
    expect(themeColorOf('light')).toBe(themeColors.light)
    expect(themeColorOf('dark')).toBe(themeColors.dark)
  })

  it('warns with console.warn naming kvit.theme and changes nothing when the storage cannot be read', () => {
    const consoleWarn = vi.spyOn(console, 'warn').mockImplementation(() => {})
    const failure = new Error('storage is blocked')
    vi.stubGlobal('localStorage', {
      getItem: (): string => {
        throw failure
      },
    })

    runThemeBoot()

    expect(consoleWarn).toHaveBeenCalledWith(expect.stringContaining('kvit.theme'), failure)
    expect(appliedTheme()).toBeUndefined()
    expect(themeColorOf('light')).toBe(themeColors.light)
    expect(themeColorOf('dark')).toBe(themeColors.dark)
  })

  it('does not warn when the storage can be read', () => {
    const consoleWarn = vi.spyOn(console, 'warn').mockImplementation(() => {})
    stubSavedTheme('dark')

    runThemeBoot()

    expect(consoleWarn).not.toHaveBeenCalled()
  })
})
