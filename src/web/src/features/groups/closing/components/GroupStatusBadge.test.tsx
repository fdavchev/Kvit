import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { GroupStatus } from '@/core/services/groups/groupsService'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { colorTokenOfClasses, contrastBetween, type ThemeName } from '@/test/indexCssColors'
import { translated } from '@/test/translated'
import { GroupStatusBadge } from './GroupStatusBadge'

const borderWidthClass = /^border(-\d+(\.\d+)?|-\[[\d.]+px\])?$/
const minimumEdgeContrast = 3

const badgeCases: { status: GroupStatus; textKey: string }[] = [
  { status: 'Closing', textKey: 'closing.badgeClosing' },
  { status: 'Finished', textKey: 'closing.badgeFinished' },
]

async function renderedBadge(status: GroupStatus, textKey: string): Promise<HTMLElement> {
  await renderElementWithProviders(<GroupStatusBadge status={status} />, '/badge')
  return screen.getByText(translated('en', textKey))
}

describe('the Closing and Finished badge', () => {
  describe.each(badgeCases)('for a $status group', ({ status, textKey }) => {
    it('has a border in light mode too, not only in dark mode', async () => {
      const badge = await renderedBadge(status, textKey)

      const lightClasses = Array.from(badge.classList).filter((className) => !className.includes(':'))

      expect(lightClasses.some((className) => borderWidthClass.test(className))).toBe(true)
    })

    it.each<ThemeName>(['light', 'dark'])(
      'has a border color that stands out from the page by at least 3:1 in %s mode',
      async (theme) => {
        const badge = await renderedBadge(status, textKey)

        const borderToken = colorTokenOfClasses(Array.from(badge.classList), 'border', theme)

        if (borderToken === null) {
          throw new Error(`The badge has no border color class in ${theme} mode: "${badge.className}"`)
        }
        expect(contrastBetween(borderToken, 'background', theme)).toBeGreaterThanOrEqual(minimumEdgeContrast)
      },
    )
  })

  it('shows nothing for an open group', async () => {
    await renderElementWithProviders(<GroupStatusBadge status="Open" />, '/badge')

    expect(screen.queryByText(translated('en', 'closing.badgeClosing'))).toBeNull()
    expect(screen.queryByText(translated('en', 'closing.badgeFinished'))).toBeNull()
  })
})
