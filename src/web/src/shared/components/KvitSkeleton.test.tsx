import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { languages } from '@/core/i18n/language'
import { colorOfToken, contrastBetween } from '@/test/indexCssColors'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { loadingLook } from '@/test/skeletonTestHelpers'
import { translated } from '@/test/translated'
import { KvitLoading } from './KvitLoading'
import { KvitSkeleton } from './KvitSkeleton'

function skeleton(): HTMLElement {
  const found = document.querySelector<HTMLElement>('[data-skeleton]')
  if (found === null) {
    throw new Error('No skeleton block was drawn')
  }
  return found
}

describe('KvitSkeleton', () => {
  it('pulses', () => {
    render(<KvitSkeleton className="h-16" />)

    expect(skeleton().classList.contains('animate-pulse')).toBe(true)
  })

  it('stops pulsing for people who prefer reduced motion', () => {
    render(<KvitSkeleton className="h-16" />)

    expect(skeleton().classList.contains('motion-reduce:animate-none')).toBe(true)
  })

  it('is hidden from screen readers', () => {
    render(<KvitSkeleton className="h-16" />)

    expect(skeleton().getAttribute('aria-hidden')).toBe('true')
  })

  it('is painted with the skeleton colour', () => {
    render(<KvitSkeleton className="h-16" />)

    expect(skeleton().classList.contains('bg-skeleton')).toBe(true)
  })

  it('has the colour of a card in light mode, so the rows that replace it do not change colour', () => {
    expect(colorOfToken('skeleton', 'light')).toEqual(colorOfToken('card', 'light'))
  })

  it('stands out from the page in dark mode at a calm 1.4:1 to 1.6:1', () => {
    const contrast = contrastBetween('skeleton', 'background', 'dark')

    expect(contrast, `dark skeleton against the page, ${contrast.toFixed(2)}`).toBeGreaterThanOrEqual(1.4)
    expect(contrast, `dark skeleton against the page, ${contrast.toFixed(2)}`).toBeLessThanOrEqual(1.6)
  })

  it('is lighter than a card in dark mode, so it does not sink into the page', () => {
    expect(contrastBetween('skeleton', 'background', 'dark')).toBeGreaterThan(contrastBetween('card', 'background', 'dark'))
  })

  it('takes the size it is given and lets a given corner replace the default one', () => {
    render(<KvitSkeleton className="h-4 w-24 rounded-full" />)

    expect(skeleton().classList.contains('h-4')).toBe(true)
    expect(skeleton().classList.contains('rounded-full')).toBe(true)
    expect(skeleton().classList.contains('rounded-2xl')).toBe(false)
  })
})

describe('KvitLoading', () => {
  it.each(languages)('tells screen readers that it is loading, with placeholders drawn (%s)', async (language) => {
    await renderElementWithProviders(
      <KvitLoading>
        <KvitSkeleton className="h-16" />
      </KvitLoading>,
      '/',
      { language },
    )

    expect(screen.getByRole('status').textContent).toBe(translated(language, 'common.loading'))
  })

  it('draws the placeholders it is given instead of the spinner', async () => {
    await renderElementWithProviders(
      <KvitLoading>
        <KvitSkeleton className="h-16" />
        <KvitSkeleton className="h-16" />
      </KvitLoading>,
      '/',
    )

    expect(loadingLook()).toEqual({ skeletonCount: 2, hasSpinner: false })
  })

  it('draws the spinner when it is given no placeholders', async () => {
    await renderElementWithProviders(<KvitLoading />, '/')

    expect(loadingLook()).toEqual({ skeletonCount: 0, hasSpinner: true })
  })
})
