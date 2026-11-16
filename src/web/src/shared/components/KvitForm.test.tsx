import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { hasUnsavedTyping } from '@/shared/hooks/useUnsavedTyping'
import { describingElementsOf, isDescribedBy } from '@/test/describedByTestHelpers'
import { fieldLabelled } from '@/test/formTestHelpers'
import { KvitForm } from './KvitForm'
import { KvitTextField } from './KvitTextField'

const errorText = 'This email is already taken.'
const hint = 'Use the email from your invitation.'

interface FormProps {
  errorMessage?: string | null
  invalid?: boolean
  hint?: string
  onSubmit?: () => void
}

function EmailForm({ errorMessage = null, invalid, hint: fieldHint, onSubmit = () => {} }: FormProps) {
  return (
    <KvitForm submitLabel="Save" isPending={false} errorMessage={errorMessage} onSubmit={onSubmit}>
      <KvitTextField id="email-field" label="Email" value="" onChange={() => {}} invalid={invalid} hint={fieldHint} />
    </KvitForm>
  )
}

function typeInEmail(): void {
  fireEvent.input(fieldLabelled('Email'), { target: { value: 'a@b.mk' } })
}

function pressSave(): void {
  fireEvent.click(screen.getByRole('button', { name: 'Save' }))
}

describe('KvitForm linking a refused field to its error', () => {
  it('gives the error an id and points the invalid field to it', () => {
    render(<EmailForm errorMessage={errorText} invalid />)

    const error = screen.getByRole('alert')

    expect(error.id).not.toBe('')
    expect(fieldLabelled('Email').getAttribute('aria-describedby')).toBe(error.id)
    expect(isDescribedBy(fieldLabelled('Email'), error)).toBe(true)
  })

  it('points the invalid field to the error and then to its hint', () => {
    render(<EmailForm errorMessage={errorText} invalid hint={hint} />)

    expect(describingElementsOf(fieldLabelled('Email')).map((element) => element.textContent)).toEqual([errorText, hint])
  })

  it('does not point a field that is not invalid to the error', () => {
    render(<EmailForm errorMessage={errorText} invalid={false} hint={hint} />)

    expect(isDescribedBy(fieldLabelled('Email'), screen.getByRole('alert'))).toBe(false)
    expect(fieldLabelled('Email').getAttribute('aria-describedby')).toBe('email-field-hint')
  })

  it('points the invalid field to nothing while the form shows no error', () => {
    render(<EmailForm invalid />)

    expect(fieldLabelled('Email').hasAttribute('aria-describedby')).toBe(false)
  })

  it('gives two forms on one screen two different error ids', () => {
    render(
      <>
        <EmailForm errorMessage="first error" />
        <EmailForm errorMessage="second error" />
      </>,
    )

    const [first, second] = screen.getAllByRole('alert')

    expect(first?.id).not.toBe(second?.id)
  })
})

describe('KvitForm noting unsaved typing', () => {
  it('has no unsaved typing before anybody types', () => {
    render(<EmailForm />)

    expect(hasUnsavedTyping()).toBe(false)
  })

  it('notes the typing in a field', () => {
    render(<EmailForm />)

    typeInEmail()

    expect(hasUnsavedTyping()).toBe(true)
  })

  it('forgets the typing when the save starts', () => {
    const onSubmit = vi.fn<() => void>()
    render(<EmailForm onSubmit={onSubmit} />)
    typeInEmail()

    pressSave()

    expect(onSubmit).toHaveBeenCalledOnce()
    expect(hasUnsavedTyping()).toBe(false)
  })

  it('notes the typing again when the save comes back with an error', () => {
    const { rerender } = render(<EmailForm />)
    typeInEmail()
    pressSave()

    rerender(<EmailForm errorMessage={errorText} />)

    expect(hasUnsavedTyping()).toBe(true)
  })

  it('forgets the typing when the form goes away', () => {
    const { unmount } = render(<EmailForm />)
    typeInEmail()

    unmount()

    expect(hasUnsavedTyping()).toBe(false)
  })
})
