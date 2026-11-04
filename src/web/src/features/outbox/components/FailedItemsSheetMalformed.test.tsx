import { screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { OutboxProvider } from '@/core/outbox/OutboxProvider'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { createFakeOutbox } from '@/test/fakeOutboxSender'
import { queuedExpense, withoutDisplayFields } from '@/test/outboxScreenTestData'
import { consoleErrorCallsAbout } from '@/test/outboxScreenTestHelpers'
import { outboxText } from '@/test/outboxTextsTestData'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { seedMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { FailedItemsSheet } from './FailedItemsSheet'

async function renderSheet(items: readonly OutboxItem[]) {
  const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
  const outbox = createFakeOutbox(items)
  await renderElementWithProviders(
    <OutboxProvider sender={outbox.sender}>
      <FailedItemsSheet onClose={() => {}} />
    </OutboxProvider>,
    '/failed',
    { seedCache: seedMe(testMe) },
  )
  return consoleError
}

function sheet(): HTMLElement {
  return screen.getByRole('dialog', { name: outboxText('en', 'outbox.failedSheetTitle') })
}

afterEach(() => {
  vi.restoreAllMocks()
})

describe('FailedItemsSheet with a stored failed change that has lost its display fields', () => {
  const good = queuedExpense('good', { state: 'failed', display: { title: 'Refused lunch' } })
  const broken = withoutDisplayFields(queuedExpense('broken', { state: 'failed' }))

  it('still shows the other failed change, with its Retry and Discard buttons', async () => {
    await renderSheet([good, broken])

    expect(within(sheet()).getByText('Refused lunch')).toBeTruthy()
  })

  it('shows the broken change too, titled with the dash, with Retry and Discard so it can be got rid of', async () => {
    await renderSheet([good, broken])

    expect(within(sheet()).getAllByRole('button', { name: outboxText('en', 'outbox.retry') })).toHaveLength(2)
    expect(within(sheet()).getAllByRole('button', { name: outboxText('en', 'outbox.discard') })).toHaveLength(2)
    expect(sheet().textContent).toContain('—')
  })

  it('logs the broken change once with console.error, naming its id', async () => {
    const consoleError = await renderSheet([good, broken])

    expect(consoleErrorCallsAbout(consoleError, 'broken')).toHaveLength(1)
  })
})

describe('FailedItemsSheet with a stored failed change that has no reason', () => {
  it('shows the generic error text instead of crashing, and still shows the change', async () => {
    const withoutReason: OutboxItem = { ...queuedExpense('no-reason', { state: 'failed' }), failure: null }

    await renderSheet([withoutReason])

    expect(within(sheet()).getByText(translated('en', 'errors.generic'))).toBeTruthy()
    expect(within(sheet()).getByText('Museum tickets')).toBeTruthy()
  })
})
