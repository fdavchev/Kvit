import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { KvitSheet } from './KvitSheet'

const title = 'Add a name'

function renderSheet(onClose: () => void = () => {}): void {
  render(
    <KvitSheet title={title} onClose={onClose}>
      <p>sheet content</p>
      <button type="button">inner button</button>
    </KvitSheet>,
  )
}

function pressOn(element: HTMLElement): void {
  fireEvent.pointerDown(element)
  fireEvent.mouseDown(element)
  fireEvent.pointerUp(element)
  fireEvent.mouseUp(element)
  fireEvent.click(element)
}

function backdropOf(sheet: HTMLElement): HTMLElement {
  const backdrop = sheet.parentElement
  if (backdrop === null) {
    throw new Error('The sheet has no wrapping backdrop element')
  }
  return backdrop
}

function popupClassTokens(): string[] {
  return screen.getByRole('dialog').className.split(/\s+/)
}

function hasTokenStartingWith(tokens: string[], prefix: string): boolean {
  return tokens.some((token) => token.startsWith(prefix))
}

async function letPendingTimersRun(): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve, 50))
}

describe('KvitSheet', () => {
  it('is a dialog named by its title', () => {
    renderSheet()

    expect(screen.getByRole('dialog', { name: title })).toBeTruthy()
  })

  it('shows the title as visible text inside the sheet', () => {
    renderSheet()

    expect(screen.getByRole('dialog').textContent).toContain(title)
  })

  it('shows its children', () => {
    renderSheet()

    expect(screen.getByText('sheet content')).toBeTruthy()
    expect(screen.getByRole('button', { name: 'inner button' })).toBeTruthy()
  })

  it('moves the focus into the sheet', async () => {
    renderSheet()

    await waitFor(() => {
      expect(screen.getByRole('dialog').contains(document.activeElement)).toBe(true)
    })
  })

  it('calls onClose when Escape is pressed', () => {
    const onClose = vi.fn<() => void>()
    renderSheet(onClose)

    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' })

    expect(onClose).toHaveBeenCalled()
  })

  it('does not call onClose when another key is pressed', () => {
    const onClose = vi.fn<() => void>()
    renderSheet(onClose)

    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Enter' })

    expect(onClose).not.toHaveBeenCalled()
  })

  it('calls onClose when the backdrop around the sheet is pressed', () => {
    const onClose = vi.fn<() => void>()
    renderSheet(onClose)

    pressOn(backdropOf(screen.getByRole('dialog')))

    expect(onClose).toHaveBeenCalled()
  })

  it.each([
    ['the title', () => screen.getByText(title)],
    ['the content', () => screen.getByText('sheet content')],
    ['a button inside', () => screen.getByRole('button', { name: 'inner button' })],
  ])('does not call onClose when %s is pressed', (_name, find) => {
    const onClose = vi.fn<() => void>()
    renderSheet(onClose)

    pressOn(find())

    expect(onClose).not.toHaveBeenCalled()
  })

  it('calls onClose exactly once after Escape, once the closing has finished', async () => {
    const onClose = vi.fn<() => void>()
    renderSheet(onClose)

    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' })

    await waitFor(() => {
      expect(onClose).toHaveBeenCalledTimes(1)
    })
    await letPendingTimersRun()
    expect(onClose).toHaveBeenCalledTimes(1)
  })

  it('calls onClose exactly once after the backdrop is pressed, once the closing has finished', async () => {
    const onClose = vi.fn<() => void>()
    renderSheet(onClose)

    pressOn(backdropOf(screen.getByRole('dialog')))

    await waitFor(() => {
      expect(onClose).toHaveBeenCalledTimes(1)
    })
    await letPendingTimersRun()
    expect(onClose).toHaveBeenCalledTimes(1)
  })

  it('slides and fades out when it closes', () => {
    renderSheet()

    expect(hasTokenStartingWith(popupClassTokens(), 'data-ending-style:')).toBe(true)
  })

  it('slides and fades in from the starting style when it opens', () => {
    renderSheet()

    expect(hasTokenStartingWith(popupClassTokens(), 'data-starting-style:')).toBe(true)
  })

  it('turns the motion off for people who prefer reduced motion', () => {
    renderSheet()

    expect(hasTokenStartingWith(popupClassTokens(), 'motion-reduce:')).toBe(true)
  })
})
