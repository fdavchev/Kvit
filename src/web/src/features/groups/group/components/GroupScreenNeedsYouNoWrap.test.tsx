import { screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { routes } from '@/core/router/routes'
import type { GroupBalances } from '@/core/services/balances/balancesService'
import type { Settlement } from '@/core/services/settlements/settlementsService'
import { renderGroupApp } from '@/test/activityTestHelpers'
import {
  greeceTripBalances,
  markoOwesGrandmaBalances,
  markoPaidFilipPending,
  markoPaidGrandmaPending,
} from '@/test/balancesTestData'
import { rowsOf, sectionNamed } from '@/test/balancesTestHelpers'
import { freezeTime, unfreezeTime } from '@/test/expenseTestHelpers'
import { testGroup } from '@/test/groupTestData'
import { ownerViewMembers } from '@/test/memberTestData'
import { expectOnlyTheVerbPhraseDoesNotWrap, macedonianVerbPhrase } from '@/test/noWrapTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { formatMoney } from '@/shared/utils/formatMoney'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

async function needsYouCardInMacedonian(balances: GroupBalances, settlements: Settlement[]): Promise<HTMLElement> {
  await renderGroupApp(routes.groupBalances(testGroup.id), {
    language: 'mk',
    balances,
    members: ownerViewMembers,
    group: testGroup,
    settlements,
  })
  await screen.findByRole('region', { name: translated('mk', 'balances.needsYou') })
  const [card] = rowsOf(sectionNamed(translated('mk', 'balances.needsYou')))
  if (card === undefined) {
    throw new Error('The Needs you section has no card')
  }
  return card
}

describe('the Needs you card on the Balances tab in Macedonian', () => {
  beforeEach(() => {
    freezeTime()
    stubDeviceColorScheme('light')
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('keeps платил/а on one line when someone says they paid me', async () => {
    const card = await needsYouCardInMacedonian(greeceTripBalances, [markoPaidFilipPending])
    const sentence = translated('mk', 'balances.saysPaidYou', {
      from: 'Marko',
      amount: formatMoney(80000, 'MKD', 'mk'),
    })

    expect(sentence).toContain(macedonianVerbPhrase)
    expectOnlyTheVerbPhraseDoesNotWrap(card, sentence)
  })

  it('keeps платил/а on one line when someone says they paid a name without an account', async () => {
    const card = await needsYouCardInMacedonian(markoOwesGrandmaBalances, [markoPaidGrandmaPending])
    const sentence = translated('mk', 'balances.saysPaidName', {
      from: 'Marko',
      to: 'Grandma',
      amount: formatMoney(80000, 'MKD', 'mk'),
    })

    expect(sentence).toContain(macedonianVerbPhrase)
    expectOnlyTheVerbPhraseDoesNotWrap(card, sentence)
  })
})
