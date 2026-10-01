import { fireEvent, screen, within } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { themeChoices, type ThemeChoice } from '@/core/theme/theme'
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

  it('does not change the pressed button or the page by itself', async () => {
    await renderSwitch('system')

    fireEvent.click(buttonFor('dark'))

    expect(buttonFor('system').getAttribute('aria-pressed')).toBe('true')
    expect(buttonFor('dark').getAttribute('aria-pressed')).toBe('false')
    expect(appliedTheme()).toBeUndefined()
  })
})
