import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import type { OwnerToolsGroup } from '@/core/services/ownerTools/ownerToolsService'
import { expectDisabledWhilePending } from '@/test/formTestHelpers'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { closedTripGroup, tripGroup } from '@/test/ownerToolsTestData'
import {
  answerWith,
  buttonIn,
  buttonNamesIn,
  cancelButtonIn,
  confirmButtonIn,
  findGroupCard,
  findGroupNumbered,
  findSheet,
  findToolsTitle,
  openTab,
  reasonFieldIn,
  renderOwnerTools,
  requestsToUrl,
  toolsUrls,
  typeReason,
  waitForNoSheet,
  type ToolsAnswerFor,
} from '@/test/ownerToolsTestHelpers'
import { neverAnswer, networkFailureAnswer, problemAnswer } from '@/test/requestTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupUrl = `/api/admin/tools/groups/${tripGroup.id}`
const groupGet = `GET ${groupUrl}`
const closePost = `POST ${groupUrl}/close`
const reopenPost = `POST ${groupUrl}/reopen`
const reason = 'Reported by email'

interface OpenOptions {
  language?: Language
  group?: OwnerToolsGroup
  answers?: Record<string, ToolsAnswerFor>
}

async function showGroup(options: OpenOptions = {}) {
  const language = options.language ?? 'en'
  const rendered = await renderOwnerTools({
    language,
    answers: { [groupGet]: answerWith(options.group ?? tripGroup), ...options.answers },
  })
  await findToolsTitle(language)
  openTab('groups', language)
  findGroupNumbered(tripGroup.id, language)
  const card = await findGroupCard((options.group ?? tripGroup).name)
  return { ...rendered, card }
}

describe('finding a group in the owner tools', () => {
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

  describe('what is sent', () => {
    it('sends one GET to the address of the group when Find is pressed', async () => {
      const { fetchMock } = await showGroup()

      expect(toolsUrls(fetchMock, 'GET')).toEqual([groupUrl])
    })

    it('sends the number without the spaces around it', async () => {
      const { fetchMock } = await renderOwnerTools({ answers: { [groupGet]: answerWith(tripGroup) } })
      await findToolsTitle()
      openTab('groups')

      findGroupNumbered(`  ${tripGroup.id}  `)

      await waitFor(() => {
        expect(toolsUrls(fetchMock, 'GET')).toEqual([groupUrl])
      })
    })

    it.each(['', '   '])('does not call the server for the number %j', async (text) => {
      const { fetchMock } = await renderOwnerTools({ answers: { [groupGet]: answerWith(tripGroup) } })
      await findToolsTitle()
      openTab('groups')

      findGroupNumbered(text)
      await new Promise((resolve) => setTimeout(resolve, 30))

      expect(toolsUrls(fetchMock, 'GET')).toEqual([])
    })

    it('asks nothing while the number is only typed and Find is not pressed', async () => {
      const { fetchMock } = await renderOwnerTools({ answers: { [groupGet]: answerWith(tripGroup) } })
      await findToolsTitle()
      openTab('groups')

      fireEvent.change(screen.getByLabelText(translated('en', 'ownerTools.groups.searchLabel')), {
        target: { value: tripGroup.id },
      })

      expect(toolsUrls(fetchMock, 'GET')).toEqual([])
    })
  })

  describe('the card', () => {
    it('shows the emoji, the name, the owner and the number of members', async () => {
      const { card } = await showGroup()

      expect(within(card).getByText(tripGroup.emoji)).toBeTruthy()
      expect(within(card).getByText(tripGroup.name)).toBeTruthy()
      expect(within(card).getByText(translated('en', 'ownerTools.groups.owner', { name: tripGroup.ownerName }))).toBeTruthy()
      expect(within(card).getByText(translated('en', 'ownerTools.groups.members_other', { count: '4' }))).toBeTruthy()
    })

    it.each([
      [1, 'ownerTools.groups.members_one'],
      [2, 'ownerTools.groups.members_other'],
      [10, 'ownerTools.groups.members_other'],
    ])('writes %i members with the form %s', async (memberCount, key) => {
      const { card } = await showGroup({ group: { ...tripGroup, memberCount } })

      expect(within(card).getByText(translated('en', key, { count: String(memberCount) }))).toBeTruthy()
    })

    it.each([
      [1, 'ownerTools.groups.members_one'],
      [2, 'ownerTools.groups.members_other'],
      [11, 'ownerTools.groups.members_other'],
      [21, 'ownerTools.groups.members_one'],
    ])('writes %i members with the form %s in Macedonian', async (memberCount, key) => {
      const { card } = await showGroup({ language: 'mk', group: { ...tripGroup, memberCount } })

      expect(within(card).getByText(translated('mk', key, { count: String(memberCount) }))).toBeTruthy()
    })

    it.each([
      ['an open group', tripGroup, 'ownerTools.groups.status.open'],
      ['a group that is being closed', { ...tripGroup, status: 'Closing' as const }, 'ownerTools.groups.status.closing'],
      ['a finished group', { ...tripGroup, status: 'Finished' as const }, 'ownerTools.groups.status.finished'],
      ['a group that the owner of Kvit closed', closedTripGroup, 'ownerTools.groups.status.closedByOwner'],
      ['a finished group that the owner of Kvit closed', { ...closedTripGroup, status: 'Finished' as const }, 'ownerTools.groups.status.closedByOwner'],
      ['a deleted group', { ...tripGroup, isDeleted: true }, 'ownerTools.groups.status.deleted'],
    ])('names the status of %s with %s', async (_name, group, key) => {
      const { card } = await showGroup({ group })

      expect(within(card).getByText(translated('en', key))).toBeTruthy()
    })

    it('shows only one status label', async () => {
      const { card } = await showGroup({ group: closedTripGroup })

      for (const key of ['open', 'closing', 'finished', 'deleted']) {
        expect(within(card).queryByText(translated('en', `ownerTools.groups.status.${key}`)), key).toBeNull()
      }
    })

    it.each(languages)('writes the status and the owner in the language of the person (%s)', async (language) => {
      const { card } = await showGroup({ language, group: closedTripGroup })

      expect(within(card).getByText(translated(language, 'ownerTools.groups.status.closedByOwner'))).toBeTruthy()
      expect(within(card).getByText(translated(language, 'ownerTools.groups.owner', { name: tripGroup.ownerName }))).toBeTruthy()
    })

    it('has Close group and no Reopen group for a group that is not closed by the owner of Kvit', async () => {
      const { card } = await showGroup()

      expect(buttonNamesIn(card)).toEqual([translated('en', 'ownerTools.groups.close')])
    })

    it('has Reopen group and no Close group for a group that the owner of Kvit closed', async () => {
      const { card } = await showGroup({ group: closedTripGroup })

      expect(buttonNamesIn(card)).toEqual([translated('en', 'ownerTools.groups.reopen')])
    })

    it('replaces the card when another number is found', async () => {
      const otherGroup = { ...tripGroup, id: '8d2e5f1b-4c63-4b8f-9e7a-2f3b4c5d6e7f', name: 'Flat 4B' }
      await renderOwnerTools({
        answers: {
          [groupGet]: answerWith(tripGroup),
          [`GET /api/admin/tools/groups/${otherGroup.id}`]: answerWith(otherGroup),
        },
      })
      await findToolsTitle()
      openTab('groups')
      findGroupNumbered(tripGroup.id)
      await findGroupCard(tripGroup.name)

      findGroupNumbered(otherGroup.id)

      expect(await findGroupCard(otherGroup.name)).toBeTruthy()
      await waitFor(() => {
        expect(screen.queryByRole('region', { name: tripGroup.name })).toBeNull()
      })
    })
  })

  describe('when the group is not found or the server does not answer', () => {
    it('shows the translated OWNER_GROUP_NOT_FOUND text inline, shows no card and no toast', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderOwnerTools({ answers: { [groupGet]: problemAnswer(404, 'OWNER_GROUP_NOT_FOUND') } })
      await findToolsTitle()
      openTab('groups')

      findGroupNumbered(tripGroup.id)

      expect(await screen.findByText(translated('en', 'errors.OWNER_GROUP_NOT_FOUND'))).toBeTruthy()
      expect(screen.queryByRole('region', { name: tripGroup.name })).toBeNull()
      expect(vi.mocked(toast.error)).not.toHaveBeenCalled()
    })

    it.each(languages)('writes that text in the language of the person (%s)', async (language) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderOwnerTools({ language, answers: { [groupGet]: problemAnswer(404, 'OWNER_GROUP_NOT_FOUND') } })
      await findToolsTitle(language)
      openTab('groups', language)

      findGroupNumbered(tripGroup.id, language)

      expect(await screen.findByText(translated(language, 'errors.OWNER_GROUP_NOT_FOUND'))).toBeTruthy()
    })

    it('shows the network text when the server cannot be reached', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderOwnerTools({ answers: { [groupGet]: networkFailureAnswer() } })
      await findToolsTitle()
      openTab('groups')

      findGroupNumbered(tripGroup.id)

      expect(await screen.findByText(translated('en', 'errors.network'))).toBeTruthy()
    })

    it('removes the error text when a group is found afterwards', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      let calls = 0
      await renderOwnerTools({
        answers: {
          [groupGet]: () => {
            calls += 1
            return calls === 1 ? problemAnswer(404, 'OWNER_GROUP_NOT_FOUND')() : Response.json(tripGroup)
          },
        },
      })
      await findToolsTitle()
      openTab('groups')
      findGroupNumbered(tripGroup.id)
      await screen.findByText(translated('en', 'errors.OWNER_GROUP_NOT_FOUND'))

      findGroupNumbered(tripGroup.id)

      expect(await findGroupCard(tripGroup.name)).toBeTruthy()
      expect(screen.queryByText(translated('en', 'errors.OWNER_GROUP_NOT_FOUND'))).toBeNull()
    })

    it('shows a loading status and no card while the answer is on its way', async () => {
      await renderOwnerTools({ answers: { [groupGet]: neverAnswer() } })
      await findToolsTitle()
      openTab('groups')

      findGroupNumbered(tripGroup.id)

      expect(await screen.findByRole('status')).toBeTruthy()
      expect(screen.queryByRole('region', { name: tripGroup.name })).toBeNull()
    })
  })
})

interface GroupActionCase {
  name: string
  group: OwnerToolsGroup
  after: OwnerToolsGroup
  buttonKey: string
  titleKey: string
  bodyKey: string
  post: string
  doneKey: string
  statusAfterKey: string
  buttonAfterKey: string
}

const groupActionCases: GroupActionCase[] = [
  {
    name: 'Close group',
    group: tripGroup,
    after: closedTripGroup,
    buttonKey: 'ownerTools.groups.close',
    titleKey: 'ownerTools.sheet.closeGroup.title',
    bodyKey: 'ownerTools.sheet.closeGroup.body',
    post: closePost,
    doneKey: 'ownerTools.done.closeGroup',
    statusAfterKey: 'ownerTools.groups.status.closedByOwner',
    buttonAfterKey: 'ownerTools.groups.reopen',
  },
  {
    name: 'Reopen group',
    group: closedTripGroup,
    after: tripGroup,
    buttonKey: 'ownerTools.groups.reopen',
    titleKey: 'ownerTools.sheet.reopenGroup.title',
    bodyKey: 'ownerTools.sheet.reopenGroup.body',
    post: reopenPost,
    doneKey: 'ownerTools.done.reopenGroup',
    statusAfterKey: 'ownerTools.groups.status.open',
    buttonAfterKey: 'ownerTools.groups.close',
  },
]

async function openGroupSheet(testCase: GroupActionCase, postAnswer?: ToolsAnswerFor, language: Language = 'en') {
  let current = testCase.group
  const rendered = await showGroup({
    language,
    group: testCase.group,
    answers: {
      [groupGet]: () => Response.json(current),
      [testCase.post]:
        postAnswer ??
        (() => {
          current = testCase.after
          return new Response(null, { status: 204 })
        }),
    },
  })
  fireEvent.click(buttonIn(rendered.card, translated(language, testCase.buttonKey)))
  const sheet = await findSheet(testCase.titleKey, testCase.group.name, language)
  return { ...rendered, sheet }
}

describe.each(groupActionCases)('the $name sheet of the owner tools', (testCase) => {
  const postUrl = testCase.post.slice('POST '.length)

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

  it('is titled with the name of the group, explains what happens and has a disabled Confirm and a Cancel', async () => {
    const { sheet, fetchMock } = await openGroupSheet(testCase)

    expect(within(sheet).getByText(translated('en', testCase.bodyKey))).toBeTruthy()
    expect(reasonFieldIn(sheet).value).toBe('')
    expect(confirmButtonIn(sheet).disabled).toBe(true)
    expect(cancelButtonIn(sheet)).toBeTruthy()
    expect(requestsToUrl(fetchMock, 'POST', postUrl)).toEqual([])
  })

  it('is written in Macedonian for a person who uses Macedonian', async () => {
    const { sheet } = await openGroupSheet(testCase, undefined, 'mk')

    expect(within(sheet).getByText(translated('mk', testCase.bodyKey))).toBeTruthy()
    expect(confirmButtonIn(sheet, 'mk').disabled).toBe(true)
  })

  it.each(['', '   '])('keeps Confirm disabled for the reason %j', async (text) => {
    const { sheet } = await openGroupSheet(testCase)

    typeReason(sheet, text)

    expect(confirmButtonIn(sheet).disabled).toBe(true)
  })

  it.each(['x', 'x'.repeat(500)])('enables Confirm for a reason of %# characters up to 500', async (text) => {
    const { sheet } = await openGroupSheet(testCase)

    typeReason(sheet, text)

    expect(confirmButtonIn(sheet).disabled).toBe(false)
  })

  it('keeps Confirm disabled for a reason of 501 characters', async () => {
    const { sheet } = await openGroupSheet(testCase)

    typeReason(sheet, 'x'.repeat(501))

    expect(confirmButtonIn(sheet).disabled).toBe(true)
  })

  it('closes the sheet without a request when Cancel is pressed', async () => {
    const { sheet, fetchMock } = await openGroupSheet(testCase)
    typeReason(sheet, reason)

    fireEvent.click(cancelButtonIn(sheet))

    await waitForNoSheet()
    expect(requestsToUrl(fetchMock, 'POST', postUrl)).toEqual([])
  })

  it('sends one POST with the trimmed reason as JSON, closes the sheet and shows the success toast', async () => {
    const { sheet, fetchMock } = await openGroupSheet(testCase)
    typeReason(sheet, `  ${reason}  `)

    fireEvent.click(confirmButtonIn(sheet))

    await waitForNoSheet()
    const posts = requestsToUrl(fetchMock, 'POST', postUrl)
    expect(posts).toHaveLength(1)
    expect(posts[0].contentType).toBe('application/json')
    expect(posts[0].body).toEqual({ reason })
    expect(vi.mocked(toast.success)).toHaveBeenCalledOnce()
    expect(vi.mocked(toast.success)).toHaveBeenCalledWith(translated('en', testCase.doneKey))
  })

  it('asks for the group once more and shows its new status and the other button', async () => {
    const { sheet, fetchMock } = await openGroupSheet(testCase)
    typeReason(sheet, reason)

    fireEvent.click(confirmButtonIn(sheet))
    await waitForNoSheet()

    await waitFor(() => {
      expect(requestsToUrl(fetchMock, 'GET', groupUrl)).toHaveLength(2)
    })
    await waitFor(() => {
      const card = screen.getByRole('region', { name: tripGroup.name })
      expect(within(card).getByText(translated('en', testCase.statusAfterKey))).toBeTruthy()
    })
    expect(buttonNamesIn(screen.getByRole('region', { name: tripGroup.name }))).toEqual([
      translated('en', testCase.buttonAfterKey),
    ])
  })

  it('disables Confirm while the server has not answered and sends one request when it is pressed again', async () => {
    const { sheet, fetchMock } = await openGroupSheet(testCase, neverAnswer())
    typeReason(sheet, reason)
    const confirm = confirmButtonIn(sheet)

    fireEvent.click(confirm)
    await expectDisabledWhilePending(confirm)
    fireEvent.click(confirm)

    expect(requestsToUrl(fetchMock, 'POST', postUrl)).toHaveLength(1)
  })

  it.each([
    [400, 'OWNER_REASON_INVALID'],
    [403, 'GROUP_CLOSED_BY_ADMIN'],
    [404, 'OWNER_GROUP_NOT_FOUND'],
  ])('shows the translated text of %i %s as an error toast and keeps the sheet open', async (status, code) => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    const { sheet } = await openGroupSheet(testCase, problemAnswer(status, code))
    typeReason(sheet, reason)

    fireEvent.click(confirmButtonIn(sheet))

    await waitFor(() => {
      expect(vi.mocked(toast.error)).toHaveBeenCalledWith(translated('en', `errors.${code}`))
    })
    expect(screen.getByRole('dialog', { name: translated('en', testCase.titleKey, { name: testCase.group.name }) })).toBeTruthy()
    expect(vi.mocked(toast.success)).not.toHaveBeenCalled()
  })
})
