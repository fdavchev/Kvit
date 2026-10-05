import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { KvitEmojiTile } from './KvitEmojiTile'

const beach = '\u{1F3D6}\u{FE0F}'
const pizza = '\u{1F355}'

function tileElement(container: HTMLElement): Element {
  const tile = container.firstElementChild
  if (tile === null) {
    throw new Error('KvitEmojiTile rendered nothing')
  }
  return tile
}

describe('KvitEmojiTile', () => {
  it('shows the emoji', () => {
    render(<KvitEmojiTile emoji={beach} />)

    expect(screen.getByText(beach)).toBeTruthy()
  })

  it('shows a different emoji when given one', () => {
    render(<KvitEmojiTile emoji={pizza} />)

    expect(screen.getByText(pizza)).toBeTruthy()
  })

  it('is hidden from screen readers because the emoji only decorates the name beside it', () => {
    const { container } = render(<KvitEmojiTile emoji={beach} />)

    expect(tileElement(container).getAttribute('aria-hidden')).toBe('true')
  })

  it('does not appear as an image or a button in the accessibility tree', () => {
    render(<KvitEmojiTile emoji={beach} />)

    expect(screen.queryByRole('img')).toBeNull()
    expect(screen.queryByRole('button')).toBeNull()
  })
})
