import { act, fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { meQueryKey } from '@/core/auth/useMe'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { routes } from '@/core/router/routes'
import type { Me } from '@/core/services/me/meService'
import { queuedExpense, queuedPersonalEntry } from '@/test/outboxScreenTestData'
import { outboxText } from '@/test/outboxTextsTestData'
import { createDeferred, fakeIdbKeyval, seedOutbox, storedIds } from '@/test/outboxTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { seedMe, testGuestMe, testMe } from '@/test/testMe'
import { resetThemeDocument } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { SettingsScreen } from './SettingsScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

async function renderSettings(me: Me, saved: OutboxItem[]) {
  seedOutbox(me.id, saved)
  const rendered = await renderElementWithProviders(<SettingsScreen />, routes.settings, {
    seedCache: seedMe(me),
  })
  await waitFor(() => {
    expect(rendered.outboxSender.getSnapshot().items).toHaveLength(saved.length)
  })
  return rendered
}

function holdTheLogOutRequest() {
  const answer = createDeferred<Response>()
  const fetchMock = vi.fn<typeof fetch>(() => answer.promise)
  vi.stubGlobal('fetch', fetchMock)
  return { answer, fetchMock }
}

function pressLogOut(): void {
  fireEvent.click(screen.getByRole('button', { name: translated('en', 'settings.logOut') }))
}

function pressLogOutAnyway(): void {
  const sheet = screen.getByRole('dialog', { name: outboxText('en', 'outbox.logOutTitle') })
  fireEvent.click(within(sheet).getByRole('button', { name: outboxText('en', 'outbox.logOutAnyway') }))
}

function pressGuestLogOutAnyway(): void {
  const sheet = screen.getByRole('dialog', { name: translated('en', 'settings.guest.logOutTitle') })
  fireEvent.click(within(sheet).getByRole('button', { name: translated('en', 'settings.guest.logOutAnyway') }))
}

beforeEach(() => {
  window.localStorage.clear()
  resetThemeDocument()
  vi.clearAllMocks()
})

afterEach(() => {
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe('SettingsScreen log out deleting exactly the changes the warning counted', () => {
  it('removes the counted changes one by one and does not delete the whole queue of the person', async () => {
    const { answer, fetchMock } = holdTheLogOutRequest()
    await renderSettings(testMe, [queuedExpense('a'), queuedPersonalEntry('b')])
    pressLogOut()
    pressLogOutAnyway()
    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })

    await act(async () => {
      answer.resolve(new Response(null, { status: 204 }))
    })

    await waitFor(() => {
      expect(storedIds(testMe.id)).toEqual([])
    })
    expect(fakeIdbKeyval.del).not.toHaveBeenCalled()
  })

  it('keeps a change that was queued while the log-out request was still running, because it was never counted', async () => {
    const { answer, fetchMock } = holdTheLogOutRequest()
    const { outboxSender } = await renderSettings(testMe, [queuedExpense('a'), queuedPersonalEntry('b')])
    pressLogOut()
    pressLogOutAnyway()
    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })
    await act(async () => {
      await outboxSender.enqueue(queuedExpense('queued-meanwhile'))
    })

    await act(async () => {
      answer.resolve(new Response(null, { status: 204 }))
    })

    await waitFor(() => {
      expect(storedIds(testMe.id)).toEqual(['queued-meanwhile'])
    })
    expect(fakeIdbKeyval.del).not.toHaveBeenCalled()
  })

  it('counts and removes a change that was queued while the warning was open, before Log out anyway was pressed', async () => {
    const { answer, fetchMock } = holdTheLogOutRequest()
    const { outboxSender } = await renderSettings(testMe, [queuedExpense('a'), queuedPersonalEntry('b')])
    pressLogOut()
    await act(async () => {
      await outboxSender.enqueue(queuedExpense('queued-while-open'))
    })
    expect(screen.getByText(outboxText('en', 'outbox.logOutBody', 3))).toBeTruthy()
    pressLogOutAnyway()
    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })

    await act(async () => {
      answer.resolve(new Response(null, { status: 204 }))
    })

    await waitFor(() => {
      expect(storedIds(testMe.id)).toEqual([])
    })
  })

  it('removes only the counted changes of a guest too', async () => {
    const { answer, fetchMock } = holdTheLogOutRequest()
    const { outboxSender } = await renderSettings(testGuestMe, [queuedExpense('a')])
    pressLogOut()
    pressGuestLogOutAnyway()
    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })
    await act(async () => {
      await outboxSender.enqueue(queuedExpense('queued-meanwhile'))
    })

    await act(async () => {
      answer.resolve(new Response(null, { status: 204 }))
    })

    await waitFor(() => {
      expect(storedIds(testGuestMe.id)).toEqual(['queued-meanwhile'])
    })
    expect(fakeIdbKeyval.del).not.toHaveBeenCalled()
  })
})

describe('SettingsScreen log out when the queued changes cannot be removed from the browser', () => {
  async function logOutWithFailingRemoval() {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    const { answer, fetchMock } = holdTheLogOutRequest()
    const rendered = await renderSettings(testMe, [queuedExpense('a'), queuedPersonalEntry('b')])
    pressLogOut()
    pressLogOutAnyway()
    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })
    const failure = new Error('IndexedDB write failed')
    fakeIdbKeyval.update.mockRejectedValue(failure)
    await act(async () => {
      answer.resolve(new Response(null, { status: 204 }))
    })
    return { ...rendered, consoleError, failure }
  }

  it('logs the error with console.error', async () => {
    const { consoleError, failure } = await logOutWithFailingRemoval()

    await waitFor(() => {
      expect(consoleError.mock.calls.some((call) => call.includes(failure))).toBe(true)
    })
  })

  it('still ends logged out', async () => {
    const { queryClient } = await logOutWithFailingRemoval()

    await waitFor(() => {
      expect(queryClient.getQueryData(meQueryKey)).toBeNull()
    })
  })

  it('shows no generic error toast after the log-out', async () => {
    const { consoleError, failure } = await logOutWithFailingRemoval()
    await waitFor(() => {
      expect(consoleError.mock.calls.some((call) => call.includes(failure))).toBe(true)
    })

    expect(vi.mocked(toast.error)).not.toHaveBeenCalled()
  })
})
