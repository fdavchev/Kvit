import { fireEvent, screen, waitFor } from '@testing-library/react'
import { expect } from 'vitest'

export function fieldLabelled(label: string): HTMLInputElement {
  return screen.getByLabelText<HTMLInputElement>(label)
}

export function typeInto(label: string, value: string): void {
  fireEvent.change(fieldLabelled(label), { target: { value } })
}

export function descriptionOf(field: HTMLElement): string {
  const ids = (field.getAttribute('aria-describedby') ?? '')
    .split(' ')
    .filter((id) => id !== '')
  return ids
    .map((id) => document.getElementById(id)?.textContent ?? '')
    .join(' ')
}

export async function expectDisabledWhilePending(button: HTMLElement): Promise<void> {
  await waitFor(() => {
    expect(button.matches(':disabled')).toBe(true)
  })
}
