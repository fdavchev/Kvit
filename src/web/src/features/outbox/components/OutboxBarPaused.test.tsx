import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { OutboxProvider } from '@/core/outbox/OutboxProvider'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { createOutboxSender } from '@/core/outbox/outboxSender'
import { queuedExpense } from '@/test/outboxScreenTestData'
import { testApiError } from '@/test/outboxTestData'
import { outboxText } from '@/test/outboxTextsTestData'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { seedMe, testMe } from '@/test/testMe'
import { OutboxBar } from './OutboxBar'

type SendItem = (item: OutboxItem) => Promise<unknown>

function bar(): HTMLElement | null {
  return document.querySelector<HTMLElement>('[data-outbox-bar]')
}

function barText(): string {
  return (bar()?.textContent ?? '').replace(/\s+/g, ' ').trim()
}

async function renderPausedBar() {
  const sendItem = vi.fn<SendItem>().mockRejectedValueOnce(testApiError(401)).mockResolvedValue(undefined)
  const sender = createOutboxSender({ personId: testMe.id, sendItem, afterSent: async () => undefined })
  await renderElementWithProviders(
    <OutboxProvider sender={sender}>
      <OutboxBar />
    </OutboxProvider>,
    '/bar',
    { seedCache: seedMe(testMe) },
  )
  await sender.enqueue(queuedExpense('first'))
  await sender.enqueue(queuedExpense('second'))
  await sender.run()
  return { sender, sendItem }
}

afterEach(() => {
  vi.restoreAllMocks()
})

describe('OutboxBar while the sender is paused because the person must sign in', () => {
  it('says how many changes wait and does not say Sending…', async () => {
    const { sender } = await renderPausedBar()

    await waitFor(() => {
      expect(barText()).toBe(outboxText('en', 'outbox.waiting', 2))
    })
    expect(sender.isPaused()).toBe(true)
    expect(sender.getSnapshot().isSending).toBe(false)
  })

  it('sends nothing when the bar is tapped', async () => {
    const { sendItem } = await renderPausedBar()
    const button = await screen.findByRole('button', { name: outboxText('en', 'outbox.waiting', 2) })

    fireEvent.click(button)
    await new Promise((resolve) => setTimeout(resolve, 30))

    expect(sendItem).toHaveBeenCalledTimes(1)
    expect(barText()).toBe(outboxText('en', 'outbox.waiting', 2))
  })

  it('sends the changes again after the sender is resumed and run, and then says everything is up to date', async () => {
    const { sender, sendItem } = await renderPausedBar()

    sender.resume()
    await sender.run()

    expect(await screen.findByText(outboxText('en', 'outbox.upToDate'))).toBeTruthy()
    expect(sendItem.mock.calls.map(([item]) => item.id)).toEqual(['first', 'first', 'second'])
  })

  it('does not say Sending… again once the resumed sender has nothing left', async () => {
    const { sender } = await renderPausedBar()

    sender.resume()
    await sender.run()

    await waitFor(() => {
      expect(barText()).toBe(outboxText('en', 'outbox.upToDate'))
    })
    expect(barText()).not.toContain(outboxText('en', 'outbox.sending'))
  })
})
