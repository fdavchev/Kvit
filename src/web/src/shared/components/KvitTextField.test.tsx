import { fireEvent, render, screen } from '@testing-library/react'
import type { ComponentProps } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { descriptionOf, fieldLabelled } from '@/test/formTestHelpers'
import { KvitTextField } from './KvitTextField'

const label = 'Email address'

function renderField(props: Partial<ComponentProps<typeof KvitTextField>> = {}): void {
  render(
    <KvitTextField id="email-field" label={label} value="" onChange={vi.fn()} {...props} />,
  )
}

describe('KvitTextField', () => {
  it('binds the label to the input so the field can be found by its label', () => {
    renderField()

    const field = fieldLabelled(label)

    expect(field.tagName).toBe('INPUT')
    expect(field.id).toBe('email-field')
  })

  it('shows the value it is given', () => {
    renderField({ value: 'filip@example.com' })

    expect(fieldLabelled(label).value).toBe('filip@example.com')
  })

  it('calls onChange with the new text, not with the event', () => {
    const onChange = vi.fn()
    renderField({ onChange })

    fireEvent.change(fieldLabelled(label), { target: { value: 'a@b.mk' } })

    expect(onChange).toHaveBeenCalledExactlyOnceWith('a@b.mk')
  })

  it('is a text input when no type is given', () => {
    renderField()

    expect(fieldLabelled(label).type).toBe('text')
  })

  it.each(['email', 'password'] as const)('is a %s input when that type is given', (type) => {
    renderField({ type })

    expect(fieldLabelled(label).type).toBe(type)
  })

  it('puts the keyboard and autofill attributes it is given on the input', () => {
    renderField({
      autoComplete: 'email',
      inputMode: 'email',
      enterKeyHint: 'next',
      autoCapitalize: 'none',
      autoCorrect: 'off',
    })

    const field = fieldLabelled(label)

    expect({
      autocomplete: field.getAttribute('autocomplete'),
      inputmode: field.getAttribute('inputmode'),
      enterkeyhint: field.getAttribute('enterkeyhint'),
      autocapitalize: field.getAttribute('autocapitalize'),
      autocorrect: field.getAttribute('autocorrect'),
    }).toEqual({
      autocomplete: 'email',
      inputmode: 'email',
      enterkeyhint: 'next',
      autocapitalize: 'none',
      autocorrect: 'off',
    })
  })

  it('shows the hint and links it to the input as its description', () => {
    renderField({ hint: 'Use the email from your invitation.' })

    expect(screen.getByText('Use the email from your invitation.')).toBeTruthy()
    expect(descriptionOf(fieldLabelled(label))).toBe('Use the email from your invitation.')
  })

  it('has no description when there is no hint', () => {
    renderField()

    expect(fieldLabelled(label).hasAttribute('aria-describedby')).toBe(false)
  })

  it('marks the input as invalid when invalid is true', () => {
    renderField({ invalid: true })

    expect(fieldLabelled(label).getAttribute('aria-invalid')).toBe('true')
  })

  it('does not mark the input as invalid by default', () => {
    renderField()

    expect(fieldLabelled(label).getAttribute('aria-invalid')).not.toBe('true')
  })
})
