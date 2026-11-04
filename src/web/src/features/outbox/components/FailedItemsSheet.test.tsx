import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { OutboxProvider } from '@/core/outbox/OutboxProvider'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { money } from '@/test/budgetLeftCardTestHelpers'
import { createFakeOutbox } from '@/test/fakeOutboxSender'
import { queuedExpense, queuedOneBill, queuedSettlement } from '@/test/outboxScreenTestData'
import { outboxText } from '@/test/outboxTextsTestData'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { seedMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { FailedItemsSheet } from './FailedItemsSheet'

async function renderSheet(items: readonly OutboxItem[], language: Language = 'en') {
  const outbox = createFakeOutbox(items)
  const onClose = vi.fn()
  await renderElementWithProviders(
    <OutboxProvider sender={outbox.sender}>
      <FailedItemsSheet onClose={onClose} />
    </OutboxProvider>,
    '/failed',
    { language, seedCache: seedMe({ ...testMe, language }) },
  )
  return { ...outbox, onClose }
}

function sheet(language: Language = 'en'): HTMLElement {
  return screen.getByRole('dialog', { name: outboxText(language, 'outbox.failedSheetTitle') })
}

function retryButtons(language: Language = 'en'): HTMLElement[] {
  return within(sheet(language)).queryAllByRole('button', { name: outboxText(language, 'outbox.retry') })
}

function blockOf(text: string, language: Language = 'en'): HTMLElement {
  let node: HTMLElement | null = within(sheet(language)).getByText(text, { exact: false })
  while (node !== null && node !== sheet(language)) {
    if (within(node).queryAllByRole('button', { name: outboxText(language, 'outbox.retry') }).length === 1) {
      return node
    }
    node = node.parentElement
  }
  throw new Error(`Found no block of exactly one failed change around the text "${text}"`)
}

function textOf(element: HTMLElement): string {
  return (element.textContent ?? '').replace(/\s+/g, ' ')
}

const refusedExpense = queuedExpense('refused-expense', {
  state: 'failed',
  display: { title: 'Museum tickets', amountMinor: 150000, currency: 'MKD' },
  failure: { errorCode: 'GROUP_FINISHED', httpStatus: 400 },
})

const refusedPayment = queuedSettlement('refused-payment', {
  state: 'failed',
  display: { payerName: 'Marko', receiverName: 'Filip', amountMinor: 80000, currency: 'MKD' },
  failure: { errorCode: 'SETTLEMENT_NOT_ALLOWED', httpStatus: 403 },
})

describe('FailedItemsSheet', () => {
  it.each(languages)('is a sheet with the title about changes that could not be saved (%s)', async (language) => {
    await renderSheet([refusedExpense], language)

    expect(sheet(language)).toBeTruthy()
  })

  it('shows one block with Retry and Discard for each failed change', async () => {
    await renderSheet([refusedExpense, refusedPayment])

    expect(retryButtons()).toHaveLength(2)
    expect(within(sheet()).getAllByRole('button', { name: outboxText('en', 'outbox.discard') })).toHaveLength(2)
  })

  it('shows the title of a failed expense with its amount', async () => {
    await renderSheet([refusedExpense])

    const block = blockOf('Museum tickets')

    expect(textOf(block)).toContain(money(150000, 'MKD'))
  })

  it('shows a failed euro amount in euros', async () => {
    await renderSheet([queuedExpense('euro', { state: 'failed', display: { title: 'Ferry', amountMinor: 4550, currency: 'EUR' } })])

    expect(textOf(blockOf('Ferry'))).toContain(money(4550, 'EUR'))
  })

  it('shows who paid whom for a failed payment, in the words of the payments list', async () => {
    await renderSheet([refusedPayment])

    const block = blockOf(translated('en', 'settlements.fromPaidTo', { from: 'Marko', to: 'Filip' }))

    expect(textOf(block)).toContain(money(80000, 'MKD'))
  })

  it('shows the name of a failed One bill', async () => {
    await renderSheet([queuedOneBill('refused-bill', { state: 'failed', display: { groupName: 'Dinner at Rex', title: 'Dinner at Rex' } })])

    expect(blockOf('Dinner at Rex')).toBeTruthy()
  })

  it.each(languages)('tells the reason of the refusal in words, from the code the server sent (%s)', async (language) => {
    await renderSheet([refusedExpense], language)

    const block = blockOf('Museum tickets', language)

    expect(textOf(block)).toContain(translated(language, 'errors.GROUP_FINISHED'))
  })

  it('tells each change its own reason', async () => {
    await renderSheet([refusedExpense, refusedPayment])

    const expenseBlock = blockOf('Museum tickets')
    const paymentBlock = blockOf(translated('en', 'settlements.fromPaidTo', { from: 'Marko', to: 'Filip' }))

    expect(textOf(expenseBlock)).toContain(translated('en', 'errors.GROUP_FINISHED'))
    expect(textOf(expenseBlock)).not.toContain(translated('en', 'errors.SETTLEMENT_NOT_ALLOWED'))
    expect(textOf(paymentBlock)).toContain(translated('en', 'errors.SETTLEMENT_NOT_ALLOWED'))
  })

  it('tells the generic reason when the refusal came with no code and no status', async () => {
    await renderSheet([queuedExpense('odd', { state: 'failed', failure: { errorCode: null, httpStatus: null } })])

    expect(textOf(blockOf('Museum tickets'))).toContain(translated('en', 'errors.generic'))
  })

  it('does not list a change that is only waiting or being sent', async () => {
    await renderSheet([
      refusedExpense,
      queuedExpense('still-waiting', { display: { title: 'Taxi home' } }),
      queuedExpense('on-its-way', { state: 'sending', display: { title: 'Coffee' } }),
    ])

    expect(retryButtons()).toHaveLength(1)
    expect(within(sheet()).queryByText('Taxi home', { exact: false })).toBeNull()
    expect(within(sheet()).queryByText('Coffee', { exact: false })).toBeNull()
  })

  it('asks the sender to retry the change with its id when Retry is pressed', async () => {
    const { retry } = await renderSheet([refusedExpense, refusedPayment])

    fireEvent.click(within(blockOf('Museum tickets')).getByRole('button', { name: outboxText('en', 'outbox.retry') }))

    expect(retry).toHaveBeenCalledTimes(1)
    expect(retry).toHaveBeenCalledWith('refused-expense')
  })

  it('discards the change with its id when Discard is pressed, without asking anything first', async () => {
    const { discard } = await renderSheet([refusedExpense, refusedPayment])

    fireEvent.click(within(blockOf('Museum tickets')).getByRole('button', { name: outboxText('en', 'outbox.discard') }))

    expect(discard).toHaveBeenCalledTimes(1)
    expect(discard).toHaveBeenCalledWith('refused-expense')
    expect(screen.getAllByRole('dialog')).toHaveLength(1)
  })

  it('takes the discarded change out of the sheet and keeps the sheet open for the others', async () => {
    const { onClose } = await renderSheet([refusedExpense, refusedPayment])

    fireEvent.click(within(blockOf('Museum tickets')).getByRole('button', { name: outboxText('en', 'outbox.discard') }))

    await waitFor(() => {
      expect(retryButtons()).toHaveLength(1)
    })
    expect(onClose).not.toHaveBeenCalled()
  })

  it('closes by itself when the last failed change is discarded', async () => {
    const { onClose } = await renderSheet([refusedExpense])

    fireEvent.click(within(sheet()).getByRole('button', { name: outboxText('en', 'outbox.discard') }))

    await waitFor(() => {
      expect(onClose).toHaveBeenCalledTimes(1)
    })
  })

  it('closes by itself when the last failed change goes back to waiting after a retry', async () => {
    const { onClose, show } = await renderSheet([refusedExpense])

    show([{ ...refusedExpense, state: 'waiting', failure: null }])

    await waitFor(() => {
      expect(onClose).toHaveBeenCalledTimes(1)
    })
  })

  it('closes by itself when the failed changes are gone for any other reason', async () => {
    const { onClose, show } = await renderSheet([refusedExpense, refusedPayment])

    show([])

    await waitFor(() => {
      expect(onClose).toHaveBeenCalledTimes(1)
    })
  })

  it('stays open while a failed change is left', async () => {
    const { onClose, show } = await renderSheet([refusedExpense, refusedPayment])

    show([refusedPayment])
    await new Promise((resolve) => setTimeout(resolve, 50))

    expect(onClose).not.toHaveBeenCalled()
    expect(retryButtons()).toHaveLength(1)
  })

  it('does not close while it is shown with failed changes', async () => {
    const { onClose } = await renderSheet([refusedExpense])
    await new Promise((resolve) => setTimeout(resolve, 50))

    expect(onClose).not.toHaveBeenCalled()
  })
})
