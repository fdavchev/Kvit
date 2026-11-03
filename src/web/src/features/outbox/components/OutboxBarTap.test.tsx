import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { OutboxProvider } from '@/core/outbox/OutboxProvider'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { createOutboxSender } from '@/core/outbox/outboxSender'
import { createFakeOutbox } from '@/test/fakeOutboxSender'
import { queuedExpense } from '@/test/outboxScreenTestData'
import { outboxText } from '@/test/outboxTextsTestData'
import { createDeferred } from '@/test/outboxTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { seedMe, testMe } from '@/test/testMe'
import { expectTapTarget } from '@/test/tapTargetTestHelpers'
import { OutboxBar } from './OutboxBar'

function waitingItems(count: number): OutboxItem[] {
  return Array.from({ length: count }, (_value, index) => queuedExpense(`waiting-${index}`))
}

function failedItems(count: number): OutboxItem[] {
  return Array.from({ length: count }, (_value, index) =>
    queuedExpense(`failed-${index}`, { state: 'failed', display: { title: `Refused ${index}` } }),
  )
}

async function renderBar(items: readonly OutboxItem[], language: Language = 'en', isSending: boolean = false) {
  const outbox = createFakeOutbox(items, false, isSending)
  await renderElementWithProviders(
    <OutboxProvider sender={outbox.sender}>
      <OutboxBar />
    </OutboxProvider>,
    '/bar',
    { language, seedCache: seedMe({ ...testMe, language }) },
  )
  return outbox
}

function bar(): HTMLElement {
  const found = document.querySelector<HTMLElement>('[data-outbox-bar]')
  if (found === null) {
    throw new Error('The outbox bar is not on the page')
  }
  return found
}

function waitingButton(count: number, language: Language = 'en'): HTMLElement {
  return within(bar()).getByRole('button', { name: outboxText(language, 'outbox.waiting', count) })
}

afterEach(() => {
  vi.restoreAllMocks()
})

describe('OutboxBar as a button while changes wait', () => {
  it.each(languages)('is a button named by the waiting text (%s)', async (language) => {
    await renderBar(waitingItems(2), language)

    expect(waitingButton(2, language)).toBeTruthy()
  })

  it('is a button named by the waiting text for one change', async () => {
    await renderBar(waitingItems(1))

    expect(waitingButton(1)).toBeTruthy()
  })

  it('is still a button while a request is running, and its name still starts with the waiting text', async () => {
    await renderBar(waitingItems(2), 'en', true)

    expect(within(bar()).getByRole('button', { name: new RegExp(`^${outboxText('en', 'outbox.waiting', 2)}`) })).toBeTruthy()
  })

  it('has a tap area of at least 44 px', async () => {
    await renderBar(waitingItems(2))

    expectTapTarget(waitingButton(2), 'The bar in its waiting state')
  })

  it('stays a status region so a screen reader still announces the count', async () => {
    await renderBar(waitingItems(2))

    expect(bar().getAttribute('role')).toBe('status')
  })

  it('sends at once when it is tapped, with a try that counts as a retry', async () => {
    const outbox = await renderBar(waitingItems(2))

    fireEvent.click(waitingButton(2))

    expect(outbox.run).toHaveBeenCalledTimes(1)
    expect(outbox.run.mock.calls[0][0]?.countsAsRetry ?? true).toBe(true)
  })

  it('opens no sheet when it is tapped', async () => {
    await renderBar(waitingItems(2))

    fireEvent.click(waitingButton(2))

    expect(screen.queryByRole('dialog')).toBeNull()
  })

  it('logs the problem with console.error and stays on the page when the send fails', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    const outbox = await renderBar(waitingItems(2))
    const failure = new Error('could not load the outbox')
    outbox.run.mockRejectedValueOnce(failure)

    fireEvent.click(waitingButton(2))

    await waitFor(() => {
      expect(consoleError.mock.calls.some((call) => call.includes(failure))).toBe(true)
    })
    expect(bar()).toBeTruthy()
  })
})

describe('OutboxBar tapped while a request is already running', () => {
  it('does not send anything twice', async () => {
    const answer = createDeferred<void>()
    const sendItem = vi.fn(() => answer.promise)
    const sender = createOutboxSender({ personId: testMe.id, sendItem, afterSent: async () => undefined })
    await renderElementWithProviders(
      <OutboxProvider sender={sender}>
        <OutboxBar />
      </OutboxProvider>,
      '/bar',
      { seedCache: seedMe(testMe) },
    )
    await sender.enqueue(queuedExpense('only-one'))
    void sender.run()
    const button = await screen.findByRole('button', { name: new RegExp(`^${outboxText('en', 'outbox.waiting', 1)}`) })
    await waitFor(() => {
      expect(sendItem).toHaveBeenCalledTimes(1)
    })

    fireEvent.click(button)
    fireEvent.click(button)
    answer.resolve()

    expect(await screen.findByText(outboxText('en', 'outbox.upToDate'))).toBeTruthy()
    expect(sendItem).toHaveBeenCalledTimes(1)
  })
})

describe('OutboxBar in its other states', () => {
  it('keeps only its Review button when changes failed, and sends nothing when it is tapped', async () => {
    const outbox = await renderBar(failedItems(1))

    const buttons = within(bar()).getAllByRole('button')
    fireEvent.click(bar())

    expect(buttons.map((button) => button.textContent)).toEqual([outboxText('en', 'outbox.review')])
    expect(outbox.run).not.toHaveBeenCalled()
  })

  it('keeps only its Review button when changes wait and others failed', async () => {
    await renderBar([...waitingItems(2), ...failedItems(1)])

    const buttons = within(bar()).getAllByRole('button')

    expect(buttons.map((button) => button.textContent)).toEqual([outboxText('en', 'outbox.review')])
  })

  it('has a Review button with a tap area of at least 44 px', async () => {
    await renderBar(failedItems(1))

    expectTapTarget(within(bar()).getByRole('button', { name: outboxText('en', 'outbox.review') }), 'The Review button')
  })

  it('is not a button when everything is up to date', async () => {
    const outbox = await renderBar([])

    outbox.show([], { justFinished: true })

    expect(within(bar()).queryByRole('button')).toBeNull()
  })
})
