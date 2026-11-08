import { dehydrate, type QueryClient } from '@tanstack/react-query'
import { act, fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { savedQueryCacheVersion } from '@/core/api/savedQueryCache'
import { endpoints } from '@/core/api/endpoints'
import { meQueryKey } from '@/core/auth/useMe'
import { languages, type Language } from '@/core/i18n/language'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { routes } from '@/core/router/routes'
import type { Me } from '@/core/services/me/meService'
import { problemResponse, sentRequest, stubFetch, stubFetchThatNeverAnswers } from '@/test/apiTestHelpers'
import { isChecked } from '@/test/checkedStateTestHelpers'
import { dismissSheetByTappingBackdrop } from '@/test/expenseFormTestHelpers'
import { queuedExpense, queuedPersonalEntry } from '@/test/outboxScreenTestData'
import { outboxText } from '@/test/outboxTextsTestData'
import { createDeferred, fakeIdbKeyval, seedOutbox, storedIds } from '@/test/outboxTestHelpers'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { seedMe, testAdminMe, testGuestMe, testMe } from '@/test/testMe'
import { createTestPersister } from '@/test/testPersister'
import { resetThemeDocument } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { SettingsScreen } from './SettingsScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupsKey = ['groups']
const groups = ['Greece trip']
const welcomePage = 'welcome page'
const millisecondsToWatchForAClose = 50
const people: [string, Me][] = [
  ['a person with a password', testMe],
  ['a person without a password', { ...testMe, hasPassword: false }],
  ['an admin', testAdminMe],
  ['a guest', testGuestMe],
]

interface RenderOptions {
  me?: Me
  language?: Language
  saved?: OutboxItem[]
  persister?: ReturnType<typeof createTestPersister>
}

async function renderSettings(options: RenderOptions = {}) {
  const me = options.me ?? testMe
  const language = options.language ?? 'en'
  const saved = options.saved ?? []
  seedOutbox(me.id, saved)
  const seed = seedMe({ ...me, language })
  const rendered = await renderRoutesWithProviders(
    [
      { path: routes.settings, element: <SettingsScreen /> },
      { path: routes.welcome, element: <p>{welcomePage}</p> },
      { path: '*', element: <p>another page</p> },
    ],
    routes.settings,
    {
      language,
      persister: options.persister,
      seedCache: (queryClient: QueryClient) => {
        seed(queryClient)
        queryClient.setQueryData(groupsKey, groups)
      },
    },
  )
  await waitFor(() => {
    expect(rendered.outboxSender.getSnapshot().items).toHaveLength(saved.length)
  })
  return rendered
}

function rowButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'settings.deleteAccount') })
}

function logOutButton(): HTMLElement {
  return screen.getByRole('button', { name: translated('en', 'settings.logOut') })
}

function openSheet(language: Language = 'en'): HTMLElement {
  fireEvent.click(rowButton(language))
  return screen.getByRole('dialog', { name: translated(language, 'settings.deleteAccountSheet.title') })
}

function keepNameRadio(sheet: HTMLElement, language: Language = 'en'): HTMLElement {
  return within(sheet).getByRole('radio', { name: translated(language, 'settings.deleteAccountSheet.keepName') })
}

function formerMemberRadio(sheet: HTMLElement, language: Language = 'en'): HTMLElement {
  return within(sheet).getByRole('radio', { name: translated(language, 'settings.deleteAccountSheet.formerMember') })
}

function confirmButton(sheet: HTMLElement, language: Language = 'en'): HTMLElement {
  return within(sheet).getByRole('button', { name: translated(language, 'settings.deleteAccountSheet.confirm') })
}

function cancelButton(sheet: HTMLElement, language: Language = 'en'): HTMLElement {
  return within(sheet).getByRole('button', { name: translated(language, 'settlements.cancel') })
}

function warningSheet(): HTMLElement {
  return screen.getByRole('dialog', { name: outboxText('en', 'outbox.logOutTitle') })
}

function pressLogOutAnyway(): void {
  fireEvent.click(within(warningSheet()).getByRole('button', { name: outboxText('en', 'outbox.logOutAnyway') }))
}

function pressStay(): void {
  fireEvent.click(within(warningSheet()).getByRole('button', { name: outboxText('en', 'outbox.stay') }))
}

function comesBefore(first: HTMLElement, second: HTMLElement): boolean {
  return Boolean(first.compareDocumentPosition(second) & Node.DOCUMENT_POSITION_FOLLOWING)
}

async function watchForAClose(): Promise<void> {
  await act(async () => {
    await new Promise((resolve) => setTimeout(resolve, millisecondsToWatchForAClose))
  })
}

function holdTheDeleteRequest() {
  const answer = createDeferred<Response>()
  const fetchMock = vi.fn<typeof fetch>(() => answer.promise)
  vi.stubGlobal('fetch', fetchMock)
  return { answer, fetchMock }
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
  vi.restoreAllMocks()
})

describe('SettingsScreen, Delete my account row', () => {
  it.each(people)('shows the Delete my account button to %s', async (_name, me) => {
    await renderSettings({ me })

    expect(rowButton()).toBeTruthy()
  })

  it.each(languages)('shows the button in %s', async (language) => {
    await renderSettings({ language })

    expect(rowButton(language)).toBeTruthy()
  })

  it.each(people)('shows exactly one Delete my account button to %s', async (_name, me) => {
    await renderSettings({ me })

    expect(screen.getAllByRole('button', { name: translated('en', 'settings.deleteAccount') })).toHaveLength(1)
  })

  it('draws the button in the danger style', async () => {
    await renderSettings()

    expect(Array.from(rowButton().classList)).toEqual(expect.arrayContaining(['text-danger', 'min-h-13']))
  })

  it('is a button and not a link, because it opens a question', async () => {
    await renderSettings()

    expect(screen.queryByRole('link', { name: translated('en', 'settings.deleteAccount') })).toBeNull()
  })

  it.each(people)('puts the button after every other row and before Log out for %s', async (_name, me) => {
    await renderSettings({ me })
    const button = rowButton()
    const rowsBefore = [
      screen.getByRole('button', { name: translated('en', 'settings.downloadData') }),
      screen.getByText(translated('en', 'settings.downloadDataHint')),
      screen.getByRole('link', { name: translated('en', 'common.terms') }),
      screen.getByRole('link', { name: translated('en', 'common.privacy') }),
    ]

    for (const row of rowsBefore) {
      expect(comesBefore(row, button)).toBe(true)
    }
    expect(comesBefore(button, logOutButton())).toBe(true)
  })

  it('puts the button after the Statistics link of an admin', async () => {
    await renderSettings({ me: testAdminMe })

    expect(comesBefore(screen.getByRole('link', { name: translated('en', 'adminStats.title') }), rowButton())).toBe(true)
  })

  it.each([true, false])('puts the button after the password link when hasPassword is %s', async (hasPassword) => {
    await renderSettings({ me: { ...testMe, hasPassword } })
    const passwordLink = screen.getByRole('link', {
      name: translated('en', hasPassword ? 'auth.changePassword.title' : 'auth.setPassword.title'),
    })

    expect(comesBefore(passwordLink, rowButton())).toBe(true)
  })

  it('keeps Log out as the very last button', async () => {
    await renderSettings()

    const buttons = screen.getAllByRole('button')

    expect(buttons.at(-1)).toBe(logOutButton())
    expect(buttons.at(-2)).toBe(rowButton())
  })

  it.each(people)('sends no request, opens no sheet and shows no toast before the tap for %s', async (_name, me) => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))

    await renderSettings({ me })

    expect(fetchMock).not.toHaveBeenCalled()
    expect(screen.queryByRole('dialog')).toBeNull()
    expect(toast.success).not.toHaveBeenCalled()
    expect(toast.error).not.toHaveBeenCalled()
  })
})

describe('SettingsScreen, the Delete my account sheet', () => {
  it.each(people)('opens with one tap and sends no request for %s', async (_name, me) => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    await renderSettings({ me })

    const sheet = openSheet()

    expect(sheet).toBeTruthy()
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it.each(languages)('shows the name label, both choices with their hints, the confirm button and Cancel in %s', async (language) => {
    await renderSettings({ language })

    const sheet = openSheet(language)

    expect(within(sheet).getByRole('radiogroup', { name: translated(language, 'settings.deleteAccountSheet.nameLabel') })).toBeTruthy()
    expect(keepNameRadio(sheet, language)).toBeTruthy()
    expect(formerMemberRadio(sheet, language)).toBeTruthy()
    expect(within(sheet).getByText(translated(language, 'settings.deleteAccountSheet.keepNameHint'))).toBeTruthy()
    expect(within(sheet).getByText(translated(language, 'settings.deleteAccountSheet.formerMemberHint'))).toBeTruthy()
    expect(confirmButton(sheet, language)).toBeTruthy()
    expect(cancelButton(sheet, language)).toBeTruthy()
  })

  it('lists the choices in the order Keep my name, then Show as Former member', async () => {
    await renderSettings()
    const sheet = openSheet()

    expect(within(sheet).getAllByRole('radio')).toEqual([keepNameRadio(sheet), formerMemberRadio(sheet)])
  })

  it('preselects Keep my name', async () => {
    await renderSettings()
    const sheet = openSheet()

    expect(isChecked(keepNameRadio(sheet))).toBe(true)
    expect(isChecked(formerMemberRadio(sheet))).toBe(false)
  })

  it('moves the selection to Show as Former member with one tap, and back', async () => {
    await renderSettings()
    const sheet = openSheet()

    fireEvent.click(formerMemberRadio(sheet))

    expect(isChecked(formerMemberRadio(sheet))).toBe(true)
    expect(isChecked(keepNameRadio(sheet))).toBe(false)
    fireEvent.click(keepNameRadio(sheet))
    expect(isChecked(keepNameRadio(sheet))).toBe(true)
    expect(isChecked(formerMemberRadio(sheet))).toBe(false)
  })

  it.each(languages)('tells a person with an account that logging in within 30 days cancels the deletion (%s)', async (language) => {
    await renderSettings({ language })

    const sheet = openSheet(language)

    expect(within(sheet).getByText(translated(language, 'settings.deleteAccountSheet.whatHappens'))).toBeTruthy()
    expect(within(sheet).queryByText(translated(language, 'settings.deleteAccountSheet.whatHappensGuest'))).toBeNull()
  })

  it.each(languages)('tells a guest that it cannot be undone (%s)', async (language) => {
    await renderSettings({ me: testGuestMe, language })

    const sheet = openSheet(language)

    expect(within(sheet).getByText(translated(language, 'settings.deleteAccountSheet.whatHappensGuest'))).toBeTruthy()
    expect(within(sheet).queryByText(translated(language, 'settings.deleteAccountSheet.whatHappens'))).toBeNull()
  })

  it('draws the confirm button in the danger style', async () => {
    await renderSettings()
    const sheet = openSheet()

    expect(Array.from(confirmButton(sheet).classList)).toContain('text-danger')
  })

  it('has the confirm button enabled and not busy before it is tapped', async () => {
    await renderSettings()
    const sheet = openSheet()

    expect(confirmButton(sheet).matches(':disabled')).toBe(false)
    expect(confirmButton(sheet).getAttribute('aria-busy')).not.toBe('true')
  })

  it('closes with Cancel, sends no request and keeps the person signed in', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    const { queryClient, router } = await renderSettings()
    const sheet = openSheet()

    fireEvent.click(cancelButton(sheet))

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).toBeNull()
    })
    expect(fetchMock).not.toHaveBeenCalled()
    expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
    expect(router.state.location.pathname).toBe(routes.settings)
    expect(toast.success).not.toHaveBeenCalled()
  })

  it('closes with Escape and sends no request', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    const { queryClient } = await renderSettings()
    const sheet = openSheet()

    fireEvent.keyDown(sheet, { key: 'Escape' })

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).toBeNull()
    })
    expect(fetchMock).not.toHaveBeenCalled()
    expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
  })

  it('closes when the backdrop is tapped and sends no request', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    const { queryClient } = await renderSettings()
    openSheet()

    dismissSheetByTappingBackdrop()

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).toBeNull()
    })
    expect(fetchMock).not.toHaveBeenCalled()
    expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
  })

  it('selects Keep my name again each time the sheet opens', async () => {
    await renderSettings()
    const firstSheet = openSheet()
    fireEvent.click(formerMemberRadio(firstSheet))
    fireEvent.click(cancelButton(firstSheet))
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).toBeNull()
    })

    const secondSheet = openSheet()

    expect(isChecked(keepNameRadio(secondSheet))).toBe(true)
    expect(isChecked(formerMemberRadio(secondSheet))).toBe(false)
  })

  it('sends the default Keep my name after the sheet was reopened following a Former member choice', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    await renderSettings()
    const firstSheet = openSheet()
    fireEvent.click(formerMemberRadio(firstSheet))
    fireEvent.click(cancelButton(firstSheet))
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).toBeNull()
    })
    const secondSheet = openSheet()

    fireEvent.click(confirmButton(secondSheet))

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })
    expect(sentRequest(fetchMock).body).toEqual({ nameChoice: 'KeepName' })
  })
})

describe('SettingsScreen, confirming the deletion', () => {
  it.each(people)('sends POST /api/me/delete with KeepName by default for %s', async (_name, me) => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    await renderSettings({ me })
    const sheet = openSheet()

    fireEvent.click(confirmButton(sheet))

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })
    expect(sentRequest(fetchMock)).toMatchObject({
      url: endpoints.meDelete,
      method: 'POST',
      body: { nameChoice: 'KeepName' },
    })
  })

  it('sends FormerMember when Show as Former member was chosen', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    await renderSettings()
    const sheet = openSheet()
    fireEvent.click(formerMemberRadio(sheet))

    fireEvent.click(confirmButton(sheet))

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })
    expect(sentRequest(fetchMock).body).toEqual({ nameChoice: 'FormerMember' })
  })

  it('sends KeepName when the choice was changed to Former member and back', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    await renderSettings()
    const sheet = openSheet()
    fireEvent.click(formerMemberRadio(sheet))
    fireEvent.click(keepNameRadio(sheet))

    fireEvent.click(confirmButton(sheet))

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })
    expect(sentRequest(fetchMock).body).toEqual({ nameChoice: 'KeepName' })
  })

  it('sends only the delete request and does not call the log-out endpoint', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    await renderSettings()
    const sheet = openSheet()

    fireEvent.click(confirmButton(sheet))
    await waitFor(() => {
      expect(toast.success).toHaveBeenCalled()
    })

    expect(fetchMock.mock.calls.map(([input]) => String(input))).toEqual([endpoints.meDelete])
  })

  it('disables the confirm button and marks it busy while the server has not answered', async () => {
    stubFetchThatNeverAnswers()
    await renderSettings()
    const sheet = openSheet()

    fireEvent.click(confirmButton(sheet))

    await waitFor(() => {
      expect(confirmButton(sheet).matches(':disabled')).toBe(true)
    })
    expect(confirmButton(sheet).getAttribute('aria-busy')).toBe('true')
  })

  it('cleans up nothing and shows no toast while the server has not answered', async () => {
    stubFetchThatNeverAnswers()
    const { queryClient, router } = await renderSettings()
    const sheet = openSheet()

    fireEvent.click(confirmButton(sheet))
    await waitFor(() => {
      expect(confirmButton(sheet).matches(':disabled')).toBe(true)
    })

    expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
    expect(queryClient.getQueryData(groupsKey)).toEqual(groups)
    expect(router.state.location.pathname).toBe(routes.settings)
    expect(toast.success).not.toHaveBeenCalled()
    expect(toast.error).not.toHaveBeenCalled()
  })

  it('sends one request even when the confirm button is tapped twice quickly', async () => {
    const { answer, fetchMock } = holdTheDeleteRequest()
    await renderSettings()
    const sheet = openSheet()

    fireEvent.click(confirmButton(sheet))
    fireEvent.click(confirmButton(sheet))
    await act(async () => {
      answer.resolve(new Response(null, { status: 204 }))
    })

    await waitFor(() => {
      expect(toast.success).toHaveBeenCalledOnce()
    })
    expect(fetchMock).toHaveBeenCalledOnce()
  })
})

describe('SettingsScreen, after the server deleted the account', () => {
  it.each(languages)('shows the Deleted, log in within 30 days toast once to a person with an account (%s)', async (language) => {
    stubFetch(new Response(null, { status: 204 }))
    await renderSettings({ language })
    const sheet = openSheet(language)

    fireEvent.click(confirmButton(sheet, language))

    await waitFor(() => {
      expect(toast.success).toHaveBeenCalledWith(translated(language, 'settings.deleteAccountDone'))
    })
    expect(toast.success).toHaveBeenCalledOnce()
    expect(toast.error).not.toHaveBeenCalled()
  })

  it.each(languages)('shows the short Deleted toast once to a guest (%s)', async (language) => {
    stubFetch(new Response(null, { status: 204 }))
    await renderSettings({ me: testGuestMe, language })
    const sheet = openSheet(language)

    fireEvent.click(confirmButton(sheet, language))

    await waitFor(() => {
      expect(toast.success).toHaveBeenCalledWith(translated(language, 'settings.deleteAccountDoneGuest'))
    })
    expect(toast.success).toHaveBeenCalledOnce()
  })

  it.each(people)('goes to the welcome screen for %s', async (_name, me) => {
    stubFetch(new Response(null, { status: 204 }))
    const { router } = await renderSettings({ me })
    const sheet = openSheet()

    fireEvent.click(confirmButton(sheet))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.welcome)
    })
    expect(await screen.findByText(welcomePage)).toBeTruthy()
  })

  it('shows the toast only after the server answered', async () => {
    const { answer, fetchMock } = holdTheDeleteRequest()
    await renderSettings()
    const sheet = openSheet()
    fireEvent.click(confirmButton(sheet))
    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })
    expect(toast.success).not.toHaveBeenCalled()

    await act(async () => {
      answer.resolve(new Response(null, { status: 204 }))
    })

    await waitFor(() => {
      expect(toast.success).toHaveBeenCalledOnce()
    })
  })

  it('marks nobody as signed in and empties the cached data, like Log out does', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { queryClient } = await renderSettings()
    const sheet = openSheet()

    fireEvent.click(confirmButton(sheet))

    await waitFor(() => {
      expect(queryClient.getQueryData(meQueryKey)).toBeNull()
    })
    expect(queryClient.getQueryData(groupsKey)).toBeUndefined()
    expect(queryClient.getMutationCache().getAll()).toEqual([])
  })

  it('deletes the saved copy from the browser, like Log out does', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const persister = createTestPersister()
    const { queryClient } = await renderSettings({ persister })
    await persister.persistClient({
      timestamp: Date.now(),
      buster: savedQueryCacheVersion,
      clientState: dehydrate(queryClient),
    })
    const sheet = openSheet()

    fireEvent.click(confirmButton(sheet))

    await waitFor(() => {
      expect(toast.success).toHaveBeenCalled()
    })
    expect(await persister.restoreClient()).toBeUndefined()
  })
})

describe('SettingsScreen, when the deletion fails', () => {
  const failures = [
    ['ACCOUNT_DELETE_NAME_CHOICE_INVALID', problemResponse(400, 'ACCOUNT_DELETE_NAME_CHOICE_INVALID')],
    ['RATE_LIMITED', problemResponse(429, 'RATE_LIMITED')],
  ] as const

  const failuresInEachLanguage = languages.flatMap((language) =>
    failures.map(([code, answer]) => [language, code, answer] as const),
  )

  it.each(failuresInEachLanguage)('shows the %s error toast for %s', async (language, code, answer) => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    stubFetch(answer.clone())
    await renderSettings({ language })
    const sheet = openSheet(language)

    fireEvent.click(confirmButton(sheet, language))

    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledWith(translated(language, `errors.${code}`))
    })
  })

  it('shows the network toast when the server cannot be reached', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    stubFetch(new TypeError('Failed to fetch'))
    await renderSettings()
    const sheet = openSheet()

    fireEvent.click(confirmButton(sheet))

    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.network'))
    })
  })

  it('shows the generic toast when the server fails without a reason', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    stubFetch(new Response(null, { status: 500 }))
    await renderSettings()
    const sheet = openSheet()

    fireEvent.click(confirmButton(sheet))

    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.generic'))
    })
  })

  it.each([
    ['a refused name choice', problemResponse(400, 'ACCOUNT_DELETE_NAME_CHOICE_INVALID')],
    ['a network failure', new TypeError('Failed to fetch')],
    ['a server error', new Response(null, { status: 500 })],
  ])('keeps the sheet open, the person signed in, the cache and the screen after %s', async (_name, failure) => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    stubFetch(failure)
    const persister = createTestPersister()
    const { queryClient, router } = await renderSettings({ persister })
    await persister.persistClient({
      timestamp: Date.now(),
      buster: savedQueryCacheVersion,
      clientState: dehydrate(queryClient),
    })
    const sheet = openSheet()

    fireEvent.click(confirmButton(sheet))
    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledOnce()
    })
    await watchForAClose()

    expect(screen.getByRole('dialog', { name: translated('en', 'settings.deleteAccountSheet.title') })).toBeTruthy()
    expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
    expect(queryClient.getQueryData(groupsKey)).toEqual(groups)
    expect(router.state.location.pathname).toBe(routes.settings)
    expect(toast.success).not.toHaveBeenCalled()
    expect(await persister.restoreClient()).toBeDefined()
  })

  it('keeps the unsent changes in the browser after a failure', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    stubFetch(new Response(null, { status: 500 }))
    await renderSettings({ saved: twoWaiting() })
    fireEvent.click(rowButton())
    fireEvent.click(confirmButton(screen.getByRole('dialog', { name: translated('en', 'settings.deleteAccountSheet.title') })))
    pressLogOutAnyway()

    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledOnce()
    })

    expect(storedIds(testMe.id)).toEqual(['a', 'b'])
  })

  it('enables the confirm button again and sends the second tap after a failure', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    const fetchMock = stubFetch(new Response(null, { status: 500 }), new Response(null, { status: 204 }))
    const { router } = await renderSettings()
    const sheet = openSheet()
    fireEvent.click(confirmButton(sheet))
    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledOnce()
    })
    await waitFor(() => {
      expect(confirmButton(sheet).matches(':disabled')).toBe(false)
    })
    expect(confirmButton(sheet).getAttribute('aria-busy')).not.toBe('true')

    fireEvent.click(confirmButton(sheet))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.welcome)
    })
    expect(fetchMock).toHaveBeenCalledTimes(2)
    expect(toast.success).toHaveBeenCalledWith(translated('en', 'settings.deleteAccountDone'))
  })
})

describe('SettingsScreen, deleting with changes not sent yet', () => {
  function confirmDeletion(): void {
    fireEvent.click(confirmButton(screen.getByRole('dialog', { name: translated('en', 'settings.deleteAccountSheet.title') })))
  }

  it('shows the same warning as Log out instead of deleting at once, and sends no request', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    await renderSettings({ saved: twoWaiting() })
    openSheet()

    confirmDeletion()

    expect(warningSheet()).toBeTruthy()
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('tells how many changes would be lost, and offers Log out anyway and Stay', async () => {
    stubFetch(new Response(null, { status: 204 }))
    await renderSettings({ saved: twoWaiting() })
    openSheet()

    confirmDeletion()

    const sheet = warningSheet()
    expect(within(sheet).getByText(outboxText('en', 'outbox.logOutBody', 2))).toBeTruthy()
    expect(within(sheet).getByRole('button', { name: outboxText('en', 'outbox.logOutAnyway') })).toBeTruthy()
    expect(within(sheet).getByRole('button', { name: outboxText('en', 'outbox.stay') })).toBeTruthy()
  })

  it('shows no warning and sends the request at once when nothing is waiting', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    await renderSettings()
    openSheet()

    confirmDeletion()

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })
    expect(screen.queryByRole('dialog', { name: outboxText('en', 'outbox.logOutTitle') })).toBeNull()
  })

  it('sends the request with the chosen name after Log out anyway', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    await renderSettings({ saved: twoWaiting() })
    const sheet = openSheet()
    fireEvent.click(formerMemberRadio(sheet))
    confirmDeletion()

    pressLogOutAnyway()

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })
    expect(sentRequest(fetchMock)).toMatchObject({
      url: endpoints.meDelete,
      method: 'POST',
      body: { nameChoice: 'FormerMember' },
    })
  })

  it('sends no request, keeps the person signed in and removes nothing after Stay', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    const { queryClient } = await renderSettings({ saved: twoWaiting() })
    openSheet()
    confirmDeletion()

    pressStay()
    await watchForAClose()

    expect(fetchMock).not.toHaveBeenCalled()
    expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
    expect(storedIds(testMe.id)).toEqual(['a', 'b'])
  })

  it('removes the counted changes one by one after the deletion and does not delete the whole queue', async () => {
    const { answer, fetchMock } = holdTheDeleteRequest()
    await renderSettings({ saved: twoWaiting() })
    openSheet()
    confirmDeletion()
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

  it('keeps a change that was queued while the delete request was still running, because it was never counted', async () => {
    const { answer, fetchMock } = holdTheDeleteRequest()
    const { outboxSender } = await renderSettings({ saved: twoWaiting() })
    openSheet()
    confirmDeletion()
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

  it('shows the Deleted toast and goes to the welcome screen after Log out anyway', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { router } = await renderSettings({ saved: twoWaiting() })
    openSheet()
    confirmDeletion()

    pressLogOutAnyway()

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.welcome)
    })
    expect(toast.success).toHaveBeenCalledWith(translated('en', 'settings.deleteAccountDone'))
  })

  it('removes only the counted changes of a guest too, and warns a guest with the same sheet', async () => {
    const { answer, fetchMock } = holdTheDeleteRequest()
    const { outboxSender } = await renderSettings({ me: testGuestMe, saved: [queuedExpense('a')] })
    openSheet()
    confirmDeletion()
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
      expect(storedIds(testGuestMe.id)).toEqual(['queued-meanwhile'])
    })
    expect(fakeIdbKeyval.del).not.toHaveBeenCalled()
  })

  it('still ends deleted, logs the error and shows no error toast when the queued changes cannot be removed from the browser', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    const { answer, fetchMock } = holdTheDeleteRequest()
    const { queryClient } = await renderSettings({ saved: twoWaiting() })
    openSheet()
    confirmDeletion()
    pressLogOutAnyway()
    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledOnce()
    })
    const failure = new Error('IndexedDB write failed')
    fakeIdbKeyval.update.mockRejectedValue(failure)

    await act(async () => {
      answer.resolve(new Response(null, { status: 204 }))
    })

    await waitFor(() => {
      expect(consoleError.mock.calls.some((call) => call.includes(failure))).toBe(true)
    })
    expect(queryClient.getQueryData(meQueryKey)).toBeNull()
    expect(toast.error).not.toHaveBeenCalled()
    expect(toast.success).toHaveBeenCalledWith(translated('en', 'settings.deleteAccountDone'))
  })
})
