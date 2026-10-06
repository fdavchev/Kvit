import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { KvitAvatar } from './KvitAvatar'

const pictureUrl = 'https://lh3.googleusercontent.com/a/filip-picture=s96-c'

function pictureOf(container: HTMLElement): HTMLImageElement {
  const picture = container.querySelector('img')
  if (picture === null) {
    throw new Error('Expected the avatar to show a picture, found no img element')
  }
  return picture
}

function colorTokensIn(container: HTMLElement): string[] {
  return [...container.innerHTML.matchAll(/--avatar-(\d+)(?!\d)/g)].map((match) => match[1])
}

function colorIndexOf(container: HTMLElement): string {
  const tokens = new Set(colorTokensIn(container))
  if (tokens.size !== 1) {
    throw new Error(
      `Expected the avatar to use exactly one --avatar-N colour token, found ${JSON.stringify([...tokens])}`,
    )
  }
  return [...tokens][0]
}

describe('KvitAvatar', () => {
  describe('with a picture', () => {
    it('shows the picture from the given address', () => {
      const { container } = render(
        <KvitAvatar name="Filip" pictureUrl={pictureUrl} colorIndex={0} />,
      )

      expect(pictureOf(container).getAttribute('src')).toBe(pictureUrl)
    })

    it('asks the browser to send no referrer with the picture request', () => {
      const { container } = render(
        <KvitAvatar name="Filip" pictureUrl={pictureUrl} colorIndex={0} />,
      )

      expect(pictureOf(container).getAttribute('referrerpolicy')).toBe('no-referrer')
    })

    it('has an empty alt text, because the name is always written next to the circle', () => {
      const { container } = render(
        <KvitAvatar name="Filip" pictureUrl={pictureUrl} colorIndex={0} />,
      )

      expect(pictureOf(container).getAttribute('alt')).toBe('')
      expect(screen.queryByRole('img')).toBeNull()
    })

    it('does not write the initial next to the picture', () => {
      render(<KvitAvatar name="Filip" pictureUrl={pictureUrl} colorIndex={0} />)

      expect(screen.queryByText('F')).toBeNull()
    })

    it('shows the initial on the colour when the picture fails to load', () => {
      const { container } = render(
        <KvitAvatar name="Filip" pictureUrl={pictureUrl} colorIndex={4} />,
      )

      fireEvent.error(pictureOf(container))

      expect(container.querySelector('img')).toBeNull()
      expect(screen.getByText('F')).toBeTruthy()
      expect(colorIndexOf(container)).toBe('4')
    })

    it('keeps showing the picture when it loads fine', () => {
      const { container } = render(
        <KvitAvatar name="Filip" pictureUrl={pictureUrl} colorIndex={0} />,
      )

      fireEvent.load(pictureOf(container))

      expect(pictureOf(container).getAttribute('src')).toBe(pictureUrl)
      expect(screen.queryByText('F')).toBeNull()
    })
  })

  describe('without a picture', () => {
    it('shows the initial of the name and no img element', () => {
      const { container } = render(<KvitAvatar name="Ana" pictureUrl={null} colorIndex={1} />)

      expect(screen.getByText('A')).toBeTruthy()
      expect(container.querySelector('img')).toBeNull()
    })

    it('shows only the first letter of a name with several words', () => {
      render(<KvitAvatar name="Ана Петровска" pictureUrl={null} colorIndex={1} />)

      expect(screen.getByText('А')).toBeTruthy()
    })

    it('shows the first letter of a Macedonian name in Cyrillic', () => {
      render(<KvitAvatar name="Марко" pictureUrl={null} colorIndex={2} />)

      expect(screen.getByText('М')).toBeTruthy()
    })

    it.each([
      [0, '0'],
      [1, '1'],
      [3, '3'],
      [9, '9'],
    ])('uses the colour token --avatar-%i for the place %i in the joining order', (colorIndex, token) => {
      const { container } = render(
        <KvitAvatar name="Ana" pictureUrl={null} colorIndex={colorIndex} />,
      )

      expect(colorIndexOf(container)).toBe(token)
    })

    it.each([
      [10, '0'],
      [11, '1'],
      [19, '9'],
      [20, '0'],
      [25, '5'],
    ])('wraps the place %i around to the colour --avatar-%s', (colorIndex, token) => {
      const { container } = render(
        <KvitAvatar name="Ana" pictureUrl={null} colorIndex={colorIndex} />,
      )

      expect(colorIndexOf(container)).toBe(token)
    })

    it('gives two people on different places different colours', () => {
      const first = render(<KvitAvatar name="Ana" pictureUrl={null} colorIndex={0} />)
      const second = render(<KvitAvatar name="Ana" pictureUrl={null} colorIndex={1} />)

      expect(colorIndexOf(first.container)).not.toBe(colorIndexOf(second.container))
    })

    it('is decorative: it has no image role and no alt text of its own', () => {
      render(<KvitAvatar name="Ana" pictureUrl={null} colorIndex={1} />)

      expect(screen.queryByRole('img')).toBeNull()
    })

    it('is hidden from screen readers, so the initial is not read before the name', () => {
      const { container } = render(<KvitAvatar name="Ana" pictureUrl={null} colorIndex={1} />)

      expect(container.firstElementChild?.getAttribute('aria-hidden')).toBe('true')
    })

    it('is hidden from screen readers when it shows a picture too', () => {
      const { container } = render(
        <KvitAvatar name="Filip" pictureUrl={pictureUrl} colorIndex={0} />,
      )

      expect(container.firstElementChild?.getAttribute('aria-hidden')).toBe('true')
    })
  })

  describe('sizes', () => {
    it('looks different in the small and the large size', () => {
      const small = render(<KvitAvatar name="Ana" pictureUrl={null} colorIndex={1} size="small" />)
      const large = render(<KvitAvatar name="Ana" pictureUrl={null} colorIndex={1} size="large" />)

      expect(small.container.innerHTML).not.toBe(large.container.innerHTML)
    })

    it('draws a picture in the large size too', () => {
      const { container } = render(
        <KvitAvatar name="Filip" pictureUrl={pictureUrl} colorIndex={0} size="large" />,
      )

      expect(pictureOf(container).getAttribute('src')).toBe(pictureUrl)
    })
  })
})
