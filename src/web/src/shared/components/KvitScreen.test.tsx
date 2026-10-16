import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { KvitScreen } from './KvitScreen'

function screenClasses(): string[] {
  return screen.getByRole('main').className.split(/\s+/)
}

describe('KvitScreen', () => {
  it('keeps the phone width of 28 rem below the desktop breakpoint', () => {
    render(<KvitScreen>page</KvitScreen>)

    expect(screenClasses()).toEqual(expect.arrayContaining(['max-w-md', 'mx-auto', 'w-full']))
  })

  it('caps a screen at a readable 40 rem on desktop when no width is given', () => {
    render(<KvitScreen>page</KvitScreen>)

    expect(screenClasses()).toContain('lg:max-w-[40rem]')
    expect(screenClasses()).not.toContain('lg:max-w-[64rem]')
  })

  it('widens a wide screen to 64 rem on desktop', () => {
    render(<KvitScreen width="wide">page</KvitScreen>)

    expect(screenClasses()).toEqual(expect.arrayContaining(['max-w-md', 'lg:max-w-[64rem]']))
    expect(screenClasses()).not.toContain('lg:max-w-[40rem]')
  })

  it('sizes the desktop widths in rem, never in pixels', () => {
    render(<KvitScreen width="wide">page</KvitScreen>)

    expect(screenClasses().filter((name) => name.startsWith('lg:')).join(' ')).not.toMatch(/px/)
  })
})
