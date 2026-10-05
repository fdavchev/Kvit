import { fireEvent, render, screen, within } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { KvitChoiceChips } from './KvitChoiceChips'

const options = [
  { value: 'MKD', label: 'MKD' },
  { value: 'EUR', label: 'EUR' },
]

function renderChips(value: string, onChange: (value: string) => void = () => {}): void {
  render(<KvitChoiceChips label="Currency" options={options} value={value} onChange={onChange} />)
}

function chip(label: string): HTMLElement {
  return screen.getByRole('button', { name: label })
}

describe('KvitChoiceChips', () => {
  it('is a group named by its label', () => {
    renderChips('MKD')

    expect(screen.getByRole('group', { name: 'Currency' })).toBeTruthy()
  })

  it('shows one button per option inside the group, in the given order', () => {
    renderChips('MKD')

    const buttons = within(screen.getByRole('group', { name: 'Currency' })).getAllByRole('button')

    expect(buttons.map((button) => button.textContent)).toEqual(['MKD', 'EUR'])
  })

  it.each([
    ['MKD', 'MKD', 'EUR'],
    ['EUR', 'EUR', 'MKD'],
  ])('marks only the chip of the value %s as pressed', (value, pressed, other) => {
    renderChips(value)

    expect(chip(pressed).getAttribute('aria-pressed')).toBe('true')
    expect(chip(other).getAttribute('aria-pressed')).toBe('false')
  })

  it('marks no chip as pressed when the value matches no option', () => {
    renderChips('USD')

    expect(chip('MKD').getAttribute('aria-pressed')).toBe('false')
    expect(chip('EUR').getAttribute('aria-pressed')).toBe('false')
  })

  it('calls onChange with the value of the chip that is pressed', () => {
    const onChange = vi.fn<(value: string) => void>()
    renderChips('MKD', onChange)

    fireEvent.click(chip('EUR'))

    expect(onChange).toHaveBeenCalledExactlyOnceWith('EUR')
  })

  it('uses the value of an option, not its label, when they differ', () => {
    const onChange = vi.fn<(value: string) => void>()
    render(
      <KvitChoiceChips
        label="Emoji"
        options={[{ value: 'beach', label: '\u{1F3D6}\u{FE0F}' }]}
        value=""
        onChange={onChange}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: '\u{1F3D6}\u{FE0F}' }))

    expect(onChange).toHaveBeenCalledExactlyOnceWith('beach')
  })

  it('does not change which chip is pressed by itself', () => {
    renderChips('MKD')

    fireEvent.click(chip('EUR'))

    expect(chip('MKD').getAttribute('aria-pressed')).toBe('true')
    expect(chip('EUR').getAttribute('aria-pressed')).toBe('false')
  })

  it('makes every chip a plain button that does not submit a form', () => {
    renderChips('MKD')

    expect(chip('MKD').getAttribute('type')).toBe('button')
    expect(chip('EUR').getAttribute('type')).toBe('button')
  })
})
