import { fireEvent, screen, within } from '@testing-library/react'
import type { SubmitEvent } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { defaultGroupEmoji, groupEmojis } from '@/test/groupTestData'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { translated } from '@/test/translated'
import { GroupFields } from './GroupFields'

const nameInvalid = 'Enter a name (up to 60 characters).'

interface FieldsOptions {
  name?: string
  emoji?: string
  currency?: string
  nameInvalid?: string | null
  language?: Language
  onNameChange?: (name: string) => void
  onEmojiChange?: (emoji: string) => void
  onCurrencyChange?: (currency: string) => void
}

async function renderFields(options: FieldsOptions = {}) {
  return renderElementWithProviders(
    <GroupFields
      name={options.name ?? ''}
      emoji={options.emoji ?? defaultGroupEmoji}
      currency={options.currency ?? 'MKD'}
      nameInvalid={options.nameInvalid}
      onNameChange={options.onNameChange ?? vi.fn()}
      onEmojiChange={options.onEmojiChange ?? vi.fn()}
      onCurrencyChange={options.onCurrencyChange ?? vi.fn()}
    />,
    '/',
    { language: options.language ?? 'en' },
  )
}

function nameField(language: Language = 'en'): HTMLInputElement {
  return screen.getByLabelText<HTMLInputElement>(translated(language, 'groupFields.name'))
}

function emojiGroup(language: Language = 'en'): HTMLElement {
  return screen.getByRole('group', { name: translated(language, 'groupFields.emoji') })
}

function currencyGroup(language: Language = 'en'): HTMLElement {
  return screen.getByRole('group', { name: translated(language, 'groupFields.currency') })
}

describe('GroupFields', () => {
  it.each(languages)('has a name field with the translated label and placeholder (%s)', async (language) => {
    await renderFields({ language })

    expect(nameField(language).placeholder).toBe(translated(language, 'groupFields.namePlaceholder'))
  })

  it('shows the name it is given in the name field', async () => {
    await renderFields({ name: 'Greece trip' })

    expect(nameField().value).toBe('Greece trip')
  })

  it('calls onNameChange with the typed text', async () => {
    const onNameChange = vi.fn<(name: string) => void>()
    await renderFields({ onNameChange })

    fireEvent.change(nameField(), { target: { value: 'Flat 4B' } })

    expect(onNameChange).toHaveBeenCalledExactlyOnceWith('Flat 4B')
  })

  it.each(languages)('has the six suggested emojis in a group named by the translated label (%s)', async (language) => {
    await renderFields({ language })

    const chips = within(emojiGroup(language)).getAllByRole('button')

    expect(chips.map((chip) => chip.textContent)).toEqual(groupEmojis)
  })

  it.each(groupEmojis)('marks only the emoji %s as pressed when it is the chosen one', async (chosen) => {
    await renderFields({ emoji: chosen })

    for (const emoji of groupEmojis) {
      expect(
        within(emojiGroup()).getByRole('button', { name: emoji }).getAttribute('aria-pressed'),
      ).toBe(String(emoji === chosen))
    }
  })

  it('calls onEmojiChange with the emoji that is pressed', async () => {
    const onEmojiChange = vi.fn<(emoji: string) => void>()
    await renderFields({ onEmojiChange })

    fireEvent.click(within(emojiGroup()).getByRole('button', { name: groupEmojis[5] }))

    expect(onEmojiChange).toHaveBeenCalledExactlyOnceWith(groupEmojis[5])
  })

  it.each(languages)('has the MKD and EUR chips in a group named by the translated label (%s)', async (language) => {
    await renderFields({ language })

    const chips = within(currencyGroup(language)).getAllByRole('button')

    expect(chips.map((chip) => chip.textContent)).toEqual(['MKD', 'EUR'])
  })

  it.each([
    ['MKD', 'EUR'],
    ['EUR', 'MKD'],
  ])('marks only the currency %s as pressed when it is the chosen one', async (chosen, other) => {
    await renderFields({ currency: chosen })

    expect(
      within(currencyGroup()).getByRole('button', { name: chosen }).getAttribute('aria-pressed'),
    ).toBe('true')
    expect(
      within(currencyGroup()).getByRole('button', { name: other }).getAttribute('aria-pressed'),
    ).toBe('false')
  })

  it('calls onCurrencyChange with the currency that is pressed', async () => {
    const onCurrencyChange = vi.fn<(currency: string) => void>()
    await renderFields({ onCurrencyChange })

    fireEvent.click(within(currencyGroup()).getByRole('button', { name: 'EUR' }))

    expect(onCurrencyChange).toHaveBeenCalledExactlyOnceWith('EUR')
  })

  it.each(languages)('explains which currency new expenses use (%s)', async (language) => {
    await renderFields({ language })

    expect(screen.getByText(translated(language, 'groupFields.currencyHint'))).toBeTruthy()
  })

  it('shows the message and marks the name field invalid when nameInvalid is given', async () => {
    await renderFields({ nameInvalid })

    expect(screen.getByText(nameInvalid)).toBeTruthy()
    expect(nameField().getAttribute('aria-invalid')).toBe('true')
  })

  it.each([
    ['undefined', undefined],
    ['null', null],
  ])('shows no message and does not mark the name field invalid when nameInvalid is %s', async (_name, value) => {
    await renderFields({ nameInvalid: value })

    expect(screen.queryByText(nameInvalid)).toBeNull()
    expect(nameField().getAttribute('aria-invalid')).not.toBe('true')
  })

  it('does not submit a surrounding form when a chip is pressed', async () => {
    const onSubmit = vi.fn<(event: SubmitEvent<HTMLFormElement>) => void>((event) => {
      event.preventDefault()
    })
    await renderElementWithProviders(
      <form onSubmit={onSubmit}>
        <GroupFields
          name=""
          emoji={defaultGroupEmoji}
          currency="MKD"
          onNameChange={vi.fn()}
          onEmojiChange={vi.fn()}
          onCurrencyChange={vi.fn()}
        />
      </form>,
      '/',
    )

    fireEvent.click(within(emojiGroup()).getByRole('button', { name: groupEmojis[1] }))
    fireEvent.click(within(currencyGroup()).getByRole('button', { name: 'EUR' }))

    expect(onSubmit).not.toHaveBeenCalled()
  })
})
