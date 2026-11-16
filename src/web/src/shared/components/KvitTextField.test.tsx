import { fireEvent, render, screen } from '@testing-library/react'
import type { ComponentProps } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { describingElementsOf } from '@/test/describedByTestHelpers'
import { descriptionOf, fieldLabelled } from '@/test/formTestHelpers'
import { expectPlaceholderLegibleOn, placeholderTokenOf } from '@/test/placeholderTestHelpers'
import { KvitFormErrorIdContext } from './kvitFormErrorId'
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

  describe('linking a refused field to the error of its form', () => {
    const hint = 'Use the email from your invitation.'
    const errorText = 'This email is already taken.'

    function renderFieldInFormWithError(props: Partial<ComponentProps<typeof KvitTextField>>, errorId: string | null = 'form-error'): void {
      render(
        <KvitFormErrorIdContext value={errorId}>
          <KvitTextField id="email-field" label={label} value="" onChange={vi.fn()} {...props} />
          {errorId !== null && <p id={errorId}>{errorText}</p>}
        </KvitFormErrorIdContext>,
      )
    }

    it('points to the error of the form when the field is invalid', () => {
      renderFieldInFormWithError({ invalid: true })

      expect(fieldLabelled(label).getAttribute('aria-describedby')).toBe('form-error')
      expect(descriptionOf(fieldLabelled(label))).toBe(errorText)
    })

    it('points to the error first and to the hint second when the field is invalid and has a hint', () => {
      renderFieldInFormWithError({ invalid: true, hint })

      expect(describingElementsOf(fieldLabelled(label)).map((element) => element.textContent)).toEqual([errorText, hint])
    })

    it('points only to the hint when the field is not invalid', () => {
      renderFieldInFormWithError({ invalid: false, hint })

      expect(descriptionOf(fieldLabelled(label))).toBe(hint)
    })

    it('points only to the hint when invalid is not given', () => {
      renderFieldInFormWithError({ hint })

      expect(descriptionOf(fieldLabelled(label))).toBe(hint)
    })

    it('points to nothing when the field is not invalid and has no hint', () => {
      renderFieldInFormWithError({ invalid: false })

      expect(fieldLabelled(label).hasAttribute('aria-describedby')).toBe(false)
    })

    it('points only to the hint when the field is invalid but the form shows no error', () => {
      renderFieldInFormWithError({ invalid: true, hint }, null)

      expect(descriptionOf(fieldLabelled(label))).toBe(hint)
    })

    it('points to nothing when the field is invalid, has no hint and the form shows no error', () => {
      renderFieldInFormWithError({ invalid: true }, null)

      expect(fieldLabelled(label).hasAttribute('aria-describedby')).toBe(false)
    })

    it('points to nothing outside a form when the field is invalid and has no hint', () => {
      renderField({ invalid: true })

      expect(fieldLabelled(label).hasAttribute('aria-describedby')).toBe(false)
    })
  })

  describe('the placeholder', () => {
    it.each(['light', 'dark'] as const)('is not left to the browser default grey and not faded with an opacity in %s mode', (theme) => {
      renderField({ placeholder: 'Marko' })

      expect(() => placeholderTokenOf(fieldLabelled(label), theme)).not.toThrow()
    })

    it('is drawn in normal weight while the typed text is semibold, so the two never look alike', () => {
      renderField({ placeholder: 'Marko' })

      const classes = Array.from(fieldLabelled(label).classList)

      expect(classes).toContain('placeholder:font-normal')
      expect(classes).toContain('font-semibold')
    })

    it('has at least 4.5:1 contrast on the field colour, in light and in dark mode', () => {
      renderField({ placeholder: 'Marko' })

      expectPlaceholderLegibleOn(fieldLabelled(label), ['field'])
    })
  })
})
