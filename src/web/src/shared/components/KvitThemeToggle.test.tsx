import { fireEvent, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { Language } from '@/core/i18n/language'
import type { ThemeChoice } from '@/core/theme/theme'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import {
  appliedTheme,
  resetThemeDocument,
  stubDeviceColorScheme,
  type ColorScheme,
} from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { KvitThemeToggle } from './KvitThemeToggle'

const storageKey = 'kvit.theme'

type ToggleTextKey = 'common.switchToDarkMode' | 'common.switchToLightMode'

const macedonianTexts: Record<ToggleTextKey, string> = {
  'common.switchToDarkMode': 'Префрли на темен режим',
  'common.switchToLightMode': 'Префрли на светол режим',
}

async function renderToggle(
  saved: ThemeChoice,
  options: { device?: ColorScheme; screenLanguage?: Language } = {},
): Promise<void> {
  window.localStorage.setItem(storageKey, saved)
  stubDeviceColorScheme(options.device ?? 'light')
  await renderElementWithProviders(<KvitThemeToggle />, '/', {
    language: options.screenLanguage ?? 'en',
  })
}

function toggleNamed(key: ToggleTextKey, language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, key) })
}

describe('KvitThemeToggle', () => {
  beforeEach(() => {
    window.localStorage.clear()
    resetThemeDocument()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each<[ThemeChoice, ColorScheme, ToggleTextKey, ThemeChoice]>([
    ['light', 'light', 'common.switchToDarkMode', 'dark'],
    ['light', 'dark', 'common.switchToDarkMode', 'dark'],
    ['dark', 'dark', 'common.switchToLightMode', 'light'],
    ['dark', 'light', 'common.switchToLightMode', 'light'],
    ['system', 'dark', 'common.switchToLightMode', 'light'],
    ['system', 'light', 'common.switchToDarkMode', 'dark'],
  ])(
    'saved %s on a %s device is named %s, and a tap saves and applies %s',
    async (saved, device, key, flipped) => {
      await renderToggle(saved, { device })

      fireEvent.click(toggleNamed(key))

      expect(window.localStorage.getItem(storageKey)).toBe(flipped)
      expect(appliedTheme()).toBe(flipped)
    },
  )

  it('renames itself after each tap and flips back to light on the second tap', async () => {
    await renderToggle('light')

    fireEvent.click(toggleNamed('common.switchToDarkMode'))
    fireEvent.click(toggleNamed('common.switchToLightMode'))

    expect(window.localStorage.getItem(storageKey)).toBe('light')
    expect(appliedTheme()).toBe('light')
    expect(toggleNamed('common.switchToDarkMode')).toBeTruthy()
  })

  it.each(Object.entries(macedonianTexts))('has the Macedonian text for %s in mk.json', (key, text) => {
    expect(translated('mk', key)).toBe(text)
  })

  it.each<[ThemeChoice, ToggleTextKey]>([
    ['light', 'common.switchToDarkMode'],
    ['dark', 'common.switchToLightMode'],
  ])('is named in Macedonian when the screen is in mk and the saved choice is %s', async (saved, key) => {
    await renderToggle(saved, { screenLanguage: 'mk' })

    expect(toggleNamed(key, 'mk')).toBeTruthy()
  })

  it('is a button of type button so it never submits a form', async () => {
    await renderToggle('light')

    expect(toggleNamed('common.switchToDarkMode').getAttribute('type')).toBe('button')
  })

  it('has no aria-pressed because its name changes instead', async () => {
    await renderToggle('dark')

    expect(toggleNamed('common.switchToLightMode').hasAttribute('aria-pressed')).toBe(false)
  })
})
