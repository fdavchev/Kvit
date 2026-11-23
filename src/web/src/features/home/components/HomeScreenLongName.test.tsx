import { screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { renderHome } from '@/test/homeTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'

const sixtyLetterName = 'Абвгд'.repeat(12)

async function renderedHeaderParts() {
  await renderHome({
    seedCache: seedMe({ ...testMe, displayName: sixtyLetterName }),
  })
  const greeting = await screen.findByRole('heading', {
    level: 1,
    name: translated('en', 'home.greeting', { name: sixtyLetterName }),
  })
  const avatarLink = screen.getByRole('link', { name: translated('en', 'settings.title') })
  return { greeting, avatarLink }
}

function classesOf(element: HTMLElement): string[] {
  return Array.from(element.classList)
}

describe('the Home greeting with a name of 60 letters and no spaces', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('lets the greeting shrink below its own text width and break anywhere', async () => {
    const { greeting } = await renderedHeaderParts()

    expect(sixtyLetterName).toHaveLength(60)
    expect(classesOf(greeting)).toEqual(expect.arrayContaining(['min-w-0', 'wrap-anywhere']))
  })

  it('keeps the avatar link at its own size', async () => {
    const { avatarLink } = await renderedHeaderParts()

    expect(classesOf(avatarLink)).toContain('flex-none')
  })

  it('puts the greeting and the avatar link in the same row', async () => {
    const { greeting, avatarLink } = await renderedHeaderParts()

    expect(greeting.parentElement).toBe(avatarLink.parentElement)
    expect(classesOf(greeting.parentElement as HTMLElement)).toContain('flex')
  })
})
