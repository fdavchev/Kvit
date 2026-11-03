import { fireEvent, screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { settlementsPath } from '@/test/balancesTestData'
import { saveButton, typeAmount } from '@/test/expenseFormTestHelpers'
import { testGroupId } from '@/test/groupTestData'
import { anaMember, filipMember, markoMember } from '@/test/memberTestData'
import { answerOnlyWhileOnline, goOffline, goOnline, postCount } from '@/test/offlineTestHelpers'
import { holdOutboxSender, readSavedOutbox } from '@/test/queuedSaveTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { jsonAnswer, stubFetchByRequest } from '@/test/requestTestHelpers'
import { pendingPaymentOf } from '@/test/settlementActionsTestData'
import { sheetButton } from '@/test/settlementActionsTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { RecordPaymentSheet } from './RecordPaymentSheet'

const recordAnotherPayment = 'Record another payment'
const iPaid = 'I paid'
const members = [filipMember, anaMember, markoMember]

async function renderOpenSheet() {
  const fetchMock = stubFetchByRequest({
    [`POST ${settlementsPath}`]: answerOnlyWhileOnline(jsonAnswer(pendingPaymentOf(filipMember, anaMember, 50000))),
  })
  const onClose = vi.fn<() => void>()
  const rendered = await renderElementWithProviders(
    <RecordPaymentSheet groupId={testGroupId} me={filipMember} members={members} defaultCurrency="MKD" onClose={onClose} />,
    '/sheet',
    {
      seedCache: (queryClient) => {
        seedMe(testMe)(queryClient)
        queryClient.setQueryData(['groups', testGroupId], {})
      },
    },
  )
  const sheet = await screen.findByRole('dialog', { name: recordAnotherPayment })
  const run = holdOutboxSender(rendered.outboxSender)
  return { ...rendered, sheet, fetchMock, onClose, run }
}

function chooseIPaidAnaAndSave(sheet: HTMLElement): void {
  fireEvent.click(sheetButton(sheet, iPaid))
  fireEvent.click(within(sheet).getByRole('button', { name: 'Ana' }))
  typeAmount('500')
  fireEvent.click(saveButton())
}

async function letRealTimeGoBy(milliseconds: number): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve, milliseconds))
}

afterEach(() => {
  goOnline()
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe('the Record another payment sheet when the browser is offline and the payment waits for the other person', () => {
  it('shows the network message at once and keeps the sheet open', async () => {
    const { sheet, onClose } = await renderOpenSheet()
    goOffline()

    chooseIPaidAnaAndSave(sheet)

    expect((await within(sheet).findByRole('alert')).textContent).toContain(translated('en', 'errors.network'))
    expect(screen.getByRole('dialog', { name: recordAnotherPayment })).toBe(sheet)
    expect(onClose).not.toHaveBeenCalled()
  })

  it('queues nothing and does not start the sender', async () => {
    const { sheet, run } = await renderOpenSheet()
    goOffline()

    chooseIPaidAnaAndSave(sheet)

    await within(sheet).findByRole('alert')
    expect(readSavedOutbox()).toEqual([])
    expect(run).not.toHaveBeenCalled()
  })

  it('does not send the payment when the browser is online again, not even after the sheet was cancelled', async () => {
    const { sheet, fetchMock } = await renderOpenSheet()
    goOffline()
    chooseIPaidAnaAndSave(sheet)
    await within(sheet).findByRole('alert')
    const requestsWhileOffline = postCount(fetchMock, settlementsPath)

    fireEvent.click(within(sheet).getByRole('button', { name: translated('en', 'settlements.cancel') }))
    goOnline()
    await letRealTimeGoBy(200)

    expect(postCount(fetchMock, settlementsPath)).toBe(requestsWhileOffline)
    expect(requestsWhileOffline).toBeLessThanOrEqual(1)
  })

  it('does not send the payment when the browser is online again with the sheet still open', async () => {
    const { sheet, fetchMock, onClose } = await renderOpenSheet()
    goOffline()
    chooseIPaidAnaAndSave(sheet)
    await within(sheet).findByRole('alert')
    const requestsWhileOffline = postCount(fetchMock, settlementsPath)

    goOnline()
    await letRealTimeGoBy(200)

    expect(postCount(fetchMock, settlementsPath)).toBe(requestsWhileOffline)
    expect(onClose).not.toHaveBeenCalled()
  })
})
