import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { GroupMember } from '@/core/services/groups/membersService'
import { settlementsPath } from '@/test/balancesTestData'
import { amountField, isDisabled, saveButton, sentBody, typeAmount } from '@/test/expenseFormTestHelpers'
import { freezeTime, unfreezeTime } from '@/test/expenseTestHelpers'
import { testGroupId } from '@/test/groupTestData'
import { anaMember, filipMember, grandmaMember, markoMember } from '@/test/memberTestData'
import { colorTokenOfClasses, type ThemeName } from '@/test/indexCssColors'
import { expectPlaceholderLegibleOn } from '@/test/placeholderTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { jsonAnswer, requestCount, stubFetchByRequest } from '@/test/requestTestHelpers'
import { pendingPaymentOf } from '@/test/settlementActionsTestData'
import { isPressed, sheetButton } from '@/test/settlementActionsTestHelpers'
import { RecordPaymentSheet } from './RecordPaymentSheet'

const recordAnotherPayment = 'Record another payment'
const iPaid = 'I paid'
const someonePaidMe = 'Someone paid me'

async function renderOpenSheet(members: readonly GroupMember[]) {
  const fetchMock = stubFetchByRequest({
    [`POST ${settlementsPath}`]: jsonAnswer(pendingPaymentOf(anaMember, filipMember, 20000)),
  })
  await renderElementWithProviders(
    <RecordPaymentSheet
      groupId={testGroupId}
      me={filipMember}
      members={members}
      defaultCurrency="MKD"
      onClose={() => {}}
    />,
    '/sheet',
  )
  const sheet = await screen.findByRole('dialog', { name: recordAnotherPayment })
  return { sheet, fetchMock }
}

describe('the Record another payment sheet when exactly one other person is in the group', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    freezeTime()
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it.each([
    ['a person with an account', anaMember],
    ['a name without an account', markoMember],
  ])('chooses %s as soon as Someone paid me is chosen', async (_name, other) => {
    const { sheet } = await renderOpenSheet([filipMember, other])

    fireEvent.click(sheetButton(sheet, someonePaidMe))

    expect(isPressed(sheetButton(sheet, other.displayName))).toBe(true)
  })

  it('keeps the person chosen when the direction goes back to I paid', async () => {
    const { sheet } = await renderOpenSheet([filipMember, anaMember])
    fireEvent.click(sheetButton(sheet, someonePaidMe))

    fireEvent.click(sheetButton(sheet, iPaid))

    expect(isPressed(sheetButton(sheet, anaMember.displayName))).toBe(true)
  })

  it('records the payment from the only other person to me without pressing the person', async () => {
    const { sheet, fetchMock } = await renderOpenSheet([filipMember, anaMember])
    fireEvent.click(sheetButton(sheet, someonePaidMe))
    typeAmount('200')

    expect(isDisabled(saveButton())).toBe(false)
    fireEvent.click(saveButton())

    await waitFor(() => {
      expect(requestCount(fetchMock, 'POST', settlementsPath)).toBe(1)
    })
    expect(sentBody(fetchMock, 'POST', settlementsPath)).toMatchObject({
      fromMemberId: anaMember.id,
      toMemberId: filipMember.id,
      amountMinor: 20000,
      currency: 'MKD',
    })
  })

  it('records the payment from me to the only other person after going to Someone paid me and back to I paid', async () => {
    const { sheet, fetchMock } = await renderOpenSheet([filipMember, anaMember])
    fireEvent.click(sheetButton(sheet, someonePaidMe))
    fireEvent.click(sheetButton(sheet, iPaid))
    typeAmount('200')

    expect(isDisabled(saveButton())).toBe(false)
    fireEvent.click(saveButton())

    await waitFor(() => {
      expect(requestCount(fetchMock, 'POST', settlementsPath)).toBe(1)
    })
    expect(sentBody(fetchMock, 'POST', settlementsPath)).toMatchObject({
      fromMemberId: filipMember.id,
      toMemberId: anaMember.id,
    })
  })
})

describe('the amount placeholder of the Record another payment sheet', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    freezeTime()
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it.each<ThemeName>(['light', 'dark'])('has at least 4.5:1 contrast on the surface of the sheet in %s mode', async (theme) => {
    const { sheet } = await renderOpenSheet([filipMember, anaMember])

    const surfaceToken = colorTokenOfClasses(Array.from(sheet.classList), 'bg', theme)

    if (surfaceToken === null) {
      throw new Error(`The sheet has no background colour class in ${theme} mode: "${sheet.className}"`)
    }
    expectPlaceholderLegibleOn(amountField(), [surfaceToken])
  })
})

describe('the Record another payment sheet when two or more other people are in the group', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    freezeTime()
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('chooses nobody when Someone paid me is chosen', async () => {
    const { sheet } = await renderOpenSheet([filipMember, anaMember, markoMember])

    fireEvent.click(sheetButton(sheet, someonePaidMe))

    for (const other of [anaMember, markoMember]) {
      expect(isPressed(sheetButton(sheet, other.displayName))).toBe(false)
    }
  })

  it('keeps Save disabled after Someone paid me and an amount, until a person is chosen', async () => {
    const { sheet } = await renderOpenSheet([filipMember, anaMember, markoMember, grandmaMember])
    fireEvent.click(sheetButton(sheet, someonePaidMe))
    typeAmount('200')

    expect(isDisabled(saveButton())).toBe(true)

    fireEvent.click(within(sheet).getByRole('button', { name: markoMember.displayName }))

    expect(isDisabled(saveButton())).toBe(false)
  })
})
