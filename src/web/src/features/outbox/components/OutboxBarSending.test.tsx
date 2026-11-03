import { screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { OutboxProvider } from '@/core/outbox/OutboxProvider'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { createOutboxSender, type OutboxSender } from '@/core/outbox/outboxSender'
import { createDeferred } from '@/test/outboxTestHelpers'
import { queuedExpense } from '@/test/outboxScreenTestData'
import { outboxText } from '@/test/outboxTextsTestData'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { seedMe, testMe } from '@/test/testMe'
import { OutboxBar } from './OutboxBar'

const personId = 'person-1'

function bar(): HTMLElement | null {
  return document.querySelector<HTMLElement>('[data-outbox-bar]')
}

function barText(): string {
  return (bar()?.textContent ?? '').replace(/\s+/g, ' ').trim()
}

interface BarWatch {
  textsAfterEveryChange: string[]
  barRemovals: number
  stop: () => void
}

function watchTheBar(): BarWatch {
  const watch: BarWatch = { textsAfterEveryChange: [], barRemovals: 0, stop: () => {} }
  const observer = new MutationObserver((records) => {
    watch.textsAfterEveryChange.push(bar() === null ? 'no bar' : barText())
    for (const record of records) {
      watch.barRemovals += Array.from(record.removedNodes).filter(
        (node) => node instanceof HTMLElement && node.matches('[data-outbox-bar]'),
      ).length
    }
  })
  observer.observe(document.body, { childList: true, subtree: true, characterData: true })
  watch.stop = () => observer.disconnect()
  return watch
}

async function renderBarWithRealSender(
  sendItem: (item: OutboxItem) => Promise<unknown>,
): Promise<OutboxSender> {
  const sender = createOutboxSender({ personId, sendItem, afterSent: async () => undefined })
  await renderElementWithProviders(
    <OutboxProvider sender={sender}>
      <OutboxBar />
    </OutboxProvider>,
    '/bar',
    { seedCache: seedMe(testMe) },
  )
  return sender
}

interface ReactActFlag {
  IS_REACT_ACT_ENVIRONMENT?: boolean
}

function setActEnvironment(value: boolean | undefined): void {
  ;(globalThis as ReactActFlag).IS_REACT_ACT_ENVIRONMENT = value
}

let actEnvironmentBefore: boolean | undefined

beforeEach(() => {
  actEnvironmentBefore = (globalThis as ReactActFlag).IS_REACT_ACT_ENVIRONMENT
  setActEnvironment(false)
})

afterEach(() => {
  setActEnvironment(actEnvironmentBefore)
  vi.restoreAllMocks()
})

describe('OutboxBar while the real sender sends the last waiting change', () => {
  async function sendOneChange() {
    const sending = createDeferred<void>()
    const sender = await renderBarWithRealSender(() => sending.promise)
    await sender.enqueue(queuedExpense('only-one'))
    await waitFor(() => {
      expect(barText()).toBe(outboxText('en', 'outbox.waiting', 1))
    })
    const watch = watchTheBar()
    const running = sender.run()
    sending.resolve()
    await running
    await waitFor(() => {
      expect(barText()).toBe(outboxText('en', 'outbox.upToDate'))
    })
    return watch
  }

  it('never takes the bar out of the page between the waiting text and the up-to-date text', async () => {
    const watch = await sendOneChange()

    watch.stop()

    expect(watch.barRemovals).toBe(0)
    expect(watch.textsAfterEveryChange).not.toContain('no bar')
  })

  it('shows only the waiting text and then the up-to-date text', async () => {
    const watch = await sendOneChange()

    watch.stop()

    const waiting = outboxText('en', 'outbox.waiting', 1)
    const allowedTexts = new Set([
      waiting,
      `${waiting} · ${outboxText('en', 'outbox.sending')}`,
      outboxText('en', 'outbox.upToDate'),
    ])
    expect(watch.textsAfterEveryChange.filter((text) => !allowedTexts.has(text))).toEqual([])
    expect(screen.getByText(outboxText('en', 'outbox.upToDate'))).toBeTruthy()
  })
})

describe('OutboxBar while the real sender sends several waiting changes', () => {
  it('never takes the bar out of the page from the first waiting text to the up-to-date text', async () => {
    const sender = await renderBarWithRealSender(async () => undefined)
    await sender.enqueue(queuedExpense('first'))
    await sender.enqueue(queuedExpense('second'))
    await waitFor(() => {
      expect(barText()).toContain(outboxText('en', 'outbox.waiting', 2))
    })
    const watch = watchTheBar()

    await sender.run()
    await waitFor(() => {
      expect(barText()).toBe(outboxText('en', 'outbox.upToDate'))
    })

    watch.stop()
    expect(watch.barRemovals).toBe(0)
    expect(watch.textsAfterEveryChange).not.toContain('no bar')
  })
})
