import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { KvitDialog } from './KvitDialog'

const title = 'This account signs in with Google.'

function renderDialog(onClose: () => void = () => {}): void {
  render(
    <KvitDialog title={title} onClose={onClose}>
      <p>dialog content</p>
      <button type="button">inner button</button>
    </KvitDialog>,
  )
}

function pressOn(element: HTMLElement): void {
  fireEvent.pointerDown(element)
  fireEvent.mouseDown(element)
  fireEvent.pointerUp(element)
  fireEvent.mouseUp(element)
  fireEvent.click(element)
}

function backdropOf(dialog: HTMLElement): HTMLElement {
  const backdrop = dialog.parentElement
  if (backdrop === null) {
    throw new Error('The dialog has no wrapping backdrop element')
  }
  return backdrop
}

describe('KvitDialog', () => {
  it('is a dialog named by its title', () => {
    renderDialog()

    expect(screen.getByRole('dialog', { name: title })).toBeTruthy()
  })

  it('shows the title as visible text inside the dialog', () => {
    renderDialog()

    expect(screen.getByRole('dialog').textContent).toContain(title)
  })

  it('shows its children', () => {
    renderDialog()

    expect(screen.getByText('dialog content')).toBeTruthy()
    expect(screen.getByRole('button', { name: 'inner button' })).toBeTruthy()
  })

  it('moves the focus into the dialog', async () => {
    renderDialog()

    await waitFor(() => {
      expect(screen.getByRole('dialog').contains(document.activeElement)).toBe(true)
    })
  })

  it('calls onClose when Escape is pressed', () => {
    const onClose = vi.fn<() => void>()
    renderDialog(onClose)

    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' })

    expect(onClose).toHaveBeenCalled()
  })

  it('does not call onClose when another key is pressed', () => {
    const onClose = vi.fn<() => void>()
    renderDialog(onClose)

    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Enter' })

    expect(onClose).not.toHaveBeenCalled()
  })

  it('calls onClose when the backdrop around the dialog is pressed', () => {
    const onClose = vi.fn<() => void>()
    renderDialog(onClose)

    pressOn(backdropOf(screen.getByRole('dialog')))

    expect(onClose).toHaveBeenCalled()
  })

  it.each([
    ['the title', () => screen.getByText(title)],
    ['the content', () => screen.getByText('dialog content')],
    ['a button inside', () => screen.getByRole('button', { name: 'inner button' })],
  ])('does not call onClose when %s is pressed', (_name, find) => {
    const onClose = vi.fn<() => void>()
    renderDialog(onClose)

    pressOn(find())

    expect(onClose).not.toHaveBeenCalled()
  })
})
