import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { Language } from '@/core/i18n/language'
import type { OwnerToolsLogEntry, OwnerToolsPerson } from '@/core/services/ownerTools/ownerToolsService'
import { expectDisabledWhilePending } from '@/test/formTestHelpers'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { anaPerson, logEntry, suspendedPerson } from '@/test/ownerToolsTestData'
import {
  buttonIn,
  cancelButtonIn,
  confirmButtonIn,
  findPersonCard,
  findSheet,
  findToolsTitle,
  openTab,
  reasonFieldIn,
  renderOwnerTools,
  requestsToUrl,
  searchPeopleFor,
  typeReason,
  waitForNoSheet,
  type ToolsAnswerFor,
} from '@/test/ownerToolsTestHelpers'
import { neverAnswer, networkFailureAnswer, problemAnswer } from '@/test/requestTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const peopleGet = 'GET /api/admin/tools/people'
const logGet = 'GET /api/admin/tools/log'
const peopleUrl = '/api/admin/tools/people?query=xx'
const reason = 'Reported by three people'

interface ActionCase {
  name: string
  person: OwnerToolsPerson
  buttonKey: string
  titleKey: string
  bodyKey: string
  action: string
  doneKey: string
  after: OwnerToolsPerson
}

const actionCases: ActionCase[] = [
  {
    name: 'Suspend',
    person: anaPerson,
    buttonKey: 'ownerTools.suspend',
    titleKey: 'ownerTools.sheet.suspend.title',
    bodyKey: 'ownerTools.sheet.suspend.body',
    action: 'suspend',
    doneKey: 'ownerTools.done.suspend',
    after: { ...anaPerson, isSuspended: true },
  },
  {
    name: 'Unsuspend',
    person: suspendedPerson,
    buttonKey: 'ownerTools.unsuspend',
    titleKey: 'ownerTools.sheet.unsuspend.title',
    bodyKey: 'ownerTools.sheet.unsuspend.body',
    action: 'unsuspend',
    doneKey: 'ownerTools.done.unsuspend',
    after: { ...suspendedPerson, isSuspended: false },
  },
  {
    name: 'Erase name',
    person: anaPerson,
    buttonKey: 'ownerTools.eraseName',
    titleKey: 'ownerTools.sheet.eraseName.title',
    bodyKey: 'ownerTools.sheet.eraseName.body',
    action: 'erase-name',
    doneKey: 'ownerTools.done.eraseName',
    after: { ...anaPerson, isDeleted: true, email: null },
  },
]

function postKey(testCase: ActionCase): string {
  return `POST /api/admin/tools/people/${testCase.person.id}/${testCase.action}`
}

function postUrl(testCase: ActionCase): string {
  return postKey(testCase).slice('POST '.length)
}

interface Live {
  people: OwnerToolsPerson[]
  log: OwnerToolsLogEntry[]
}

interface OpenOptions {
  language?: Language
  postAnswer?: ToolsAnswerFor
}

async function openSheet(testCase: ActionCase, options: OpenOptions = {}) {
  const language = options.language ?? 'en'
  const live: Live = { people: [testCase.person], log: [] }
  const rendered = await renderOwnerTools({
    language,
    answers: {
      [peopleGet]: () => Response.json(live.people),
      [logGet]: () => Response.json(live.log),
      [postKey(testCase)]:
        options.postAnswer ??
        (() => {
          live.people = [testCase.after]
          live.log = [{ ...logEntry, reason }]
          return new Response(null, { status: 204 })
        }),
    },
  })
  await findToolsTitle(language)
  searchPeopleFor('xx', language)
  const card = await findPersonCard(testCase.person.displayName)
  fireEvent.click(buttonIn(card, translated(language, testCase.buttonKey)))
  const sheet = await findSheet(testCase.titleKey, testCase.person.displayName, language)
  return { ...rendered, sheet, live }
}

describe.each(actionCases)('the $name sheet of the owner tools', (testCase) => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.clearAllMocks()
    stubDeviceColorScheme('light')
    stubGoogleSignIn()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('when it opens', () => {
    it('is titled with the name of the person and explains what happens', async () => {
      const { sheet } = await openSheet(testCase)

      expect(within(sheet).getByText(translated('en', testCase.bodyKey))).toBeTruthy()
    })

    it('has an empty reason field, a Confirm button that is disabled and a Cancel button', async () => {
      const { sheet } = await openSheet(testCase)

      expect(reasonFieldIn(sheet).value).toBe('')
      expect(confirmButtonIn(sheet).disabled).toBe(true)
      expect(cancelButtonIn(sheet)).toBeTruthy()
    })

    it('sends nothing to the server yet', async () => {
      const { fetchMock } = await openSheet(testCase)

      expect(requestsToUrl(fetchMock, 'POST', postUrl(testCase))).toEqual([])
    })

    it('is titled in Macedonian for a person who uses Macedonian', async () => {
      const { sheet } = await openSheet(testCase, { language: 'mk' })

      expect(within(sheet).getByText(translated('mk', testCase.bodyKey))).toBeTruthy()
      expect(reasonFieldIn(sheet, 'mk')).toBeTruthy()
      expect(confirmButtonIn(sheet, 'mk').disabled).toBe(true)
    })
  })

  describe('the reason', () => {
    it.each(['', ' ', '   \n  '])('keeps Confirm disabled for the reason %j, because it is empty once trimmed', async (text) => {
      const { sheet } = await openSheet(testCase)

      typeReason(sheet, text)

      expect(confirmButtonIn(sheet).disabled).toBe(true)
    })

    it.each(['x', reason, 'x'.repeat(500), `  ${'x'.repeat(500)}  `])('enables Confirm for a reason that is 1 to 500 characters once trimmed (%#)', async (text) => {
      const { sheet } = await openSheet(testCase)

      typeReason(sheet, text)

      expect(confirmButtonIn(sheet).disabled).toBe(false)
    })

    it.each(['x'.repeat(501), `  ${'x'.repeat(501)}  `])('keeps Confirm disabled for a reason longer than 500 characters once trimmed (%#)', async (text) => {
      const { sheet } = await openSheet(testCase)

      typeReason(sheet, text)

      expect(confirmButtonIn(sheet).disabled).toBe(true)
    })

    it('disables Confirm again when the reason is cleared', async () => {
      const { sheet } = await openSheet(testCase)
      typeReason(sheet, reason)

      typeReason(sheet, '')

      expect(confirmButtonIn(sheet).disabled).toBe(true)
    })

    it('starts empty again when the sheet is opened a second time', async () => {
      const { sheet } = await openSheet(testCase)
      typeReason(sheet, reason)
      fireEvent.click(cancelButtonIn(sheet))
      await waitForNoSheet()

      fireEvent.click(buttonIn(await findPersonCard(testCase.person.displayName), translated('en', testCase.buttonKey)))
      const secondSheet = await findSheet(testCase.titleKey, testCase.person.displayName)

      expect(reasonFieldIn(secondSheet).value).toBe('')
    })
  })

  describe('Cancel', () => {
    it('closes the sheet and sends nothing', async () => {
      const { sheet, fetchMock } = await openSheet(testCase)
      typeReason(sheet, reason)

      fireEvent.click(cancelButtonIn(sheet))

      await waitForNoSheet()
      expect(requestsToUrl(fetchMock, 'POST', postUrl(testCase))).toEqual([])
      expect(vi.mocked(toast.success)).not.toHaveBeenCalled()
    })
  })

  describe('Confirm', () => {
    it('sends one POST to the address of the action with the reason as JSON', async () => {
      const { sheet, fetchMock } = await openSheet(testCase)
      typeReason(sheet, reason)

      fireEvent.click(confirmButtonIn(sheet))

      await waitFor(() => {
        expect(requestsToUrl(fetchMock, 'POST', postUrl(testCase))).toHaveLength(1)
      })
      const [request] = requestsToUrl(fetchMock, 'POST', postUrl(testCase))
      expect(request.contentType).toBe('application/json')
      expect(request.body).toEqual({ reason })
    })

    it('sends the reason without the spaces around it', async () => {
      const { sheet, fetchMock } = await openSheet(testCase)
      typeReason(sheet, `  ${reason}  `)

      fireEvent.click(confirmButtonIn(sheet))

      await waitFor(() => {
        expect(requestsToUrl(fetchMock, 'POST', postUrl(testCase))).toHaveLength(1)
      })
      expect(requestsToUrl(fetchMock, 'POST', postUrl(testCase))[0].body).toEqual({ reason })
    })

    it('closes the sheet and shows a success toast with the name when the server answered', async () => {
      const { sheet } = await openSheet(testCase)
      typeReason(sheet, reason)

      fireEvent.click(confirmButtonIn(sheet))

      await waitForNoSheet()
      expect(vi.mocked(toast.success)).toHaveBeenCalledOnce()
      expect(vi.mocked(toast.success)).toHaveBeenCalledWith(
        translated('en', testCase.doneKey, { name: testCase.person.displayName }),
      )
      expect(vi.mocked(toast.error)).not.toHaveBeenCalled()
    })

    it('asks for the same search once more, so the card shows what changed', async () => {
      const { sheet, fetchMock } = await openSheet(testCase)
      typeReason(sheet, reason)

      fireEvent.click(confirmButtonIn(sheet))

      await waitFor(() => {
        expect(requestsToUrl(fetchMock, 'GET', peopleUrl)).toHaveLength(2)
      })
    })

    it('disables Confirm while the server has not answered and sends one request when it is pressed again', async () => {
      const { sheet, fetchMock } = await openSheet(testCase, { postAnswer: neverAnswer() })
      typeReason(sheet, reason)
      const confirm = confirmButtonIn(sheet)

      fireEvent.click(confirm)
      await expectDisabledWhilePending(confirm)
      fireEvent.click(confirm)

      expect(requestsToUrl(fetchMock, 'POST', postUrl(testCase))).toHaveLength(1)
    })
  })

  describe('when the server refuses or does not answer', () => {
    it.each([
      [400, 'OWNER_REASON_INVALID'],
      [403, 'OWNER_CANNOT_ACT_ON_ADMIN'],
      [404, 'OWNER_PERSON_NOT_FOUND'],
    ])('shows the translated text of %i %s as an error toast and keeps the sheet open', async (status, code) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { sheet } = await openSheet(testCase, { postAnswer: problemAnswer(status, code) })
      typeReason(sheet, reason)

      fireEvent.click(confirmButtonIn(sheet))

      await waitFor(() => {
        expect(vi.mocked(toast.error)).toHaveBeenCalledWith(translated('en', `errors.${code}`))
      })
      expect(screen.getByRole('dialog', { name: translated('en', testCase.titleKey, { name: testCase.person.displayName }) })).toBeTruthy()
      expect(vi.mocked(toast.success)).not.toHaveBeenCalled()
    })

    it('shows the network text as an error toast and keeps the typed reason', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { sheet } = await openSheet(testCase, { postAnswer: networkFailureAnswer() })
      typeReason(sheet, reason)

      fireEvent.click(confirmButtonIn(sheet))

      await waitFor(() => {
        expect(vi.mocked(toast.error)).toHaveBeenCalledWith(translated('en', 'errors.network'))
      })
      expect(reasonFieldIn(sheet).value).toBe(reason)
    })

    it('lets the person press Confirm again after a refusal, and the second try works', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      let tries = 0
      const { sheet, fetchMock } = await openSheet(testCase, {
        postAnswer: () => {
          tries += 1
          return tries === 1 ? problemAnswer(400, 'OWNER_REASON_INVALID')() : new Response(null, { status: 204 })
        },
      })
      typeReason(sheet, reason)
      fireEvent.click(confirmButtonIn(sheet))
      await waitFor(() => {
        expect(vi.mocked(toast.error)).toHaveBeenCalled()
      })

      fireEvent.click(confirmButtonIn(sheet))

      await waitForNoSheet()
      expect(requestsToUrl(fetchMock, 'POST', postUrl(testCase))).toHaveLength(2)
      expect(vi.mocked(toast.success)).toHaveBeenCalledOnce()
    })
  })
})

describe('what an action does to the card and to the log', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.clearAllMocks()
    stubDeviceColorScheme('light')
    stubGoogleSignIn()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  const suspendCase = actionCases[0]
  const unsuspendCase = actionCases[1]
  const eraseCase = actionCases[2]

  it('shows the Suspended badge and Unsuspend instead of Suspend after a person was suspended', async () => {
    const { sheet } = await openSheet(suspendCase)
    typeReason(sheet, reason)

    fireEvent.click(confirmButtonIn(sheet))
    await waitForNoSheet()

    const card = await findPersonCard(anaPerson.displayName)
    await waitFor(() => {
      expect(within(card).getByText(translated('en', 'ownerTools.badge.suspended'))).toBeTruthy()
    })
    expect(within(card).getByRole('button', { name: translated('en', 'ownerTools.unsuspend') })).toBeTruthy()
    expect(within(card).queryByRole('button', { name: translated('en', 'ownerTools.suspend') })).toBeNull()
  })

  it('takes the Suspended badge away and shows Suspend again after a person was unsuspended', async () => {
    const { sheet } = await openSheet(unsuspendCase)
    typeReason(sheet, reason)

    fireEvent.click(confirmButtonIn(sheet))
    await waitForNoSheet()

    const card = await findPersonCard(suspendedPerson.displayName)
    await waitFor(() => {
      expect(within(card).queryByText(translated('en', 'ownerTools.badge.suspended'))).toBeNull()
    })
    expect(within(card).getByRole('button', { name: translated('en', 'ownerTools.suspend') })).toBeTruthy()
  })

  it('shows the Deleted badge and no Erase name after a name was erased', async () => {
    const { sheet } = await openSheet(eraseCase)
    typeReason(sheet, reason)

    fireEvent.click(confirmButtonIn(sheet))
    await waitForNoSheet()

    const card = await findPersonCard(anaPerson.displayName)
    await waitFor(() => {
      expect(within(card).getByText(translated('en', 'ownerTools.badge.deleted'))).toBeTruthy()
    })
    expect(within(card).queryByRole('button', { name: translated('en', 'ownerTools.eraseName') })).toBeNull()
  })

  it('shows the new line in the log when the Log tab is opened afterwards', async () => {
    const { sheet } = await openSheet(suspendCase)
    typeReason(sheet, reason)
    fireEvent.click(confirmButtonIn(sheet))
    await waitForNoSheet()

    openTab('log')

    expect(await screen.findByText(reason)).toBeTruthy()
  })

  it('does not ask for the people again after Cancel', async () => {
    const { sheet, fetchMock } = await openSheet(suspendCase)

    fireEvent.click(cancelButtonIn(sheet))
    await waitForNoSheet()

    expect(requestsToUrl(fetchMock, 'GET', peopleUrl)).toHaveLength(1)
  })
})
