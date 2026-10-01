import { fireEvent, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { translated } from '@/test/translated'
import { KvitLanguageSwitch } from './KvitLanguageSwitch'

const otherLanguage: Record<Language, Language> = { en: 'mk', mk: 'en' }

async function renderSwitch(
  language: Language,
  options: { onChange?: (language: Language) => void; screenLanguage?: Language } = {},
) {
  return renderElementWithProviders(
    <KvitLanguageSwitch language={language} onChange={options.onChange ?? vi.fn()} />,
    '/',
    { language: options.screenLanguage ?? 'en' },
  )
}

function buttonFor(language: Language): HTMLElement {
  return screen.getByRole('button', { name: translated('en', `language.${language}`) })
}

describe('KvitLanguageSwitch', () => {
  it.each(languages)('labels the group with the translated language label (screen in %s)', async (screenLanguage) => {
    await renderSwitch('en', { screenLanguage })

    expect(
      screen.getByRole('group', { name: translated(screenLanguage, 'language.label') }),
    ).toBeTruthy()
  })

  it.each(languages)('marks the %s button with its own language code', async (language) => {
    await renderSwitch('en')

    expect(buttonFor(language).getAttribute('lang')).toBe(language)
  })

  it.each(languages)('marks only the button of the active language as pressed (active %s)', async (active) => {
    await renderSwitch(active)

    expect(buttonFor(active).getAttribute('aria-pressed')).toBe('true')
    expect(buttonFor(otherLanguage[active]).getAttribute('aria-pressed')).toBe('false')
  })

  it.each(languages)('calls onChange with %s when its button is clicked', async (clicked) => {
    const onChange = vi.fn()
    await renderSwitch(otherLanguage[clicked], { onChange })

    fireEvent.click(buttonFor(clicked))

    expect(onChange).toHaveBeenCalledExactlyOnceWith(clicked)
  })

  it('does not change the language of the screen or the pressed button by itself', async () => {
    const { i18n } = await renderSwitch('en')

    fireEvent.click(buttonFor('mk'))

    expect(i18n.language).toBe('en')
    expect(buttonFor('en').getAttribute('aria-pressed')).toBe('true')
  })
})
