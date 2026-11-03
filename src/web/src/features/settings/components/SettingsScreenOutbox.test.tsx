import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { endpoints } from '@/core/api/endpoints'
import { meQueryKey } from '@/core/auth/useMe'
import { languages, type Language } from '@/core/i18n/language'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { routes } from '@/core/router/routes'
import type { Me } from '@/core/services/me/meService'
import { sentRequest, stubFetch } from '@/test/apiTestHelpers'
import { queuedExpense, queuedOneBill, queuedPersonalEntry } from '@/test/outboxScreenTestData'
import { outboxText } from '@/test/outboxTextsTestData'
import { fakeIdbKeyval, seedOutbox, storedIds } from '@/test/outboxTestHelpers'
import { readSavedOutbox } from '@/test/queuedSaveTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { seedMe, testGuestMe, testMe } from '@/test/testMe'
import { resetThemeDocument } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { SettingsScreen } from './SettingsScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const someoneElse = 'person-someone-else'

async function renderSettings(me: Me, saved: OutboxItem[], language: Language = 'en') {
  seedOutbox(me.id, saved)
  const rendered = await renderElementWithProviders(<SettingsScreen />, routes.settings, {
    language,
    seedCache: seedMe({ ...me, language }),
  })
  await waitFor(() => {
    expect(rendered.outboxSender.getSnapshot().items).toHaveLength(saved.length)
  })
  return rendered
}

function logOutButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'settings.logOut') })
}

function warningSheet(language: Language = 'en'): HTMLElement {
  return screen.getByRole('dialog', { name: outboxText(language, 'outbox.logOutTitle') })
}

function buttonIn(sheet: HTMLElement, name: string): HTMLElement {
  return within(sheet).getByRole('button', { name })
}

function twoWaiting(): OutboxItem[] {
  return [queuedExpense('a'), queuedPersonalEntry('b')]
}

beforeEach(() => {
  window.localStorage.clear()
  resetThemeDocument()
  vi.clearAllMocks()
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('SettingsScreen log out for a person with an account and changes not sent yet', () => {
  it('opens a warning sheet instead of logging out, and sends no request', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    await renderSettings(testMe, twoWaiting())

    fireEvent.click(logOutButton())

    expect(warningSheet()).toBeTruthy()
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it.each(languages)('tells how many changes would be lost, and offers Log out anyway and Stay (%s)', async (language) => {
    stubFetch(new Response(null, { status: 204 }))
    await renderSettings(testMe, twoWaiting(), language)

    fireEvent.click(logOutButton(language))

    const sheet = warningSheet(language)
    expect(within(sheet).getByText(outboxText(language, 'outbox.logOutBody', 2))).toBeTruthy()
    expect(buttonIn(sheet, outboxText(language, 'outbox.logOutAnyway'))).toBeTruthy()
    expect(buttonIn(sheet, outboxText(language, 'outbox.stay'))).toBeTruthy()
  })

  it('says "1 change hasn\'t been sent yet" for one change', async () => {
    stubFetch(new Response(null, { status: 204 }))
    await renderSettings(testMe, [queuedExpense('a')])

    fireEvent.click(logOutButton())

    expect(
      within(warningSheet()).getByText("1 change hasn't been sent yet. If you log out now it is lost."),
    ).toBeTruthy()
  })

  it('counts changes in every state: waiting, being sent and failed', async () => {
    stubFetch(new Response(null, { status: 204 }))
    await renderSettings(testMe, [
      queuedExpense('waiting'),
      queuedExpense('sending', { state: 'sending' }),
      queuedExpense('failed', { state: 'failed' }),
    ])

    fireEvent.click(logOutButton())

    expect(
      within(warningSheet()).getByText("3 changes haven't been sent yet. If you log out now they are lost."),
    ).toBeTruthy()
  })

  it('counts only the changes of this person', async () => {
    stubFetch(new Response(null, { status: 204 }))
    seedOutbox(someoneElse, [queuedExpense('theirs-1'), queuedExpense('theirs-2')])
    await renderSettings(testMe, [queuedExpense('mine')])

    fireEvent.click(logOutButton())

    expect(within(warningSheet()).getByText(outboxText('en', 'outbox.logOutBody', 1))).toBeTruthy()
  })

  describe('Stay', () => {
    it('only closes the sheet: no request, the person stays signed in and nothing is deleted', async () => {
      const fetchMock = stubFetch(new Response(null, { status: 204 }))
      const { queryClient } = await renderSettings(testMe, twoWaiting())
      fireEvent.click(logOutButton())

      fireEvent.click(buttonIn(warningSheet(), outboxText('en', 'outbox.stay')))

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      expect(fetchMock).not.toHaveBeenCalled()
      expect(queryClient.getQueryData(meQueryKey)).toMatchObject({ id: testMe.id })
      expect(storedIds(testMe.id)).toEqual(['a', 'b'])
      expect(fakeIdbKeyval.del).not.toHaveBeenCalled()
    })

    it('lets the person press Log out again and get the warning again', async () => {
      stubFetch(new Response(null, { status: 204 }))
      await renderSettings(testMe, twoWaiting())
      fireEvent.click(logOutButton())
      fireEvent.click(buttonIn(warningSheet(), outboxText('en', 'outbox.stay')))
      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })

      fireEvent.click(logOutButton())

      expect(warningSheet()).toBeTruthy()
    })
  })

  describe('Log out anyway', () => {
    it('logs out with the normal log-out request', async () => {
      const fetchMock = stubFetch(new Response(null, { status: 204 }))
      const { queryClient } = await renderSettings(testMe, twoWaiting())
      fireEvent.click(logOutButton())

      fireEvent.click(buttonIn(warningSheet(), outboxText('en', 'outbox.logOutAnyway')))

      await waitFor(() => {
        expect(fetchMock).toHaveBeenCalledOnce()
      })
      expect(sentRequest(fetchMock)).toMatchObject({ url: endpoints.logOut, method: 'POST' })
      await waitFor(() => {
        expect(queryClient.getQueryData(meQueryKey)).toBeNull()
      })
    })

    it('deletes the saved changes of this person once the log out worked', async () => {
      stubFetch(new Response(null, { status: 204 }))
      await renderSettings(testMe, twoWaiting())
      fireEvent.click(logOutButton())

      fireEvent.click(buttonIn(warningSheet(), outboxText('en', 'outbox.logOutAnyway')))

      await waitFor(() => {
        expect(readSavedOutbox(testMe.id)).toEqual([])
      })
      expect(fakeIdbKeyval.del).toHaveBeenCalledWith(testMe.id, expect.anything())
    })

    it('does not delete the saved changes of another person', async () => {
      stubFetch(new Response(null, { status: 204 }))
      seedOutbox(someoneElse, [queuedExpense('theirs')])
      await renderSettings(testMe, twoWaiting())
      fireEvent.click(logOutButton())

      fireEvent.click(buttonIn(warningSheet(), outboxText('en', 'outbox.logOutAnyway')))

      await waitFor(() => {
        expect(readSavedOutbox(testMe.id)).toEqual([])
      })
      expect(storedIds(someoneElse)).toEqual(['theirs'])
    })

    it('keeps the saved changes while the log-out request has not answered yet', async () => {
      const fetchMock = vi.fn<typeof fetch>(() => new Promise<Response>(() => {}))
      vi.stubGlobal('fetch', fetchMock)
      await renderSettings(testMe, twoWaiting())
      fireEvent.click(logOutButton())

      fireEvent.click(buttonIn(warningSheet(), outboxText('en', 'outbox.logOutAnyway')))
      await waitFor(() => {
        expect(fetchMock).toHaveBeenCalledOnce()
      })
      await new Promise((resolve) => setTimeout(resolve, 50))

      expect(storedIds(testMe.id)).toEqual(['a', 'b'])
      expect(fakeIdbKeyval.del).not.toHaveBeenCalled()
    })

    it('keeps the saved changes, shows the error and keeps the person signed in when the log out fails', async () => {
      stubFetch(new Response(null, { status: 500 }))
      const { queryClient } = await renderSettings(testMe, twoWaiting())
      fireEvent.click(logOutButton())

      fireEvent.click(buttonIn(warningSheet(), outboxText('en', 'outbox.logOutAnyway')))

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.generic'))
      })
      expect(storedIds(testMe.id)).toEqual(['a', 'b'])
      expect(fakeIdbKeyval.del).not.toHaveBeenCalled()
      expect(queryClient.getQueryData(meQueryKey)).toMatchObject({ id: testMe.id })
    })

    it('keeps the saved changes when the log out cannot reach the server', async () => {
      stubFetch(new TypeError('Failed to fetch'))
      await renderSettings(testMe, twoWaiting())
      fireEvent.click(logOutButton())

      fireEvent.click(buttonIn(warningSheet(), outboxText('en', 'outbox.logOutAnyway')))

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalled()
      })
      expect(storedIds(testMe.id)).toEqual(['a', 'b'])
    })
  })
})

describe('SettingsScreen log out for a person with an account and nothing saved in the outbox', () => {
  it('logs out at once with one tap, no sheet, and deletes nothing', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    await renderSettings(testMe, [])

    fireEvent.click(logOutButton())

    expect(screen.queryByRole('dialog')).toBeNull()
    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })
    expect(sentRequest(fetchMock)).toMatchObject({ url: endpoints.logOut, method: 'POST' })
    expect(fakeIdbKeyval.del).not.toHaveBeenCalled()
  })

  it('logs out at once also when only another person has saved changes in this browser', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    seedOutbox(someoneElse, [queuedExpense('theirs')])
    await renderSettings(testMe, [])

    fireEvent.click(logOutButton())

    expect(screen.queryByRole('dialog')).toBeNull()
    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })
    expect(storedIds(someoneElse)).toEqual(['theirs'])
  })
})

describe('SettingsScreen log out for a guest', () => {
  function guestSheet(language: Language = 'en'): HTMLElement {
    return screen.getByRole('dialog', { name: translated(language, 'settings.guest.logOutTitle') })
  }

  it('keeps its own sheet and adds the line about the changes that are not sent yet', async () => {
    stubFetch(new Response(null, { status: 204 }))
    await renderSettings(testGuestMe, [queuedOneBill('a'), queuedExpense('b')])

    fireEvent.click(logOutButton())

    const sheet = guestSheet()
    expect(within(sheet).getByText(translated('en', 'settings.guest.logOutBody'))).toBeTruthy()
    expect(within(sheet).getByText(outboxText('en', 'outbox.logOutBody', 2))).toBeTruthy()
  })

  it.each(languages)('writes the added line in the language of the app (%s)', async (language) => {
    stubFetch(new Response(null, { status: 204 }))
    await renderSettings(testGuestMe, [queuedExpense('a')], language)

    fireEvent.click(logOutButton(language))

    expect(within(guestSheet(language)).getByText(outboxText(language, 'outbox.logOutBody', 1))).toBeTruthy()
  })

  it('does not add the line when nothing is saved in the outbox, so the sheet is as before', async () => {
    stubFetch(new Response(null, { status: 204 }))
    await renderSettings(testGuestMe, [])

    fireEvent.click(logOutButton())

    expect(guestSheet()).toBeTruthy()
    expect(screen.queryByText(/been sent yet/)).toBeNull()
  })

  it('logs out with Log out anyway and deletes the saved changes of the guest', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    await renderSettings(testGuestMe, [queuedExpense('a'), queuedExpense('b')])
    fireEvent.click(logOutButton())

    fireEvent.click(buttonIn(guestSheet(), translated('en', 'settings.guest.logOutAnyway')))

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })
    expect(sentRequest(fetchMock)).toMatchObject({ url: endpoints.logOut, method: 'POST' })
    await waitFor(() => {
      expect(readSavedOutbox(testGuestMe.id)).toEqual([])
    })
    expect(fakeIdbKeyval.del).toHaveBeenCalledWith(testGuestMe.id, expect.anything())
  })

  it('keeps the saved changes of the guest when the log out fails', async () => {
    stubFetch(new Response(null, { status: 500 }))
    await renderSettings(testGuestMe, [queuedExpense('a')])
    fireEvent.click(logOutButton())

    fireEvent.click(buttonIn(guestSheet(), translated('en', 'settings.guest.logOutAnyway')))

    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.generic'))
    })
    expect(storedIds(testGuestMe.id)).toEqual(['a'])
    expect(fakeIdbKeyval.del).not.toHaveBeenCalled()
  })

  it('keeps the saved changes with Cancel', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    await renderSettings(testGuestMe, [queuedExpense('a')])
    fireEvent.click(logOutButton())

    fireEvent.click(buttonIn(guestSheet(), translated('en', 'settlements.cancel')))

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).toBeNull()
    })
    expect(fetchMock).not.toHaveBeenCalled()
    expect(storedIds(testGuestMe.id)).toEqual(['a'])
  })
})
