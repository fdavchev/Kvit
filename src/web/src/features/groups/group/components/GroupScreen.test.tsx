import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { Group } from '@/core/services/groups/groupsService'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import { stubFetchThatNeverAnswers } from '@/test/apiTestHelpers'
import { expectDisabledWhilePending, typeInto } from '@/test/formTestHelpers'
import { groupOf, testGroup, testInviteToken } from '@/test/groupTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import {
  jsonAnswer,
  networkFailureAnswer,
  noContentAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
  stubFetchByRequest,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import {
  abortError,
  letPendingWorkFinish,
  removeShareAndClipboard,
  stubClipboard,
  stubShare,
} from '@/test/shareTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { shownToastTexts } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'
import { GroupScreen } from './GroupScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupPath = `/api/groups/${testGroup.id}`
const membersPath = `${groupPath}/members`
const inviteUrl = `${window.location.origin}/join/${testInviteToken}`

interface RenderOptions {
  language?: Language
  group?: Group
  answers?: Record<string, AnswerFactory>
}

async function renderGroup(options: RenderOptions = {}) {
  const group = options.group ?? testGroup
  const language = options.language ?? 'en'
  const fetchMock = stubFetchByRequest({
    [`GET /api/groups/${group.id}`]: jsonAnswer(group),
    ...options.answers,
  })
  const rendered = await renderRoutesWithProviders(
    [
      { path: '/groups/:groupId', element: <GroupScreen /> },
      { path: '/groups/:groupId/settings', element: <p>group settings page</p> },
      { path: '/groups/:groupId/members', element: <p>members page</p> },
      { path: routes.groups, element: <p>groups page</p> },
    ],
    routes.group(group.id),
    { language, seedCache: seedMe({ ...testMe, language }) },
  )
  return { fetchMock, ...rendered }
}

async function showsGroupScreen(name: string = testGroup.name): Promise<void> {
  await screen.findByRole('heading', { level: 1, name })
}

function addNameButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'group.addName') })
}

function shareButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'group.shareLink') })
}

function sheet(language: Language = 'en'): HTMLElement {
  return screen.getByRole('dialog', { name: translated(language, 'addName.title') })
}

function sheetAddButton(language: Language = 'en'): HTMLElement {
  return within(sheet(language)).getByRole('button', {
    name: translated(language, 'addName.submit'),
  })
}

async function openSheet(language: Language = 'en'): Promise<void> {
  fireEvent.click(addNameButton(language))
  await screen.findByRole('dialog', { name: translated(language, 'addName.title') })
}

function typeName(name: string, language: Language = 'en'): void {
  typeInto(translated(language, 'addName.label'), name)
}

describe('GroupScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
    removeShareAndClipboard()
  })

  describe('loading and failing', () => {
    it('shows the loading spinner while the group is being asked', async () => {
      stubFetchThatNeverAnswers()

      await renderRoutesWithProviders(
        [{ path: '/groups/:groupId', element: <GroupScreen /> }],
        routes.group(testGroup.id),
        { seedCache: seedMe(testMe) },
      )

      expect(screen.getByRole('status')).toBeTruthy()
    })

    it.each(languages)('shows the GROUP_NOT_FOUND message when the group does not exist or the person is not in it (%s)', async (language) => {
      await renderGroup({
        language,
        answers: { [`GET ${groupPath}`]: problemAnswer(404, 'GROUP_NOT_FOUND') },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated(language, 'errors.GROUP_NOT_FOUND'),
      )
    })

    it.each(languages)('shows the group-not-found message for a 404 without an error code (%s)', async (language) => {
      await renderGroup({
        language,
        answers: { [`GET ${groupPath}`]: () => new Response(null, { status: 404 }) },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated(language, 'errors.GROUP_NOT_FOUND'),
      )
    })

    it.each(languages)('shows a Go to Groups link to the groups list and no retry button when the group is not found (%s)', async (language) => {
      await renderGroup({
        language,
        answers: { [`GET ${groupPath}`]: problemAnswer(404, 'GROUP_NOT_FOUND') },
      })
      await screen.findByRole('alert')

      const link = screen.getByRole('link', { name: translated(language, 'groups.goToGroups') })

      expect(link.getAttribute('href')).toBe(routes.groups)
      expect(
        screen.queryByRole('button', { name: translated(language, 'common.retry') }),
      ).toBeNull()
    })

    it('shows the Go to Groups link and no retry button for a 404 without an error code', async () => {
      await renderGroup({
        answers: { [`GET ${groupPath}`]: () => new Response(null, { status: 404 }) },
      })
      await screen.findByRole('alert')

      expect(
        screen.getByRole('link', { name: translated('en', 'groups.goToGroups') }).getAttribute('href'),
      ).toBe(routes.groups)
      expect(screen.queryByRole('button', { name: translated('en', 'common.retry') })).toBeNull()
    })

    it('opens the groups list when the Go to Groups link is pressed', async () => {
      const { router } = await renderGroup({
        answers: { [`GET ${groupPath}`]: problemAnswer(404, 'GROUP_NOT_FOUND') },
      })

      fireEvent.click(await screen.findByRole('link', { name: translated('en', 'groups.goToGroups') }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groups)
      })
    })

    it.each([
      ['a server error', () => new Response(null, { status: 500 })],
      ['a network failure', networkFailureAnswer()],
    ])('shows no Go to Groups link after %s', async (_label, answer) => {
      await renderGroup({ answers: { [`GET ${groupPath}`]: answer } })
      await screen.findByRole('alert')

      expect(screen.queryByRole('link', { name: translated('en', 'groups.goToGroups') })).toBeNull()
    })

    it('shows the generic message with a retry button after a server error', async () => {
      await renderGroup({
        answers: { [`GET ${groupPath}`]: () => new Response(null, { status: 500 }) },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated('en', 'errors.generic'),
      )
      expect(screen.getByRole('button', { name: translated('en', 'common.retry') })).toBeTruthy()
    })

    it('shows the network message when the server cannot be reached', async () => {
      await renderGroup({ answers: { [`GET ${groupPath}`]: networkFailureAnswer() } })

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated('en', 'errors.network'),
      )
    })

    it('asks for the group again and shows it when the retry button is pressed', async () => {
      let tries = 0
      await renderGroup({
        answers: {
          [`GET ${groupPath}`]: () => {
            tries += 1
            return tries === 1 ? new Response(null, { status: 500 }) : Response.json(testGroup)
          },
        },
      })

      fireEvent.click(await screen.findByRole('button', { name: translated('en', 'common.retry') }))

      await showsGroupScreen()
      expect(screen.queryByRole('alert')).toBeNull()
    })
  })

  describe('the header', () => {
    it('has a back button to the groups list', async () => {
      await renderGroup()
      await showsGroupScreen()

      const back = screen.getByRole('link', { name: translated('en', 'common.back') })

      expect(back.getAttribute('href')).toBe(routes.groups)
    })

    it('shows the name of the group as the heading', async () => {
      await renderGroup({ group: groupOf({ name: 'Flat 4B' }) })

      expect(await screen.findByRole('heading', { level: 1, name: 'Flat 4B' })).toBeTruthy()
    })

    it('shows the emoji of the group', async () => {
      await renderGroup({ group: groupOf({ emoji: '\u{1F355}' }) })
      await showsGroupScreen()

      expect(screen.getByText('\u{1F355}')).toBeTruthy()
    })

    it.each([
      ['en', 1, 'MKD', '1 person · MKD'],
      ['en', 3, 'EUR', '3 people · EUR'],
      ['mk', 1, 'MKD', '1 лице · MKD'],
      ['mk', 3, 'EUR', '3 лица · EUR'],
    ] as const)('shows how many people and which currency (%s, %i people, %s)', async (language, memberCount, defaultCurrency, text) => {
      await renderGroup({ language, group: groupOf({ memberCount, defaultCurrency }) })
      await showsGroupScreen()

      expect(screen.getByText(text)).toBeTruthy()
    })

    it.each(languages)('has a Group settings link to the settings of this group (%s)', async (language) => {
      await renderGroup({ language })
      await showsGroupScreen()

      const link = screen.getByRole('link', { name: translated(language, 'group.settings') })

      expect(link.getAttribute('href')).toBe(routes.groupSettings(testGroup.id))
    })

    it.each(languages)('has a Members link to the members of this group (%s)', async (language) => {
      await renderGroup({ language })
      await showsGroupScreen()

      const link = screen.getByRole('link', { name: translated(language, 'members.title') })

      expect(link.getAttribute('href')).toBe(routes.groupMembers(testGroup.id))
    })

    it('has the Members link for a member who is not the owner too', async () => {
      await renderGroup({ group: groupOf({ isOwner: false, memberCount: 3 }) })
      await showsGroupScreen()

      expect(screen.getByRole('link', { name: translated('en', 'members.title') })).toBeTruthy()
    })

    it('has the Members link when the group has several people and the Add people card is gone', async () => {
      await renderGroup({ group: groupOf({ memberCount: 4 }) })
      await showsGroupScreen()

      expect(screen.getByRole('link', { name: translated('en', 'members.title') })).toBeTruthy()
    })

    it('opens the members of the group when the Members link is pressed', async () => {
      const { router } = await renderGroup()
      await showsGroupScreen()

      fireEvent.click(screen.getByRole('link', { name: translated('en', 'members.title') }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groupMembers(testGroup.id))
      })
    })

    it('has the Group settings link for a member who is not the owner too', async () => {
      await renderGroup({ group: groupOf({ isOwner: false, memberCount: 3 }) })
      await showsGroupScreen()

      expect(screen.getByRole('link', { name: translated('en', 'group.settings') })).toBeTruthy()
    })

    it('opens the settings of the group when the Group settings link is pressed', async () => {
      const { router } = await renderGroup()
      await showsGroupScreen()

      fireEvent.click(screen.getByRole('link', { name: translated('en', 'group.settings') }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groupSettings(testGroup.id))
      })
    })

    it.each(languages)('tells that the expenses will show up soon (%s)', async (language) => {
      await renderGroup({ language })
      await showsGroupScreen()

      expect(screen.getByText(translated(language, 'group.expensesSoon'))).toBeTruthy()
    })
  })

  describe('the Add people card', () => {
    it.each(languages)('shows the card with its two lines and two buttons while the owner is alone (%s)', async (language) => {
      await renderGroup({ language })
      await showsGroupScreen()

      expect(screen.getByText(translated(language, 'group.addPeople'))).toBeTruthy()
      expect(screen.getByText(translated(language, 'group.addPeopleNoKvit'))).toBeTruthy()
      expect(screen.getByText(translated(language, 'group.addPeopleHasKvit'))).toBeTruthy()
      expect(addNameButton(language)).toBeTruthy()
      expect(shareButton(language)).toBeTruthy()
    })

    it.each([2, 3, 12])('does not show the card when the group has %i people', async (memberCount) => {
      await renderGroup({ group: groupOf({ memberCount }) })
      await showsGroupScreen()

      expect(screen.queryByText(translated('en', 'group.addPeople'))).toBeNull()
      expect(screen.queryByRole('button', { name: translated('en', 'group.addName') })).toBeNull()
      expect(screen.queryByRole('button', { name: translated('en', 'group.shareLink') })).toBeNull()
    })

    it('shows the card to a member who is not the owner while the group has one person', async () => {
      await renderGroup({ group: groupOf({ isOwner: false, memberCount: 1 }) })
      await showsGroupScreen()

      expect(shareButton()).toBeTruthy()
    })
  })

  describe('adding a name', () => {
    it('shows no sheet before Add a name is pressed', async () => {
      await renderGroup()
      await showsGroupScreen()

      expect(screen.queryByRole('dialog')).toBeNull()
    })

    it.each(languages)('opens a sheet with the name field, its hint and the Add button when Add a name is pressed (%s)', async (language) => {
      await renderGroup({ language })
      await showsGroupScreen()

      await openSheet(language)

      expect(within(sheet(language)).getByLabelText(translated(language, 'addName.label'))).toBeTruthy()
      expect(within(sheet(language)).getByText(translated(language, 'addName.hint'))).toBeTruthy()
      expect(sheetAddButton(language)).toBeTruthy()
    })

    it('sends the typed name to the members endpoint of the group', async () => {
      const { fetchMock } = await renderGroup({
        answers: { [`POST ${membersPath}`]: noContentAnswer() },
      })
      await showsGroupScreen()
      await openSheet()
      typeName('Grandma')

      fireEvent.click(sheetAddButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', membersPath)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', membersPath)[0]).toMatchObject({
        contentType: 'application/json',
        body: { name: 'Grandma' },
      })
    })

    it('sends the typed name when the form is submitted with the Enter key', async () => {
      const { fetchMock } = await renderGroup({
        answers: { [`POST ${membersPath}`]: noContentAnswer() },
      })
      await showsGroupScreen()
      await openSheet()
      typeName('Grandma')
      const form = within(sheet()).getByLabelText<HTMLInputElement>(translated('en', 'addName.label')).form
      if (form === null) {
        throw new Error('The name field is not inside a form')
      }

      fireEvent.submit(form)

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', membersPath)).toBe(1)
      })
    })

    it('closes the sheet and shows the new head count after the name is added', async () => {
      let isNameAdded = false
      await renderGroup({
        answers: {
          [`GET ${groupPath}`]: () => Response.json(isNameAdded ? groupOf({ memberCount: 2 }) : testGroup),
          [`POST ${membersPath}`]: () => {
            isNameAdded = true
            return Response.json({ id: 'new-member', name: 'Grandma' })
          },
        },
      })
      await showsGroupScreen()
      await openSheet()
      typeName('Grandma')

      fireEvent.click(sheetAddButton())

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      expect(await screen.findByText('2 people · MKD')).toBeTruthy()
    })

    it('takes the Add people card away once the group has a second person', async () => {
      let isNameAdded = false
      await renderGroup({
        answers: {
          [`GET ${groupPath}`]: () => Response.json(isNameAdded ? groupOf({ memberCount: 2 }) : testGroup),
          [`POST ${membersPath}`]: () => {
            isNameAdded = true
            return Response.json({ id: 'new-member', name: 'Grandma' })
          },
        },
      })
      await showsGroupScreen()
      await openSheet()
      typeName('Grandma')

      fireEvent.click(sheetAddButton())

      await waitFor(() => {
        expect(screen.queryByText(translated('en', 'group.addPeople'))).toBeNull()
      })
    })

    it('disables the Add button while the request waits for an answer', async () => {
      await renderGroup()
      await showsGroupScreen()
      await openSheet()
      typeName('Grandma')
      stubFetchThatNeverAnswers()

      fireEvent.click(sheetAddButton())

      await expectDisabledWhilePending(sheetAddButton())
    })

    it.each(languages)('shows the empty-name message inside the sheet and keeps it open (%s)', async (language) => {
      await renderGroup({
        language,
        answers: { [`POST ${membersPath}`]: problemAnswer(400, 'MEMBER_NAME_INVALID') },
      })
      await showsGroupScreen()
      await openSheet(language)

      fireEvent.click(sheetAddButton(language))

      expect(
        (await within(sheet(language)).findByRole('alert')).textContent,
      ).toContain(translated(language, 'errors.MEMBER_NAME_INVALID'))
    })

    it.each(languages)('shows the name-taken message with the typed name inside the sheet, keeps the sheet open and keeps the typed name (%s)', async (language) => {
      await renderGroup({
        language,
        answers: { [`POST ${membersPath}`]: problemAnswer(400, 'MEMBER_NAME_TAKEN') },
      })
      await showsGroupScreen()
      await openSheet(language)
      typeName('Marko', language)

      fireEvent.click(sheetAddButton(language))

      expect(
        (await within(sheet(language)).findByRole('alert')).textContent,
      ).toContain(translated(language, 'errors.MEMBER_NAME_TAKEN', { name: 'Marko' }))
      expect(
        within(sheet(language)).getByLabelText<HTMLInputElement>(translated(language, 'addName.label')).value,
      ).toBe('Marko')
    })

    it.each([
      ['en', 'Someone called “Marko” is already in this group.'],
      ['mk', 'Некој по име „Marko“ е веќе во групата.'],
    ] as const)('words the name-taken message exactly with the typed name between the quotes (%s)', async (language, text) => {
      await renderGroup({
        language,
        answers: { [`POST ${membersPath}`]: problemAnswer(400, 'MEMBER_NAME_TAKEN') },
      })
      await showsGroupScreen()
      await openSheet(language)
      typeName('Marko', language)

      fireEvent.click(sheetAddButton(language))

      expect((await within(sheet(language)).findByRole('alert')).textContent).toBe(text)
    })

    it('shows the name in the name-taken message as it was typed, with its capitals and punctuation', async () => {
      await renderGroup({
        answers: { [`POST ${membersPath}`]: problemAnswer(400, 'MEMBER_NAME_TAKEN') },
      })
      await showsGroupScreen()
      await openSheet()
      typeName("mARKO o'Brien & Co.")

      fireEvent.click(sheetAddButton())

      expect((await within(sheet()).findByRole('alert')).textContent).toBe(
        'Someone called “mARKO o\'Brien & Co.” is already in this group.',
      )
    })

    it('shows the network message inside the sheet when the server cannot be reached', async () => {
      await renderGroup({ answers: { [`POST ${membersPath}`]: networkFailureAnswer() } })
      await showsGroupScreen()
      await openSheet()
      typeName('Grandma')

      fireEvent.click(sheetAddButton())

      expect((await within(sheet()).findByRole('alert')).textContent).toContain(
        translated('en', 'errors.network'),
      )
    })

    it('closes the sheet when Escape is pressed', async () => {
      await renderGroup()
      await showsGroupScreen()
      await openSheet()

      fireEvent.keyDown(sheet(), { key: 'Escape' })

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
    })

    it('closes the sheet when its Close button is pressed', async () => {
      await renderGroup()
      await showsGroupScreen()
      await openSheet()

      fireEvent.click(within(sheet()).getByRole('button', { name: translated('en', 'common.close') }))

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
    })

    it('shows the field empty and without an error when the sheet is opened again', async () => {
      await renderGroup({
        answers: { [`POST ${membersPath}`]: problemAnswer(400, 'MEMBER_NAME_TAKEN') },
      })
      await showsGroupScreen()
      await openSheet()
      typeName('Marko')
      fireEvent.click(sheetAddButton())
      await within(sheet()).findByRole('alert')
      fireEvent.keyDown(sheet(), { key: 'Escape' })
      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })

      await openSheet()

      expect(
        within(sheet()).getByLabelText<HTMLInputElement>(translated('en', 'addName.label')).value,
      ).toBe('')
      expect(within(sheet()).queryByRole('alert')).toBeNull()
    })
  })

  describe('sharing the invite link', () => {
    it('opens the share menu of the phone with the invite link of the group', async () => {
      const share = stubShare(async () => {})
      await renderGroup()
      await showsGroupScreen()

      fireEvent.click(shareButton())

      await waitFor(() => {
        expect(share).toHaveBeenCalledOnce()
      })
      expect(share).toHaveBeenCalledWith(expect.objectContaining({ url: inviteUrl }))
    })

    it('uses the invite token of the group that is open', async () => {
      const share = stubShare(async () => {})
      await renderGroup({ group: groupOf({ inviteToken: 'another-token_42' }) })
      await showsGroupScreen()

      fireEvent.click(shareButton())

      await waitFor(() => {
        expect(share).toHaveBeenCalledWith(
          expect.objectContaining({ url: `${window.location.origin}/join/another-token_42` }),
        )
      })
    })

    it('does not copy the link when the share menu is available', async () => {
      stubShare(async () => {})
      const writeText = stubClipboard(async () => {})
      await renderGroup()
      await showsGroupScreen()

      fireEvent.click(shareButton())
      await letPendingWorkFinish()

      expect(writeText).not.toHaveBeenCalled()
    })

    it('shows no toast when the share menu was shared through', async () => {
      stubShare(async () => {})
      await renderGroup()
      await showsGroupScreen()

      fireEvent.click(shareButton())
      await letPendingWorkFinish()

      expect(shownToastTexts()).toEqual([])
      expect(toast.error).not.toHaveBeenCalled()
    })

    it('does nothing more when the person closes the share menu without sharing', async () => {
      const share = stubShare(async () => {
        throw abortError()
      })
      await renderGroup()
      await showsGroupScreen()

      fireEvent.click(shareButton())
      await letPendingWorkFinish()

      expect(share).toHaveBeenCalledOnce()
      expect(shownToastTexts()).toEqual([])
      expect(toast.error).not.toHaveBeenCalled()
    })

    it('shows the generic error toast when sharing fails for another reason', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      stubShare(async () => {
        throw new Error('The share menu crashed')
      })
      await renderGroup()
      await showsGroupScreen()

      fireEvent.click(shareButton())

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.generic'))
      })
    })

    it.each(languages)('copies the link to the clipboard and shows the Link copied toast when there is no share menu (%s)', async (language) => {
      const writeText = stubClipboard(async () => {})
      await renderGroup({ language })
      await showsGroupScreen()

      fireEvent.click(shareButton(language))

      await waitFor(() => {
        expect(shownToastTexts()).toContain(translated(language, 'group.linkCopied'))
      })
      expect(writeText).toHaveBeenCalledExactlyOnceWith(inviteUrl)
    })

    it('shows the generic error toast and not Link copied when the clipboard refuses the link', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      stubClipboard(async () => {
        throw new DOMException('Clipboard blocked', 'NotAllowedError')
      })
      await renderGroup()
      await showsGroupScreen()

      fireEvent.click(shareButton())

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.generic'))
      })
      expect(shownToastTexts()).not.toContain(translated('en', 'group.linkCopied'))
    })

    it('shows the generic error toast and not Link copied when the phone has neither a share menu nor a clipboard', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderGroup()
      await showsGroupScreen()

      fireEvent.click(shareButton())

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.generic'))
      })
      expect(shownToastTexts()).not.toContain(translated('en', 'group.linkCopied'))
    })
  })

  it('does not send any request besides asking for the group when nothing is pressed', async () => {
    const { fetchMock } = await renderGroup()
    await showsGroupScreen()

    expect(fetchMock.mock.calls.map(([url]) => String(url))).toEqual([groupPath])
  })
})
