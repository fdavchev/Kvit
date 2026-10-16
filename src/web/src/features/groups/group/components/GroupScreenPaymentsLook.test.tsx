import { screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { routes } from '@/core/router/routes'
import { renderGroupApp } from '@/test/activityTestHelpers'
import { greeceTripBalances } from '@/test/balancesTestData'
import { sectionNamed } from '@/test/balancesTestHelpers'
import { freezeTime, unfreezeTime } from '@/test/expenseTestHelpers'
import { testGroup } from '@/test/groupTestData'
import { filipMember, markoMember, ownerViewMembers } from '@/test/memberTestData'
import { confirmedPaymentOf, paymentIds } from '@/test/settlementActionsTestData'
import { buttonNamedIn } from '@/test/settlementActionsTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { testMe } from '@/test/testMe'
import { colorTokenOfClasses, contrastBetween, type ThemeName } from '@/test/indexCssColors'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const minimumTextContrast = 4.5

const markoPaidFilip = confirmedPaymentOf(markoMember, filipMember, 80000, {
  id: paymentIds.first,
  recordedByUserId: testMe.id,
  createdAt: '2026-10-06T08:00:00Z',
})

async function deleteLink(): Promise<HTMLElement> {
  await renderGroupApp(routes.groupBalances(testGroup.id), {
    balances: greeceTripBalances,
    members: ownerViewMembers,
    group: testGroup,
    settlements: [markoPaidFilip],
  })
  await screen.findByRole('region', { name: 'Payments' })
  return buttonNamedIn(sectionNamed('Payments'), 'Delete')
}

describe('the look of the Delete link in the Payments list', () => {
  beforeEach(() => {
    freezeTime()
    stubDeviceColorScheme('light')
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it.each<ThemeName>(['light', 'dark'])(
    'has a text color with at least 4.5:1 against the card color in %s mode',
    async (theme) => {
      const link = await deleteLink()

      const textToken = colorTokenOfClasses(Array.from(link.classList), 'text', theme)

      if (textToken === null) {
        throw new Error(`The Delete link has no text color class in ${theme} mode: "${link.className}"`)
      }
      expect(contrastBetween(textToken, 'card', theme)).toBeGreaterThanOrEqual(minimumTextContrast)
    },
  )
})
