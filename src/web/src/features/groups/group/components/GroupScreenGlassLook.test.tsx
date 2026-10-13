import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { routes } from '@/core/router/routes'
import { renderGroupApp, type GroupAppOptions } from '@/test/activityTestHelpers'
import { anaPaidFilipPending, greeceTripBalances } from '@/test/balancesTestData'
import { findOnlyBalanceCard, rowWithText, sectionNamed } from '@/test/balancesTestHelpers'
import { freezeTime, unfreezeTime } from '@/test/expenseTestHelpers'
import { testGroup } from '@/test/groupTestData'
import { ownerViewMembers } from '@/test/memberTestData'
import {
  buttonNamedIn,
  classTokens,
  openMarkPaid,
  sheetButton,
  tokensStartingWith,
} from '@/test/settlementActionsTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupId = testGroup.id
const filipView: GroupAppOptions = { members: ownerViewMembers, group: testGroup }
const blurPrefix = 'backdrop-blur'
const fillPrefixes = ['backdrop-', 'bg-', 'shadow-']
const wineRed = '(negative|danger|destructive)'

function hasBlur(button: HTMLElement): boolean {
  return classTokens(button).some((token) => token.startsWith(blurPrefix))
}

function hasWineRedOutline(button: HTMLElement): boolean {
  const tokens = classTokens(button)
  const hasBorder = tokens.some((token) => new RegExp(`^border-${wineRed}`).test(token))
  const hasText = tokens.some((token) => new RegExp(`^text-${wineRed}`).test(token))
  return hasBorder && hasText
}

async function renderBalances() {
  await renderGroupApp(routes.groupBalances(groupId), {
    balances: greeceTripBalances,
    settlements: [anaPaidFilipPending],
    ...filipView,
  })
  return findOnlyBalanceCard()
}

describe('the glass look of the payment buttons', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    freezeTime()
    stubDeviceColorScheme('light')
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('blurs what is behind the Mark paid button', async () => {
    const card = await renderBalances()

    expect(hasBlur(buttonNamedIn(rowWithText(card, 'Marko pays you'), 'Mark paid'))).toBe(true)
  })

  it('blurs what is behind the Confirm button', async () => {
    await renderBalances()

    const confirm = buttonNamedIn(rowWithText(sectionNamed('Needs you'), 'Ana says they paid you 500 MKD'), 'Confirm')

    expect(hasBlur(confirm)).toBe(true)
  })

  it('blurs what is behind the Save button of the sheet', async () => {
    const card = await renderBalances()
    const sheet = await openMarkPaid(rowWithText(card, 'Marko pays you'), 'Marko paid me')

    expect(hasBlur(sheetButton(sheet, 'Save'))).toBe(true)
  })

  it('gives the Mark paid, Confirm and Save buttons the same fill, blur and shadow, because they share one look', async () => {
    const card = await renderBalances()
    const markPaid = buttonNamedIn(rowWithText(card, 'Marko pays you'), 'Mark paid')
    const confirm = buttonNamedIn(rowWithText(sectionNamed('Needs you'), 'Ana says they paid you 500 MKD'), 'Confirm')
    const sheet = await openMarkPaid(rowWithText(card, 'Marko pays you'), 'Marko paid me')
    const save = sheetButton(sheet, 'Save')

    const markPaidLook = tokensStartingWith(markPaid, fillPrefixes)

    expect(markPaidLook.length).toBeGreaterThan(0)
    expect(tokensStartingWith(confirm, fillPrefixes)).toEqual(markPaidLook)
    expect(tokensStartingWith(save, fillPrefixes)).toEqual(markPaidLook)
  })

  it('gives the Save button of Record another payment the same look as the Save button of Mark paid', async () => {
    const card = await renderBalances()
    const markPaidSheet = await openMarkPaid(rowWithText(card, 'Marko pays you'), 'Marko paid me')
    const markPaidSave = tokensStartingWith(sheetButton(markPaidSheet, 'Save'), fillPrefixes)
    fireEvent.click(sheetButton(markPaidSheet, 'Cancel'))
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).toBeNull()
    })

    fireEvent.click(screen.getByRole('button', { name: 'Record another payment' }))
    const recordSheet = await screen.findByRole('dialog', { name: 'Record another payment' })

    expect(tokensStartingWith(sheetButton(recordSheet, 'Save'), fillPrefixes)).toEqual(markPaidSave)
  })

  it('gives the Reject button the wine-red outline and the blur, and not the fill of Confirm', async () => {
    await renderBalances()
    const needsYou = rowWithText(sectionNamed('Needs you'), 'Ana says they paid you 500 MKD')
    const reject = buttonNamedIn(needsYou, 'Reject')
    const confirm = buttonNamedIn(needsYou, 'Confirm')

    expect(hasWineRedOutline(reject)).toBe(true)
    expect(hasBlur(reject)).toBe(true)
    expect(hasWineRedOutline(confirm)).toBe(false)
    expect(tokensStartingWith(reject, ['bg-'])).not.toEqual(tokensStartingWith(confirm, ['bg-']))
  })

  it('keeps using the existing sheet component for the sheet', async () => {
    const card = await renderBalances()

    const sheet = await openMarkPaid(rowWithText(card, 'Marko pays you'), 'Marko paid me')

    expect(classTokens(sheet)).toContain('rounded-t-[24px]')
  })
})
