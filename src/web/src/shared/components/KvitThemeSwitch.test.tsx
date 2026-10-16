import { fireEvent, screen, within } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { themeChoices, type ThemeChoice } from '@/core/theme/theme'
import { colorOfToken, contrastBetween } from '@/test/indexCssColors'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { appliedTheme, themeButtonName, themeGroupName } from '@/test/themeTestHelpers'
import { KvitThemeSwitch } from './KvitThemeSwitch'

const otherChoice: Record<ThemeChoice, ThemeChoice> = {
  system: 'light',
  light: 'dark',
  dark: 'system',
}

async function renderSwitch(
  theme: ThemeChoice,
  options: { onChange?: (choice: ThemeChoice) => void; screenLanguage?: Language } = {},
) {
  return renderElementWithProviders(
    <KvitThemeSwitch theme={theme} onChange={options.onChange ?? vi.fn()} />,
    '/',
    { language: options.screenLanguage ?? 'en' },
  )
}

function buttonFor(choice: ThemeChoice): HTMLElement {
  return screen.getByRole('button', { name: themeButtonName('en', choice) })
}

describe('KvitThemeSwitch', () => {
  it.each(languages)('labels the group with the translated theme label (screen in %s)', async (screenLanguage) => {
    await renderSwitch('system', { screenLanguage })

    expect(screen.getByRole('group', { name: themeGroupName(screenLanguage) })).toBeTruthy()
  })

  it.each(languages)('shows one button per choice in the order same as device, light, dark (screen in %s)', async (screenLanguage) => {
    await renderSwitch('system', { screenLanguage })

    const group = screen.getByRole('group', { name: themeGroupName(screenLanguage) })

    expect(
      within(group).getAllByRole('button').map((button) => button.textContent),
    ).toEqual(themeChoices.map((choice) => themeButtonName(screenLanguage, choice)))
  })

  it.each(themeChoices)('marks only the button of the active choice as pressed (active %s)', async (active) => {
    await renderSwitch(active)

    for (const choice of themeChoices) {
      expect(buttonFor(choice).getAttribute('aria-pressed')).toBe(String(choice === active))
    }
  })

  it.each(themeChoices)('calls onChange with %s when its button is clicked', async (clicked) => {
    const onChange = vi.fn()
    await renderSwitch(otherChoice[clicked], { onChange })

    fireEvent.click(buttonFor(clicked))

    expect(onChange).toHaveBeenCalledExactlyOnceWith(clicked)
  })

  it('gives the pressed button the selected-tab pill and its text colour, the same as the bottom bar', async () => {
    await renderSwitch('dark')

    expect(buttonFor('dark').className).toContain('aria-pressed:bg-tab-active')
    expect(buttonFor('dark').className).toContain('aria-pressed:text-tab-active-foreground')
  })

  it('keeps the pressed button white with the usual text in light mode, as before', () => {
    expect(colorOfToken('tab-active', 'light')).toEqual(colorOfToken('track-active', 'light'))
    expect(colorOfToken('tab-active-foreground', 'light')).toEqual(colorOfToken('foreground', 'light'))
  })

  it('makes the dark pressed pill stand out from its track at 3:1 or more', () => {
    expect(contrastBetween('tab-active', 'track', 'dark')).toBeGreaterThanOrEqual(3)
  })

  it('does not change the pressed button or the page by itself', async () => {
    await renderSwitch('system')

    fireEvent.click(buttonFor('dark'))

    expect(buttonFor('system').getAttribute('aria-pressed')).toBe('true')
    expect(buttonFor('dark').getAttribute('aria-pressed')).toBe('false')
    expect(appliedTheme()).toBeUndefined()
  })
})
