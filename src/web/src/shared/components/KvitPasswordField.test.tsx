import { cleanup, fireEvent, screen } from '@testing-library/react'
import type { ComponentProps, FormEvent } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { fieldLabelled } from '@/test/formTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { translated } from '@/test/translated'
import { KvitPasswordField } from './KvitPasswordField'

const label = 'Password'

async function renderField(
  props: Partial<ComponentProps<typeof KvitPasswordField>> = {},
  language: Language = 'en',
): Promise<void> {
  await renderElementWithProviders(
    <KvitPasswordField
      id="password-field"
      label={label}
      value="Secret1pw"
      onChange={vi.fn()}
      autoComplete="current-password"
      enterKeyHint="done"
      {...props}
    />,
    '/',
    { language },
  )
}

function showButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'common.showPassword') })
}

function hideButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'common.hidePassword') })
}

describe('KvitPasswordField', () => {
  it('starts as a hidden password input', async () => {
    await renderField()

    expect(fieldLabelled(label).type).toBe('password')
  })

  it.each(languages)('has a button named with the translated show text while hidden (%s)', async (language) => {
    await renderField({}, language)

    expect(showButton(language).getAttribute('aria-pressed')).toBe('false')
  })

  it('has a plain button that cannot submit a form', async () => {
    await renderField()

    expect(showButton().getAttribute('type')).toBe('button')
  })

  it('shows the password as text when the button is pressed', async () => {
    await renderField()

    fireEvent.click(showButton())

    expect(fieldLabelled(label).type).toBe('text')
  })

  it('renames the button to the hide text and marks it pressed while the password is shown', async () => {
    await renderField()

    fireEvent.click(showButton())

    expect(hideButton().getAttribute('aria-pressed')).toBe('true')
    expect(screen.queryByRole('button', { name: translated('en', 'common.showPassword') })).toBeNull()
  })

  it('hides the password again when the button is pressed a second time', async () => {
    await renderField()

    fireEvent.click(showButton())
    fireEvent.click(hideButton())

    expect(fieldLabelled(label).type).toBe('password')
    expect(showButton().getAttribute('aria-pressed')).toBe('false')
  })

  it('keeps the typed value when it is shown and hidden', async () => {
    await renderField({ value: 'Secret1pw' })

    fireEvent.click(showButton())
    expect(fieldLabelled(label).value).toBe('Secret1pw')
    fireEvent.click(hideButton())

    expect(fieldLabelled(label).value).toBe('Secret1pw')
  })

  it('does not call onChange when the button is pressed', async () => {
    const onChange = vi.fn()
    await renderField({ onChange })

    fireEvent.click(showButton())
    fireEvent.click(hideButton())

    expect(onChange).not.toHaveBeenCalled()
  })

  it('does not submit the surrounding form when the button is pressed', async () => {
    const onSubmit = vi.fn((event: FormEvent) => {
      event.preventDefault()
    })
    await renderElementWithProviders(
      <form onSubmit={onSubmit}>
        <KvitPasswordField
          id="password-field"
          label={label}
          value="Secret1pw"
          onChange={vi.fn()}
          autoComplete="current-password"
          enterKeyHint="done"
        />
      </form>,
      '/',
    )

    fireEvent.click(showButton())

    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('prevents the default of mousedown on the button so the input keeps focus', async () => {
    await renderField()

    const wasNotPrevented = fireEvent.mouseDown(showButton())

    expect(wasNotPrevented).toBe(false)
  })

  it('shows and hides each field on its own when there are two fields', async () => {
    await renderElementWithProviders(
      <>
        <KvitPasswordField
          id="first-field"
          label="First"
          value="a"
          onChange={vi.fn()}
          autoComplete="new-password"
          enterKeyHint="next"
        />
        <KvitPasswordField
          id="second-field"
          label="Second"
          value="b"
          onChange={vi.fn()}
          autoComplete="new-password"
          enterKeyHint="done"
        />
      </>,
      '/',
    )

    const [firstButton] = screen.getAllByRole('button', {
      name: translated('en', 'common.showPassword'),
    })
    fireEvent.click(firstButton)

    expect(fieldLabelled('First').type).toBe('text')
    expect(fieldLabelled('Second').type).toBe('password')
    expect(screen.getAllByRole('button', { name: translated('en', 'common.showPassword') })).toHaveLength(1)
  })

  it('starts hidden again every time it is mounted', async () => {
    await renderField()
    fireEvent.click(showButton())
    expect(fieldLabelled(label).type).toBe('text')
    cleanup()

    await renderField()

    expect(fieldLabelled(label).type).toBe('password')
  })

  it('has the show and hide texts in English and Macedonian', () => {
    expect(
      languages.flatMap((language) => [
        translated(language, 'common.showPassword'),
        translated(language, 'common.hidePassword'),
      ]),
    ).toEqual([
      'Show password',
      'Hide password',
      'Покажи ја лозинката',
      'Сокриј ја лозинката',
    ])
  })
})
