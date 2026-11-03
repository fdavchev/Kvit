import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { OutboxProvider } from '@/core/outbox/OutboxProvider'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { createFakeOutbox } from '@/test/fakeOutboxSender'
import { queuedExpense } from '@/test/outboxScreenTestData'
import { outboxText } from '@/test/outboxTextsTestData'
import { installFakeTimers, letTimeGoBy } from '@/test/queuedSaveTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { seedMe, testMe } from '@/test/testMe'
import { OutboxBar } from './OutboxBar'

const upToDateShownForMilliseconds = 3000

function waitingItems(count: number): OutboxItem[] {
  return Array.from({ length: count }, (_value, index) => queuedExpense(`waiting-${index}`))
}

function failedItems(count: number): OutboxItem[] {
  return Array.from({ length: count }, (_value, index) =>
    queuedExpense(`failed-${index}`, { state: 'failed', display: { title: `Refused ${index}` } }),
  )
}

async function renderBar(items: readonly OutboxItem[] = [], language: Language = 'en') {
  const outbox = createFakeOutbox(items)
  await renderElementWithProviders(
    <OutboxProvider sender={outbox.sender}>
      <OutboxBar />
    </OutboxProvider>,
    '/bar',
    { language, seedCache: seedMe({ ...testMe, language }) },
  )
  return outbox
}

function bar(): HTMLElement | null {
  return document.querySelector<HTMLElement>('[data-outbox-bar]')
}

function barText(): string {
  return (bar()?.textContent ?? '').replace(/\s+/g, ' ').trim()
}

afterEach(() => {
  vi.useRealTimers()
})

describe('OutboxBar when nothing is queued', () => {
  it('renders nothing', async () => {
    await renderBar([])

    expect(bar()).toBeNull()
    expect(document.body.textContent).not.toContain('waiting')
  })

  it('does not say that everything is up to date when nothing was ever sent', async () => {
    await renderBar([])

    expect(screen.queryByText(outboxText('en', 'outbox.upToDate'))).toBeNull()
  })
})

describe('OutboxBar when changes are waiting', () => {
  it.each(languages)('says how many changes wait and that they are being sent (%s)', async (language) => {
    await renderBar(waitingItems(2), language)

    expect(barText()).toBe(`${outboxText(language, 'outbox.waiting', 2)} · ${outboxText(language, 'outbox.sending')}`)
  })

  it('says "1 change waiting · Sending…" for one change', async () => {
    await renderBar(waitingItems(1))

    expect(barText()).toBe('1 change waiting · Sending…')
  })

  it('counts an item that is being sent as waiting', async () => {
    await renderBar([queuedExpense('a', { state: 'sending' }), queuedExpense('b')])

    expect(barText()).toBe('2 changes waiting · Sending…')
  })

  it('is a status region so a screen reader announces it', async () => {
    await renderBar(waitingItems(2))

    expect(bar()?.getAttribute('role')).toBe('status')
  })

  it('has no Review button while nothing failed', async () => {
    await renderBar(waitingItems(2))

    expect(screen.queryByRole('button', { name: outboxText('en', 'outbox.review') })).toBeNull()
  })

  it('follows the sender when more changes arrive', async () => {
    const outbox = await renderBar(waitingItems(1))

    outbox.show(waitingItems(3))

    expect(barText()).toBe('3 changes waiting · Sending…')
  })

  it('goes away when the last waiting change is gone without anything having been sent', async () => {
    const outbox = await renderBar(waitingItems(1))

    outbox.show([])

    expect(bar()).toBeNull()
  })
})

describe('OutboxBar when changes could not be saved', () => {
  it.each(languages)('says how many changes could not be saved, with a Review button (%s)', async (language) => {
    await renderBar(failedItems(2), language)

    expect(barText()).toContain(outboxText(language, 'outbox.failed', 2))
    expect(screen.getByRole('button', { name: outboxText(language, 'outbox.review') })).toBeTruthy()
  })

  it('says "1 change couldn\'t be saved" for one failed change', async () => {
    await renderBar(failedItems(1))

    expect(barText()).toContain("1 change couldn't be saved")
  })

  it('shows the failure and not the waiting count when changes are waiting and failed at the same time', async () => {
    await renderBar([...waitingItems(2), ...failedItems(1)])

    expect(barText()).toContain("1 change couldn't be saved")
    expect(barText()).not.toContain('waiting')
    expect(barText()).not.toContain(outboxText('en', 'outbox.sending'))
  })

  it('counts only the failed changes in the failure text', async () => {
    await renderBar([...waitingItems(5), ...failedItems(2)])

    expect(barText()).toContain("2 changes couldn't be saved")
  })

  it('is a status region also when it shows a failure', async () => {
    await renderBar(failedItems(1))

    expect(bar()?.getAttribute('role')).toBe('status')
  })

  it('opens the sheet with the failed changes when Review is pressed', async () => {
    await renderBar(failedItems(2))

    fireEvent.click(screen.getByRole('button', { name: outboxText('en', 'outbox.review') }))

    const sheet = await screen.findByRole('dialog', { name: outboxText('en', 'outbox.failedSheetTitle') })
    expect(within(sheet).getAllByRole('button', { name: outboxText('en', 'outbox.retry') })).toHaveLength(2)
  })

  it('opens no sheet by itself', async () => {
    await renderBar(failedItems(2))

    expect(screen.queryByRole('dialog')).toBeNull()
  })

  it('closes the sheet and goes away when the last failed change is discarded in it', async () => {
    const outbox = await renderBar(failedItems(1))
    fireEvent.click(screen.getByRole('button', { name: outboxText('en', 'outbox.review') }))
    const sheet = await screen.findByRole('dialog', { name: outboxText('en', 'outbox.failedSheetTitle') })

    fireEvent.click(within(sheet).getByRole('button', { name: outboxText('en', 'outbox.discard') }))

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).toBeNull()
    })
    expect(outbox.discard).toHaveBeenCalledWith('failed-0')
    expect(bar()).toBeNull()
  })
})

describe('OutboxBar after everything was sent', () => {
  it.each(languages)('says everything is up to date (%s)', async (language) => {
    const outbox = await renderBar([], language)

    outbox.show([], { justFinished: true })

    expect(barText()).toBe(outboxText(language, 'outbox.upToDate'))
  })

  it('is a status region while it says everything is up to date', async () => {
    const outbox = await renderBar([])

    outbox.show([], { justFinished: true })

    expect(bar()?.getAttribute('role')).toBe('status')
  })

  it('still says so just before 3 seconds have passed', async () => {
    const outbox = await renderBar([])
    installFakeTimers()

    outbox.show([], { justFinished: true })
    await letTimeGoBy(upToDateShownForMilliseconds - 1)

    expect(barText()).toBe('All up to date ✓')
  })

  it('goes away by itself after 3 seconds, even though the sender still says it just finished', async () => {
    const outbox = await renderBar([])
    installFakeTimers()

    outbox.show([], { justFinished: true })
    await letTimeGoBy(upToDateShownForMilliseconds)

    expect(bar()).toBeNull()
  })

  it('shows the waiting count and not the up-to-date text when changes are waiting again', async () => {
    const outbox = await renderBar([])

    outbox.show(waitingItems(1), { justFinished: true })

    expect(barText()).toBe('1 change waiting · Sending…')
  })

  it('shows the failure and not the up-to-date text when a change failed', async () => {
    const outbox = await renderBar([])

    outbox.show(failedItems(1), { justFinished: true })

    expect(barText()).toContain("1 change couldn't be saved")
    expect(barText()).not.toContain('up to date')
  })

  it('says everything is up to date again, for 3 more seconds, after a second round of changes was sent', async () => {
    const outbox = await renderBar([])
    installFakeTimers()
    outbox.show([], { justFinished: true })
    await letTimeGoBy(upToDateShownForMilliseconds)
    outbox.show(waitingItems(1))

    outbox.show([], { justFinished: true })

    expect(barText()).toBe('All up to date ✓')
    await letTimeGoBy(upToDateShownForMilliseconds - 1)
    expect(barText()).toBe('All up to date ✓')
    await letTimeGoBy(1)
    expect(bar()).toBeNull()
  })

  it('does not show the up-to-date text again by itself after it went away', async () => {
    const outbox = await renderBar([])
    installFakeTimers()
    outbox.show([], { justFinished: true })
    await letTimeGoBy(upToDateShownForMilliseconds)

    await letTimeGoBy(10 * upToDateShownForMilliseconds)

    expect(bar()).toBeNull()
  })
})

describe('OutboxBar position and motion', () => {
  it('is fixed at the bottom of the screen', async () => {
    await renderBar(waitingItems(1))

    const classes = Array.from(bar()?.classList ?? [])

    expect(classes).toContain('fixed')
    expect(classes.some((token) => token.startsWith('bottom-'))).toBe(true)
  })

  it('sits above the bottom tab bar by using the bottom bar height in its bottom offset, which is 0 where there is no tab bar', async () => {
    await renderBar(waitingItems(1))

    const bottomOffset = Array.from(bar()?.classList ?? []).find((token) => token.startsWith('bottom-'))

    expect(bottomOffset).toContain('--bottom-bar-height')
  })

  it('animates nothing unless the person accepts motion, and never with transition-all', async () => {
    await renderBar(failedItems(1))
    const tokens = [bar(), ...Array.from(bar()?.querySelectorAll<HTMLElement>('*') ?? [])].flatMap((element) =>
      Array.from(element?.classList ?? []),
    )

    expect(tokens.filter((token) => token.includes('animate-') && !token.startsWith('motion-safe:'))).toEqual([])
    expect(tokens).not.toContain('transition-all')
  })
})
