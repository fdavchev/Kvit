import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { KvitInlineError } from './KvitInlineError'

describe('KvitInlineError', () => {
  it('shows the message in an element with the alert role', () => {
    render(<KvitInlineError message="Wrong email or password." />)

    expect(screen.getByRole('alert').textContent).toContain('Wrong email or password.')
  })

  it('has no retry button, unlike the full-screen error', () => {
    render(<KvitInlineError message="Wrong email or password." />)

    expect(screen.queryByRole('button')).toBeNull()
  })
})
