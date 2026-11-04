import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { OutboxProvider } from '@/core/outbox/OutboxProvider'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { createFakeOutbox } from '@/test/fakeOutboxSender'
import { queuedExpense } from '@/test/outboxScreenTestData'
import { closeActionOfDiscardSheet } from '@/test/outboxScreenTestHelpers'
import { outboxText } from '@/test/outboxTextsTestData'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { seedMe, testMe } from '@/test/testMe'
import { DiscardWaitingSheet } from './DiscardWaitingSheet'

async function renderSheet(item: OutboxItem, language: Language = 'en') {
  const outbox = createFakeOutbox([item])
  const onClose = vi.fn()
  await renderElementWithProviders(
    <OutboxProvider sender={outbox.sender}>
      <DiscardWaitingSheet item={item} onClose={onClose} />
    </OutboxProvider>,
    '/discard',
    { language, seedCache: seedMe({ ...testMe, language }) },
  )
  return { ...outbox, onClose }
}

function sheet(language: Language = 'en'): HTMLElement {
  return screen.getByRole('dialog', { name: outboxText(language, 'outbox.discardTitle') })
}

function discardButton(language: Language = 'en'): HTMLElement {
  return within(sheet(language)).getByRole('button', { name: outboxText(language, 'outbox.discard') })
}

const waitingItem = queuedExpense('waiting-expense')

describe('DiscardWaitingSheet', () => {
  it.each(languages)('asks whether to discard this change and tells what happens (%s)', async (language) => {
    await renderSheet(waitingItem, language)

    expect(sheet(language)).toBeTruthy()
    expect(within(sheet(language)).getByText(outboxText(language, 'outbox.discardBody'))).toBeTruthy()
  })

  it('has a Discard button that is ready to press for a change that is only waiting', async () => {
    await renderSheet(waitingItem)

    expect(discardButton().matches(':disabled')).toBe(false)
  })

  it('discards the change with its id, and then closes', async () => {
    const { discard, onClose } = await renderSheet(waitingItem)

    fireEvent.click(discardButton())

    await waitFor(() => {
      expect(onClose).toHaveBeenCalledTimes(1)
    })
    expect(discard).toHaveBeenCalledTimes(1)
    expect(discard).toHaveBeenCalledWith('waiting-expense')
  })

  it('does not close before the discard has answered', async () => {
    const { discard, onClose } = await renderSheet(waitingItem)
    discard.mockImplementationOnce(() => new Promise<boolean>(() => {}))

    fireEvent.click(discardButton())
    await new Promise((resolve) => setTimeout(resolve, 50))

    expect(discard).toHaveBeenCalledTimes(1)
    expect(onClose).not.toHaveBeenCalled()
  })

  it('just closes when the discard answers false because the change was already on its way', async () => {
    const { discard, onClose } = await renderSheet(waitingItem)
    discard.mockResolvedValueOnce(false)

    fireEvent.click(discardButton())

    await waitFor(() => {
      expect(onClose).toHaveBeenCalledTimes(1)
    })
    expect(screen.queryByRole('alert')).toBeNull()
  })

  it('closes without discarding anything with its close action', async () => {
    const { discard, onClose } = await renderSheet(waitingItem)

    fireEvent.click(closeActionOfDiscardSheet())

    await waitFor(() => {
      expect(onClose).toHaveBeenCalledTimes(1)
    })
    expect(discard).not.toHaveBeenCalled()
  })

  describe('for a change that is being sent right now', () => {
    const sendingItem = queuedExpense('sending-expense', { state: 'sending' })

    it('disables the Discard button', async () => {
      await renderSheet(sendingItem)

      expect(within(sheet()).getByRole('button', { name: outboxText('en', 'outbox.sending') }).matches(':disabled')).toBe(true)
    })

    it.each(languages)('writes "Sending…" on the disabled button instead of Discard (%s)', async (language) => {
      await renderSheet(sendingItem, language)

      expect(within(sheet(language)).queryByRole('button', { name: outboxText(language, 'outbox.discard') })).toBeNull()
      expect(within(sheet(language)).getByRole('button', { name: outboxText(language, 'outbox.sending') })).toBeTruthy()
    })

    it('discards nothing when the disabled button is pressed', async () => {
      const { discard, onClose } = await renderSheet(sendingItem)

      fireEvent.click(within(sheet()).getByRole('button', { name: outboxText('en', 'outbox.sending') }))
      await new Promise((resolve) => setTimeout(resolve, 50))

      expect(discard).not.toHaveBeenCalled()
      expect(onClose).not.toHaveBeenCalled()
    })

    it('can still be closed', async () => {
      const { onClose } = await renderSheet(sendingItem)

      fireEvent.click(closeActionOfDiscardSheet())

      await waitFor(() => {
        expect(onClose).toHaveBeenCalledTimes(1)
      })
    })
  })
})
