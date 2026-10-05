import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { Group } from '@/core/services/groups/groupsService'
import type { GroupMember, GroupMembers } from '@/core/services/groups/membersService'
import { stubFetchThatNeverAnswers } from '@/test/apiTestHelpers'
import { typeInto } from '@/test/formTestHelpers'
import { groupOf, testGroup, testInviteToken } from '@/test/groupTestData'
import {
  anaMember,
  anaViewMembers,
  bojanRemoved,
  filipMember,
  grandmaMember,
  markoMember,
  membersOf,
  ownerViewMembers,
  petarMember,
} from '@/test/memberTestData'
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
import {
  actionOf,
  pressToastAction,
  referenceDangerToastStyling,
  referenceToastStyling,
  shownToastTexts,
  stylingOf,
  toastShownWithText,
} from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'
import { MembersScreen } from './MembersScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupPath = `/api/groups/${testGroup.id}`
const membersPath = `${groupPath}/members`
const ownerPath = `${groupPath}/owner`
const resetPath = `${groupPath}/invite/reset`
const undoResetPath = `${groupPath}/invite/undo-reset`
const leavePath = `${groupPath}/leave`

const ownerGroup = groupOf({ memberCount: 5 })
const memberGroup = groupOf({ isOwner: false, memberCount: 5 })
const newInviteToken = 'Zt7-newTokenFromTheServer_0123456789abcdefghijk'

const namesOnScreen = [
  filipMember.displayName,
  anaMember.displayName,
  markoMember.displayName,
  grandmaMember.displayName,
  petarMember.displayName,
  bojanRemoved.displayName,
]

function memberPath(member: { id: string }): string {
  return `${membersPath}/${member.id}`
}

function shownLink(token: string): string {
  return `${window.location.host}/join/${token}`
}

function inviteUrl(token: string): string {
  return `${window.location.origin}/join/${token}`
}

interface RenderOptions {
  language?: Language
  group?: Group
  members?: GroupMembers
  answers?: Record<string, AnswerFactory>
}

async function renderMembers(options: RenderOptions = {}) {
  const group = options.group ?? ownerGroup
  const members = options.members ?? ownerViewMembers
  const language = options.language ?? 'en'
  const fetchMock = stubFetchByRequest({
    [`GET ${groupPath}`]: jsonAnswer(group),
    [`GET ${membersPath}`]: jsonAnswer(members),
    ...options.answers,
  })
  const rendered = await renderRoutesWithProviders(
    [
      { path: '/groups/:groupId/members', element: <MembersScreen /> },
      { path: '/groups/:groupId', element: <p>group page</p> },
      { path: routes.groups, element: <p>groups page</p> },
    ],
    routes.groupMembers(group.id),
    { language, seedCache: seedMe({ ...testMe, language }) },
  )
  return { fetchMock, ...rendered }
}

async function showsMembers(language: Language = 'en'): Promise<void> {
  await screen.findByRole('heading', { level: 1, name: translated(language, 'members.title') })
  await screen.findByText(petarMember.displayName)
}

function rowOf(name: string): HTMLElement {
  const others = namesOnScreen.filter((other) => other !== name)
  let row: HTMLElement = screen.getByText(name)
  let parent: HTMLElement | null = row.parentElement
  while (parent !== null && !others.some((other) => parent?.textContent?.includes(other))) {
    row = parent
    parent = row.parentElement
  }
  return row
}

function optionsButton(name: string, language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'members.optionsFor', { name }) })
}

async function openOptions(name: string, language: Language = 'en'): Promise<HTMLElement> {
  fireEvent.click(optionsButton(name, language))
  return screen.findByRole('dialog', { name })
}

function sheetButton(sheet: HTMLElement, key: string, language: Language = 'en'): HTMLElement {
  return within(sheet).getByRole('button', { name: translated(language, key) })
}

function shareButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'members.invite.share') })
}

function copyButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'members.invite.copy') })
}

function resetButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'members.invite.reset') })
}

function leaveButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'groupSettings.leave') })
}

function addNameLink(language: Language = 'en'): HTMLElement {
  return screen.getByText(translated(language, 'members.addNameLink'))
}

function addNameSheet(language: Language = 'en'): HTMLElement {
  return screen.getByRole('dialog', { name: translated(language, 'addName.title') })
}

async function openAddNameSheet(language: Language = 'en'): Promise<void> {
  fireEvent.click(addNameLink(language))
  await screen.findByRole('dialog', { name: translated(language, 'addName.title') })
}

function thatsMeButtons(language: Language = 'en'): HTMLElement[] {
  return screen.queryAllByRole('button', { name: translated(language, 'members.thatsMe') })
}

function optionsButtons(): HTMLElement[] {
  return screen.queryAllByRole('button', { name: /^(Options for|Опции за) / })
}

function requestedUrls(fetchMock: { mock: { calls: unknown[][] } }): string[] {
  return fetchMock.mock.calls.map(([url]) => String(url))
}

function consoleErrorSilenced(): void {
  vi.spyOn(console, 'error').mockImplementation(() => {})
}

describe('MembersScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
    removeShareAndClipboard()
  })

  describe('loading and failing', () => {
    it('shows the loading spinner while the group and its members are being asked', async () => {
      stubFetchThatNeverAnswers()

      await renderRoutesWithProviders(
        [{ path: '/groups/:groupId/members', element: <MembersScreen /> }],
        routes.groupMembers(testGroup.id),
        { seedCache: seedMe(testMe) },
      )

      expect(screen.getByRole('status')).toBeTruthy()
    })

    it.each(languages)('shows the GROUP_NOT_FOUND message when the members of the group cannot be seen (%s)', async (language) => {
      await renderMembers({
        language,
        answers: { [`GET ${membersPath}`]: problemAnswer(404, 'GROUP_NOT_FOUND') },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated(language, 'errors.GROUP_NOT_FOUND'),
      )
    })

    it('shows the GROUP_NOT_FOUND message when the group itself cannot be seen', async () => {
      await renderMembers({
        answers: { [`GET ${groupPath}`]: problemAnswer(404, 'GROUP_NOT_FOUND') },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated('en', 'errors.GROUP_NOT_FOUND'),
      )
    })

    it('shows the generic message with a retry button after a server error', async () => {
      await renderMembers({
        answers: { [`GET ${membersPath}`]: () => new Response(null, { status: 500 }) },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated('en', 'errors.generic'),
      )
      expect(screen.getByRole('button', { name: translated('en', 'common.retry') })).toBeTruthy()
    })

    it('shows the network message when the server cannot be reached', async () => {
      await renderMembers({ answers: { [`GET ${membersPath}`]: networkFailureAnswer() } })

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated('en', 'errors.network'),
      )
    })

    it('asks for the members again and shows them when the retry button is pressed', async () => {
      let tries = 0
      await renderMembers({
        answers: {
          [`GET ${membersPath}`]: () => {
            tries += 1
            return tries === 1 ? new Response(null, { status: 500 }) : Response.json(ownerViewMembers)
          },
        },
      })

      fireEvent.click(await screen.findByRole('button', { name: translated('en', 'common.retry') }))

      await showsMembers()
      expect(screen.queryByRole('alert')).toBeNull()
    })
  })

  describe('the list of people', () => {
    it('has a back button to the group', async () => {
      await renderMembers()
      await showsMembers()

      const back = screen.getByRole('link', { name: translated('en', 'common.back') })

      expect(back.getAttribute('href')).toBe(routes.group(testGroup.id))
    })

    it.each(languages)('shows the Members heading (%s)', async (language) => {
      await renderMembers({ language })

      await showsMembers(language)
    })

    it('shows the display name of every person who is in the group', async () => {
      await renderMembers()
      await showsMembers()

      for (const member of ownerViewMembers.members) {
        expect(screen.getByText(member.displayName)).toBeTruthy()
      }
    })

    it('shows the account name of a person who took a name, not the name they took, as the row name', async () => {
      await renderMembers({ members: membersOf({ members: [filipMember, petarMember] }) })
      await showsMembers()

      expect(screen.getByText('Petar')).toBeTruthy()
      expect(screen.queryByText('Darko')).toBeNull()
    })

    it.each(languages)('shows the Owner badge on the owner only (%s)', async (language) => {
      await renderMembers({ language })
      await showsMembers(language)

      expect(within(rowOf('Filip')).getByText(translated(language, 'members.owner'))).toBeTruthy()
      expect(screen.getAllByText(translated(language, 'members.owner'))).toHaveLength(1)
    })

    it.each(languages)('shows the You badge on the row of the person looking at the screen only (%s)', async (language) => {
      await renderMembers({ language, group: memberGroup, members: anaViewMembers })
      await showsMembers(language)

      expect(within(rowOf('Ana')).getByText(translated(language, 'members.you'))).toBeTruthy()
      expect(screen.getAllByText(translated(language, 'members.you'))).toHaveLength(1)
    })

    it.each(languages)('shows Just a name on the people without an account and on nobody else (%s)', async (language) => {
      await renderMembers({ language })
      await showsMembers(language)

      const text = translated(language, 'members.nameOnly')
      expect(within(rowOf('Marko')).getByText(text)).toBeTruthy()
      expect(within(rowOf('Grandma')).getByText(text)).toBeTruthy()
      expect(within(rowOf('Ana')).queryByText(text)).toBeNull()
      expect(within(rowOf('Petar')).queryByText(text)).toBeNull()
      expect(screen.getAllByText(text)).toHaveLength(2)
    })

    it.each(languages)('shows the name that someone took under their row when the owner looks (%s)', async (language) => {
      await renderMembers({ language })
      await showsMembers(language)

      expect(
        within(rowOf('Petar')).getByText(
          translated(language, 'members.tookName', { name: 'Darko' }),
        ),
      ).toBeTruthy()
    })

    it('shows no "took the name" line under people who took no name', async () => {
      await renderMembers()
      await showsMembers()

      expect(screen.getAllByText(/Took the name/)).toHaveLength(1)
    })

    it('does not tell a member who is not the owner which name someone took', async () => {
      await renderMembers({ group: memberGroup, members: anaViewMembers })
      await showsMembers()

      expect(screen.queryByText(/Took the name/)).toBeNull()
    })
  })

  describe('adding a name', () => {
    it('shows no sheet before + Add a name is pressed', async () => {
      await renderMembers()
      await showsMembers()

      expect(screen.queryByRole('dialog')).toBeNull()
    })

    it.each(languages)('shows the + Add a name link and opens the sheet with the name field when it is pressed (%s)', async (language) => {
      await renderMembers({ language })
      await showsMembers(language)

      await openAddNameSheet(language)

      expect(
        within(addNameSheet(language)).getByLabelText(translated(language, 'addName.label')),
      ).toBeTruthy()
    })

    it('shows the + Add a name link to a member who is not the owner too', async () => {
      await renderMembers({ group: memberGroup, members: anaViewMembers })
      await showsMembers()

      expect(addNameLink()).toBeTruthy()
    })

    it('sends the typed name to the members endpoint of the group', async () => {
      const { fetchMock } = await renderMembers({
        answers: { [`POST ${membersPath}`]: noContentAnswer() },
      })
      await showsMembers()
      await openAddNameSheet()
      typeInto(translated('en', 'addName.label'), 'Nana')

      fireEvent.click(
        within(addNameSheet()).getByRole('button', { name: translated('en', 'addName.submit') }),
      )

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', membersPath)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', membersPath)[0]).toMatchObject({
        contentType: 'application/json',
        body: { name: 'Nana' },
      })
    })

    it('closes the sheet and shows the new name in the list after the name is added', async () => {
      const nanaMember: GroupMember = {
        ...markoMember,
        id: 'a1b2c3d4-0007-4aaa-8bbb-000000000007',
        name: 'Nana',
        displayName: 'Nana',
      }
      let isNameAdded = false
      await renderMembers({
        answers: {
          [`GET ${membersPath}`]: () =>
            Response.json(
              isNameAdded
                ? membersOf({ members: [...ownerViewMembers.members, nanaMember] })
                : ownerViewMembers,
            ),
          [`POST ${membersPath}`]: () => {
            isNameAdded = true
            return Response.json(nanaMember)
          },
        },
      })
      await showsMembers()
      await openAddNameSheet()
      typeInto(translated('en', 'addName.label'), 'Nana')

      fireEvent.click(
        within(addNameSheet()).getByRole('button', { name: translated('en', 'addName.submit') }),
      )

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      expect(await screen.findByText('Nana')).toBeTruthy()
    })

    it.each(languages)('shows the name-taken message with the typed name inside the sheet and keeps the sheet open (%s)', async (language) => {
      await renderMembers({
        language,
        answers: { [`POST ${membersPath}`]: problemAnswer(400, 'MEMBER_NAME_TAKEN') },
      })
      await showsMembers(language)
      await openAddNameSheet(language)
      typeInto(translated(language, 'addName.label'), 'Marko')

      fireEvent.click(
        within(addNameSheet(language)).getByRole('button', {
          name: translated(language, 'addName.submit'),
        }),
      )

      expect((await within(addNameSheet(language)).findByRole('alert')).textContent).toContain(
        translated(language, 'errors.MEMBER_NAME_TAKEN', { name: 'Marko' }),
      )
    })

    it('shows the empty-name message inside the sheet and keeps the sheet open', async () => {
      await renderMembers({
        answers: { [`POST ${membersPath}`]: problemAnswer(400, 'MEMBER_NAME_INVALID') },
      })
      await showsMembers()
      await openAddNameSheet()

      fireEvent.click(
        within(addNameSheet()).getByRole('button', { name: translated('en', 'addName.submit') }),
      )

      expect((await within(addNameSheet()).findByRole('alert')).textContent).toContain(
        translated('en', 'errors.MEMBER_NAME_INVALID'),
      )
    })
  })

  describe('the invite link', () => {
    it.each(languages)('shows the Invite link heading, the Share button and the Copy link button (%s)', async (language) => {
      await renderMembers({ language })
      await showsMembers(language)

      expect(screen.getByText(translated(language, 'members.invite.title'))).toBeTruthy()
      expect(shareButton(language)).toBeTruthy()
      expect(copyButton(language)).toBeTruthy()
    })

    it('shows the invite link of the group as host, /join/ and the token', async () => {
      await renderMembers()
      await showsMembers()

      expect(screen.getByText(shownLink(testInviteToken))).toBeTruthy()
    })

    it('shows the token of the group that is open', async () => {
      await renderMembers({ group: groupOf({ memberCount: 5, inviteToken: 'another-token_42' }) })
      await showsMembers()

      expect(screen.getByText(shownLink('another-token_42'))).toBeTruthy()
      expect(screen.queryByText(shownLink(testInviteToken))).toBeNull()
    })

    it('shows the invite card to a member who is not the owner too', async () => {
      await renderMembers({ group: memberGroup, members: anaViewMembers })
      await showsMembers()

      expect(screen.getByText(shownLink(testInviteToken))).toBeTruthy()
      expect(shareButton()).toBeTruthy()
      expect(copyButton()).toBeTruthy()
    })

    describe('Share', () => {
      it('opens the share menu of the phone with the invite link of the group', async () => {
        const share = stubShare(async () => {})
        await renderMembers()
        await showsMembers()

        fireEvent.click(shareButton())

        await waitFor(() => {
          expect(share).toHaveBeenCalledOnce()
        })
        expect(share).toHaveBeenCalledWith(
          expect.objectContaining({ url: inviteUrl(testInviteToken) }),
        )
      })

      it('uses the invite token of the group that is open', async () => {
        const share = stubShare(async () => {})
        await renderMembers({ group: groupOf({ memberCount: 5, inviteToken: 'another-token_42' }) })
        await showsMembers()

        fireEvent.click(shareButton())

        await waitFor(() => {
          expect(share).toHaveBeenCalledWith(
            expect.objectContaining({ url: inviteUrl('another-token_42') }),
          )
        })
      })

      it('shows no toast and does not copy when the share menu was shared through', async () => {
        stubShare(async () => {})
        const writeText = stubClipboard(async () => {})
        await renderMembers()
        await showsMembers()

        fireEvent.click(shareButton())
        await letPendingWorkFinish()

        expect(shownToastTexts()).toEqual([])
        expect(toast.error).not.toHaveBeenCalled()
        expect(writeText).not.toHaveBeenCalled()
      })

      it('does nothing more when the person closes the share menu without sharing', async () => {
        const share = stubShare(async () => {
          throw abortError()
        })
        await renderMembers()
        await showsMembers()

        fireEvent.click(shareButton())
        await letPendingWorkFinish()

        expect(share).toHaveBeenCalledOnce()
        expect(shownToastTexts()).toEqual([])
        expect(toast.error).not.toHaveBeenCalled()
      })

      it('shows the generic error toast when sharing fails for another reason', async () => {
        consoleErrorSilenced()
        stubShare(async () => {
          throw new Error('The share menu crashed')
        })
        await renderMembers()
        await showsMembers()

        fireEvent.click(shareButton())

        await waitFor(() => {
          expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.generic'))
        })
      })

      it.each(languages)('copies the link and shows the Link copied toast when there is no share menu (%s)', async (language) => {
        const writeText = stubClipboard(async () => {})
        await renderMembers({ language })
        await showsMembers(language)

        fireEvent.click(shareButton(language))

        await waitFor(() => {
          expect(shownToastTexts()).toContain(translated(language, 'group.linkCopied'))
        })
        expect(writeText).toHaveBeenCalledExactlyOnceWith(inviteUrl(testInviteToken))
      })

      it('shows the generic error toast and not Link copied when the phone has neither a share menu nor a clipboard', async () => {
        consoleErrorSilenced()
        await renderMembers()
        await showsMembers()

        fireEvent.click(shareButton())

        await waitFor(() => {
          expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.generic'))
        })
        expect(shownToastTexts()).not.toContain(translated('en', 'group.linkCopied'))
      })
    })

    describe('Copy link', () => {
      it.each(languages)('copies the invite link to the clipboard and shows the Link copied toast (%s)', async (language) => {
        const writeText = stubClipboard(async () => {})
        await renderMembers({ language })
        await showsMembers(language)

        fireEvent.click(copyButton(language))

        await waitFor(() => {
          expect(shownToastTexts()).toContain(translated(language, 'group.linkCopied'))
        })
        expect(writeText).toHaveBeenCalledExactlyOnceWith(inviteUrl(testInviteToken))
      })

      it('copies and does not open the share menu even when the phone has one', async () => {
        const share = stubShare(async () => {})
        const writeText = stubClipboard(async () => {})
        await renderMembers()
        await showsMembers()

        fireEvent.click(copyButton())

        await waitFor(() => {
          expect(writeText).toHaveBeenCalledOnce()
        })
        expect(share).not.toHaveBeenCalled()
      })

      it('shows the generic error toast and not Link copied when the clipboard refuses the link', async () => {
        consoleErrorSilenced()
        stubClipboard(async () => {
          throw new DOMException('Clipboard blocked', 'NotAllowedError')
        })
        await renderMembers()
        await showsMembers()

        fireEvent.click(copyButton())

        await waitFor(() => {
          expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.generic'))
        })
        expect(shownToastTexts()).not.toContain(translated('en', 'group.linkCopied'))
      })
    })

    describe('Reset link for the owner', () => {
      it.each(languages)('shows the Reset link button (%s)', async (language) => {
        await renderMembers({ language })
        await showsMembers(language)

        expect(resetButton(language)).toBeTruthy()
      })

      it('sends the reset request for the group with no body', async () => {
        const { fetchMock } = await renderMembers({
          answers: { [`POST ${resetPath}`]: jsonAnswer({ inviteToken: newInviteToken }) },
        })
        await showsMembers()

        fireEvent.click(resetButton())

        await waitFor(() => {
          expect(requestCount(fetchMock, 'POST', resetPath)).toBe(1)
        })
        expect(requestsOf(fetchMock, 'POST', resetPath)[0].body).toBeUndefined()
      })

      it('asks no "are you sure" question before resetting', async () => {
        await renderMembers({
          answers: { [`POST ${resetPath}`]: jsonAnswer({ inviteToken: newInviteToken }) },
        })
        await showsMembers()

        fireEvent.click(resetButton())

        expect(screen.queryByRole('dialog')).toBeNull()
        expect(screen.queryByRole('alertdialog')).toBeNull()
      })

      it.each(languages)('shows the Link reset toast with an Undo button for 4 seconds (%s)', async (language) => {
        await renderMembers({
          language,
          answers: { [`POST ${resetPath}`]: jsonAnswer({ inviteToken: newInviteToken }) },
        })
        await showsMembers(language)

        fireEvent.click(resetButton(language))

        const shown = await waitFor(() =>
          toastShownWithText(translated(language, 'members.invite.resetDone')),
        )
        expect(shown.options.duration).toBe(4000)
        expect(actionOf(shown).label).toBe(translated(language, 'common.undo'))
      })

      it('shows the Link reset toast in the wine-red danger style', async () => {
        const dangerStyling = referenceToastStyling(true)
        await renderMembers({
          answers: { [`POST ${resetPath}`]: jsonAnswer({ inviteToken: newInviteToken }) },
        })
        await showsMembers()

        fireEvent.click(resetButton())

        const shown = await waitFor(() =>
          toastShownWithText(translated('en', 'members.invite.resetDone')),
        )
        expect(stylingOf(shown)).toEqual(dangerStyling)
      })

      it('shows the new link in the invite card after the group is loaded again', async () => {
        let isReset = false
        await renderMembers({
          answers: {
            [`GET ${groupPath}`]: () =>
              Response.json(
                isReset ? groupOf({ memberCount: 5, inviteToken: newInviteToken }) : ownerGroup,
              ),
            [`POST ${resetPath}`]: () => {
              isReset = true
              return Response.json({ inviteToken: newInviteToken })
            },
          },
        })
        await showsMembers()

        fireEvent.click(resetButton())

        expect(await screen.findByText(shownLink(newInviteToken))).toBeTruthy()
        expect(screen.queryByText(shownLink(testInviteToken))).toBeNull()
      })

      it('goes back to the old link in the invite card when Undo is pressed on the toast', async () => {
        let isReset = false
        const { fetchMock } = await renderMembers({
          answers: {
            [`GET ${groupPath}`]: () =>
              Response.json(
                isReset ? groupOf({ memberCount: 5, inviteToken: newInviteToken }) : ownerGroup,
              ),
            [`POST ${resetPath}`]: () => {
              isReset = true
              return Response.json({ inviteToken: newInviteToken })
            },
            [`POST ${undoResetPath}`]: () => {
              isReset = false
              return Response.json({ inviteToken: testInviteToken })
            },
          },
        })
        await showsMembers()
        fireEvent.click(resetButton())
        const shown = await waitFor(() =>
          toastShownWithText(translated('en', 'members.invite.resetDone')),
        )
        await screen.findByText(shownLink(newInviteToken))

        pressToastAction(shown)

        await waitFor(() => {
          expect(requestCount(fetchMock, 'POST', undoResetPath)).toBe(1)
        })
        expect(requestsOf(fetchMock, 'POST', undoResetPath)[0].body).toBeUndefined()
        expect(await screen.findByText(shownLink(testInviteToken))).toBeTruthy()
      })

      it('does not undo the reset before Undo is pressed', async () => {
        const { fetchMock } = await renderMembers({
          answers: {
            [`POST ${resetPath}`]: jsonAnswer({ inviteToken: newInviteToken }),
            [`POST ${undoResetPath}`]: jsonAnswer({ inviteToken: testInviteToken }),
          },
        })
        await showsMembers()

        fireEvent.click(resetButton())
        await waitFor(() => {
          expect(requestCount(fetchMock, 'POST', resetPath)).toBe(1)
        })

        expect(requestCount(fetchMock, 'POST', undoResetPath)).toBe(0)
      })

      it.each(languages)('shows the GROUP_NOT_OWNER error toast and no Link reset toast when the server refuses (%s)', async (language) => {
        consoleErrorSilenced()
        await renderMembers({
          language,
          answers: { [`POST ${resetPath}`]: problemAnswer(403, 'GROUP_NOT_OWNER') },
        })
        await showsMembers(language)

        fireEvent.click(resetButton(language))

        await waitFor(() => {
          expect(toast.error).toHaveBeenCalledWith(translated(language, 'errors.GROUP_NOT_OWNER'))
        })
        expect(shownToastTexts()).toEqual([])
      })

      it('shows the error toast when Undo is pressed but there is no earlier link', async () => {
        consoleErrorSilenced()
        await renderMembers({
          answers: {
            [`POST ${resetPath}`]: jsonAnswer({ inviteToken: newInviteToken }),
            [`POST ${undoResetPath}`]: problemAnswer(400, 'INVITE_NOTHING_TO_UNDO'),
          },
        })
        await showsMembers()
        fireEvent.click(resetButton())
        const shown = await waitFor(() =>
          toastShownWithText(translated('en', 'members.invite.resetDone')),
        )

        pressToastAction(shown)

        await waitFor(() => {
          expect(toast.error).toHaveBeenCalledWith(
            translated('en', 'errors.INVITE_NOTHING_TO_UNDO'),
          )
        })
      })
    })
  })

  describe('the options of a person, as the owner', () => {
    it('shows an options button on every row except the owner row', async () => {
      await renderMembers()
      await showsMembers()

      expect(optionsButtons().map((button) => button.getAttribute('aria-label'))).toEqual([
        'Options for Ana',
        'Options for Marko',
        'Options for Grandma',
        'Options for Petar',
      ])
      expect(
        screen.queryByRole('button', { name: translated('en', 'members.optionsFor', { name: 'Filip' }) }),
      ).toBeNull()
    })

    it.each(languages)('names the options button after the person (%s)', async (language) => {
      await renderMembers({ language })
      await showsMembers(language)

      expect(optionsButton('Ana', language)).toBeTruthy()
    })

    it('shows no sheet before an options button is pressed', async () => {
      await renderMembers()
      await showsMembers()

      expect(screen.queryByRole('dialog')).toBeNull()
    })

    it.each(languages)('opens a sheet titled with the display name that has Make owner and Remove from group for a person with an account (%s)', async (language) => {
      await renderMembers({ language })
      await showsMembers(language)

      const sheet = await openOptions('Ana', language)

      expect(sheetButton(sheet, 'members.makeOwner', language)).toBeTruthy()
      expect(sheetButton(sheet, 'members.remove', language)).toBeTruthy()
      expect(
        within(sheet).queryByRole('button', { name: translated(language, 'members.undoClaim') }),
      ).toBeNull()
    })

    it('offers no Make owner for a person without an account, only Remove from group', async () => {
      await renderMembers()
      await showsMembers()

      const sheet = await openOptions('Marko')

      expect(sheetButton(sheet, 'members.remove')).toBeTruthy()
      expect(
        within(sheet).queryByRole('button', { name: translated('en', 'members.makeOwner') }),
      ).toBeNull()
      expect(
        within(sheet).queryByRole('button', { name: translated('en', 'members.undoClaim') }),
      ).toBeNull()
    })

    it('offers Make owner, Undo claim and Remove from group for a person who took a name', async () => {
      await renderMembers()
      await showsMembers()

      const sheet = await openOptions('Petar')

      expect(sheetButton(sheet, 'members.makeOwner')).toBeTruthy()
      expect(sheetButton(sheet, 'members.undoClaim')).toBeTruthy()
      expect(sheetButton(sheet, 'members.remove')).toBeTruthy()
    })

    describe('Make owner', () => {
      it('sends the member id to the owner endpoint of the group', async () => {
        const { fetchMock } = await renderMembers({
          answers: { [`POST ${ownerPath}`]: noContentAnswer() },
        })
        await showsMembers()
        const sheet = await openOptions('Ana')

        fireEvent.click(sheetButton(sheet, 'members.makeOwner'))

        await waitFor(() => {
          expect(requestCount(fetchMock, 'POST', ownerPath)).toBe(1)
        })
        expect(requestsOf(fetchMock, 'POST', ownerPath)[0]).toMatchObject({
          contentType: 'application/json',
          body: { memberId: anaMember.id },
        })
      })

      it.each(languages)('shows the is-now-the-owner toast, closes the sheet and loads the members again (%s)', async (language) => {
        const { fetchMock } = await renderMembers({
          language,
          answers: { [`POST ${ownerPath}`]: noContentAnswer() },
        })
        await showsMembers(language)
        const sheet = await openOptions('Ana', language)

        fireEvent.click(sheetButton(sheet, 'members.makeOwner', language))

        await waitFor(() => {
          expect(shownToastTexts()).toContain(translated(language, 'members.ownerNow', { name: 'Ana' }))
        })
        await waitFor(() => {
          expect(screen.queryByRole('dialog')).toBeNull()
        })
        await waitFor(() => {
          expect(requestCount(fetchMock, 'GET', membersPath)).toBe(2)
        })
      })

      it.each(languages)('shows the MEMBER_NOT_ACCOUNT error toast and no success toast when the server refuses (%s)', async (language) => {
        consoleErrorSilenced()
        await renderMembers({
          language,
          answers: { [`POST ${ownerPath}`]: problemAnswer(400, 'MEMBER_NOT_ACCOUNT') },
        })
        await showsMembers(language)
        const sheet = await openOptions('Ana', language)

        fireEvent.click(sheetButton(sheet, 'members.makeOwner', language))

        await waitFor(() => {
          expect(toast.error).toHaveBeenCalledWith(
            translated(language, 'errors.MEMBER_NOT_ACCOUNT'),
          )
        })
        expect(shownToastTexts()).toEqual([])
      })

      it('shows the MEMBER_ALREADY_OWNER error toast when the person already became the owner elsewhere', async () => {
        consoleErrorSilenced()
        await renderMembers({
          answers: { [`POST ${ownerPath}`]: problemAnswer(400, 'MEMBER_ALREADY_OWNER') },
        })
        await showsMembers()
        const sheet = await openOptions('Ana')

        fireEvent.click(sheetButton(sheet, 'members.makeOwner'))

        await waitFor(() => {
          expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.MEMBER_ALREADY_OWNER'))
        })
      })
    })

    describe('Undo claim', () => {
      it('sends the undo-claim request for the row of the person with no body', async () => {
        const { fetchMock } = await renderMembers({
          answers: { [`POST ${memberPath(petarMember)}/undo-claim`]: noContentAnswer() },
        })
        await showsMembers()
        const sheet = await openOptions('Petar')

        fireEvent.click(sheetButton(sheet, 'members.undoClaim'))

        await waitFor(() => {
          expect(requestCount(fetchMock, 'POST', `${memberPath(petarMember)}/undo-claim`)).toBe(1)
        })
        expect(
          requestsOf(fetchMock, 'POST', `${memberPath(petarMember)}/undo-claim`)[0].body,
        ).toBeUndefined()
      })

      it.each(languages)('shows the stays-in-the-group toast with the account name and the name that was taken, and closes the sheet (%s)', async (language) => {
        await renderMembers({
          language,
          answers: { [`POST ${memberPath(petarMember)}/undo-claim`]: noContentAnswer() },
        })
        await showsMembers(language)
        const sheet = await openOptions('Petar', language)

        fireEvent.click(sheetButton(sheet, 'members.undoClaim', language))

        await waitFor(() => {
          expect(shownToastTexts()).toContain(
            translated(language, 'members.claimUndone', { name: 'Petar', claimed: 'Darko' }),
          )
        })
        await waitFor(() => {
          expect(screen.queryByRole('dialog')).toBeNull()
        })
      })

      it('shows the MEMBER_NOT_CLAIMED error toast and no success toast when the server refuses', async () => {
        consoleErrorSilenced()
        await renderMembers({
          answers: {
            [`POST ${memberPath(petarMember)}/undo-claim`]: problemAnswer(400, 'MEMBER_NOT_CLAIMED'),
          },
        })
        await showsMembers()
        const sheet = await openOptions('Petar')

        fireEvent.click(sheetButton(sheet, 'members.undoClaim'))

        await waitFor(() => {
          expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.MEMBER_NOT_CLAIMED'))
        })
        expect(shownToastTexts()).toEqual([])
      })
    })

    describe('Remove from group', () => {
      it('sends the delete request for the row of the person with no body', async () => {
        const { fetchMock } = await renderMembers({
          answers: { [`DELETE ${memberPath(anaMember)}`]: noContentAnswer() },
        })
        await showsMembers()
        const sheet = await openOptions('Ana')

        fireEvent.click(sheetButton(sheet, 'members.remove'))

        await waitFor(() => {
          expect(requestCount(fetchMock, 'DELETE', memberPath(anaMember))).toBe(1)
        })
        expect(requestsOf(fetchMock, 'DELETE', memberPath(anaMember))[0].body).toBeUndefined()
      })

      it('asks no "are you sure" question before removing', async () => {
        await renderMembers({
          answers: { [`DELETE ${memberPath(anaMember)}`]: noContentAnswer() },
        })
        await showsMembers()
        const sheet = await openOptions('Ana')

        fireEvent.click(sheetButton(sheet, 'members.remove'))

        expect(screen.queryByRole('alertdialog')).toBeNull()
      })

      it.each(languages)('shows the Removed toast with the name and an Undo button for 4 seconds, and closes the sheet (%s)', async (language) => {
        await renderMembers({
          language,
          answers: { [`DELETE ${memberPath(anaMember)}`]: noContentAnswer() },
        })
        await showsMembers(language)
        const sheet = await openOptions('Ana', language)

        fireEvent.click(sheetButton(sheet, 'members.remove', language))

        const shown = await waitFor(() =>
          toastShownWithText(translated(language, 'members.removed', { name: 'Ana' })),
        )
        expect(shown.options.duration).toBe(4000)
        expect(actionOf(shown).label).toBe(translated(language, 'common.undo'))
        await waitFor(() => {
          expect(screen.queryByRole('dialog')).toBeNull()
        })
      })

      it('shows the Removed toast for a person without an account too', async () => {
        await renderMembers({
          answers: { [`DELETE ${memberPath(markoMember)}`]: noContentAnswer() },
        })
        await showsMembers()
        const sheet = await openOptions('Marko')

        fireEvent.click(sheetButton(sheet, 'members.remove'))

        await waitFor(() => {
          expect(toastShownWithText(translated('en', 'members.removed', { name: 'Marko' }))).toBeTruthy()
        })
      })

      it('shows the Removed toast in the wine-red danger style', async () => {
        const dangerStyling = referenceToastStyling(true)
        await renderMembers({
          answers: { [`DELETE ${memberPath(anaMember)}`]: noContentAnswer() },
        })
        await showsMembers()
        const sheet = await openOptions('Ana')

        fireEvent.click(sheetButton(sheet, 'members.remove'))

        const shown = await waitFor(() =>
          toastShownWithText(translated('en', 'members.removed', { name: 'Ana' })),
        )
        expect(stylingOf(shown)).toEqual(dangerStyling)
      })

      it('brings the person back with let-back-in when Undo is pressed on the toast', async () => {
        const { fetchMock } = await renderMembers({
          answers: {
            [`DELETE ${memberPath(anaMember)}`]: noContentAnswer(),
            [`POST ${memberPath(anaMember)}/let-back-in`]: noContentAnswer(),
          },
        })
        await showsMembers()
        const sheet = await openOptions('Ana')
        fireEvent.click(sheetButton(sheet, 'members.remove'))
        const shown = await waitFor(() =>
          toastShownWithText(translated('en', 'members.removed', { name: 'Ana' })),
        )

        pressToastAction(shown)

        await waitFor(() => {
          expect(requestCount(fetchMock, 'POST', `${memberPath(anaMember)}/let-back-in`)).toBe(1)
        })
        expect(
          requestsOf(fetchMock, 'POST', `${memberPath(anaMember)}/let-back-in`)[0].body,
        ).toBeUndefined()
      })

      it('does not let the person back in before Undo is pressed', async () => {
        const { fetchMock } = await renderMembers({
          answers: {
            [`DELETE ${memberPath(anaMember)}`]: noContentAnswer(),
            [`POST ${memberPath(anaMember)}/let-back-in`]: noContentAnswer(),
          },
        })
        await showsMembers()
        const sheet = await openOptions('Ana')

        fireEvent.click(sheetButton(sheet, 'members.remove'))
        await waitFor(() => {
          expect(requestCount(fetchMock, 'DELETE', memberPath(anaMember))).toBe(1)
        })

        expect(requestCount(fetchMock, 'POST', `${memberPath(anaMember)}/let-back-in`)).toBe(0)
      })

      it('loads the members again after the person is removed', async () => {
        const { fetchMock } = await renderMembers({
          answers: { [`DELETE ${memberPath(anaMember)}`]: noContentAnswer() },
        })
        await showsMembers()
        const sheet = await openOptions('Ana')

        fireEvent.click(sheetButton(sheet, 'members.remove'))

        await waitFor(() => {
          expect(requestCount(fetchMock, 'GET', membersPath)).toBe(2)
        })
      })

      it.each(languages)('shows the MEMBER_NOT_FOUND error toast and no Removed toast when the server refuses (%s)', async (language) => {
        consoleErrorSilenced()
        await renderMembers({
          language,
          answers: { [`DELETE ${memberPath(anaMember)}`]: problemAnswer(404, 'MEMBER_NOT_FOUND') },
        })
        await showsMembers(language)
        const sheet = await openOptions('Ana', language)

        fireEvent.click(sheetButton(sheet, 'members.remove', language))

        await waitFor(() => {
          expect(toast.error).toHaveBeenCalledWith(translated(language, 'errors.MEMBER_NOT_FOUND'))
        })
        expect(shownToastTexts()).toEqual([])
      })

      it('shows the network error toast when the removal cannot reach the server', async () => {
        consoleErrorSilenced()
        await renderMembers({
          answers: { [`DELETE ${memberPath(anaMember)}`]: networkFailureAnswer() },
        })
        await showsMembers()
        const sheet = await openOptions('Ana')

        fireEvent.click(sheetButton(sheet, 'members.remove'))

        await waitFor(() => {
          expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.network'))
        })
      })

      it('shows an error toast when Undo is pressed but the person is gone', async () => {
        consoleErrorSilenced()
        await renderMembers({
          answers: {
            [`DELETE ${memberPath(anaMember)}`]: noContentAnswer(),
            [`POST ${memberPath(anaMember)}/let-back-in`]: problemAnswer(404, 'MEMBER_NOT_FOUND'),
          },
        })
        await showsMembers()
        const sheet = await openOptions('Ana')
        fireEvent.click(sheetButton(sheet, 'members.remove'))
        const shown = await waitFor(() =>
          toastShownWithText(translated('en', 'members.removed', { name: 'Ana' })),
        )

        pressToastAction(shown)

        await waitFor(() => {
          expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.MEMBER_NOT_FOUND'))
        })
      })
    })
  })

  describe('the Removed list, as the owner', () => {
    it.each(languages)('shows the Removed heading and each removed person with a Let back in button (%s)', async (language) => {
      await renderMembers({ language })
      await showsMembers(language)

      expect(screen.getByText(translated(language, 'members.removedTitle'))).toBeTruthy()
      expect(
        within(rowOf('Bojan')).getByRole('button', { name: translated(language, 'members.letBackIn') }),
      ).toBeTruthy()
    })

    it('shows no Removed heading when nobody was removed', async () => {
      await renderMembers({ members: membersOf({ removed: [] }) })
      await showsMembers()

      expect(screen.queryByText(translated('en', 'members.removedTitle'))).toBeNull()
      expect(screen.queryByRole('button', { name: translated('en', 'members.letBackIn') })).toBeNull()
    })

    it('shows no options button for a removed person', async () => {
      await renderMembers()
      await showsMembers()

      expect(
        screen.queryByRole('button', { name: translated('en', 'members.optionsFor', { name: 'Bojan' }) }),
      ).toBeNull()
    })

    it('sends the let-back-in request for the removed person with no body', async () => {
      const { fetchMock } = await renderMembers({
        answers: { [`POST ${memberPath(bojanRemoved)}/let-back-in`]: noContentAnswer() },
      })
      await showsMembers()

      fireEvent.click(
        within(rowOf('Bojan')).getByRole('button', { name: translated('en', 'members.letBackIn') }),
      )

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', `${memberPath(bojanRemoved)}/let-back-in`)).toBe(1)
      })
      expect(
        requestsOf(fetchMock, 'POST', `${memberPath(bojanRemoved)}/let-back-in`)[0].body,
      ).toBeUndefined()
    })

    it.each(languages)('shows the is-back-in-the-group toast and loads the members again (%s)', async (language) => {
      const { fetchMock } = await renderMembers({
        language,
        answers: { [`POST ${memberPath(bojanRemoved)}/let-back-in`]: noContentAnswer() },
      })
      await showsMembers(language)

      fireEvent.click(
        within(rowOf('Bojan')).getByRole('button', { name: translated(language, 'members.letBackIn') }),
      )

      await waitFor(() => {
        expect(shownToastTexts()).toContain(
          translated(language, 'members.backInGroup', { name: 'Bojan' }),
        )
      })
      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', membersPath)).toBe(2)
      })
    })

    it('shows the error toast and no is-back toast when the server refuses', async () => {
      consoleErrorSilenced()
      await renderMembers({
        answers: {
          [`POST ${memberPath(bojanRemoved)}/let-back-in`]: problemAnswer(404, 'MEMBER_NOT_FOUND'),
        },
      })
      await showsMembers()

      fireEvent.click(
        within(rowOf('Bojan')).getByRole('button', { name: translated('en', 'members.letBackIn') }),
      )

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.MEMBER_NOT_FOUND'))
      })
      expect(shownToastTexts()).toEqual([])
    })
  })

  describe("That's me", () => {
    it.each(languages)("shows That's me on each person without an account and on nobody else when the person can take names (%s)", async (language) => {
      await renderMembers({ language, group: memberGroup, members: anaViewMembers })
      await showsMembers(language)

      expect(thatsMeButtons(language)).toHaveLength(2)
      const text = translated(language, 'members.thatsMe')
      expect(within(rowOf('Marko')).getByRole('button', { name: text })).toBeTruthy()
      expect(within(rowOf('Grandma')).getByRole('button', { name: text })).toBeTruthy()
      expect(within(rowOf('Ana')).queryByRole('button', { name: text })).toBeNull()
      expect(within(rowOf('Petar')).queryByRole('button', { name: text })).toBeNull()
      expect(within(rowOf('Filip')).queryByRole('button', { name: text })).toBeNull()
    })

    it("shows no That's me when the person cannot take names", async () => {
      await renderMembers({
        group: memberGroup,
        members: { ...anaViewMembers, canClaimNames: false },
      })
      await showsMembers()

      expect(thatsMeButtons()).toHaveLength(0)
    })

    it("shows That's me to the owner too when the server says the owner can take names", async () => {
      await renderMembers({ members: { ...ownerViewMembers, canClaimNames: true } })
      await showsMembers()

      expect(thatsMeButtons()).toHaveLength(2)
    })

    it('sends the claim request for the row that was pressed with no body', async () => {
      const { fetchMock } = await renderMembers({
        group: memberGroup,
        members: anaViewMembers,
        answers: { [`POST ${memberPath(markoMember)}/claim`]: noContentAnswer() },
      })
      await showsMembers()

      fireEvent.click(
        within(rowOf('Marko')).getByRole('button', { name: translated('en', 'members.thatsMe') }),
      )

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', `${memberPath(markoMember)}/claim`)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', `${memberPath(markoMember)}/claim`)[0].body).toBeUndefined()
      expect(requestCount(fetchMock, 'POST', `${memberPath(grandmaMember)}/claim`)).toBe(0)
    })

    it.each(languages)('shows the You-are-now toast with the name of the row and loads the members again (%s)', async (language) => {
      const { fetchMock } = await renderMembers({
        language,
        group: memberGroup,
        members: anaViewMembers,
        answers: { [`POST ${memberPath(grandmaMember)}/claim`]: noContentAnswer() },
      })
      await showsMembers(language)

      fireEvent.click(
        within(rowOf('Grandma')).getByRole('button', { name: translated(language, 'members.thatsMe') }),
      )

      await waitFor(() => {
        expect(shownToastTexts()).toContain(
          translated(language, 'members.claimed', { name: 'Grandma' }),
        )
      })
      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', membersPath)).toBe(2)
      })
    })

    it.each(languages)('shows the MEMBER_CANNOT_CLAIM error toast and no success toast when the server refuses (%s)', async (language) => {
      consoleErrorSilenced()
      await renderMembers({
        language,
        group: memberGroup,
        members: anaViewMembers,
        answers: {
          [`POST ${memberPath(markoMember)}/claim`]: problemAnswer(400, 'MEMBER_CANNOT_CLAIM'),
        },
      })
      await showsMembers(language)

      fireEvent.click(
        within(rowOf('Marko')).getByRole('button', { name: translated(language, 'members.thatsMe') }),
      )

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated(language, 'errors.MEMBER_CANNOT_CLAIM'))
      })
      expect(shownToastTexts()).toEqual([])
    })
  })

  describe('as a member who is not the owner', () => {
    it('shows no Reset link button', async () => {
      await renderMembers({ group: memberGroup, members: anaViewMembers })
      await showsMembers()

      expect(
        screen.queryByRole('button', { name: translated('en', 'members.invite.reset') }),
      ).toBeNull()
    })

    it('shows no options button on any row', async () => {
      await renderMembers({ group: memberGroup, members: anaViewMembers })
      await showsMembers()

      expect(optionsButtons()).toHaveLength(0)
    })

    it('shows no Removed list even when the answer lists removed people', async () => {
      await renderMembers({
        group: memberGroup,
        members: { ...anaViewMembers, removed: [bojanRemoved] },
      })
      await showsMembers()

      expect(screen.queryByText(translated('en', 'members.removedTitle'))).toBeNull()
      expect(screen.queryByText('Bojan')).toBeNull()
      expect(screen.queryByRole('button', { name: translated('en', 'members.letBackIn') })).toBeNull()
    })

    it.each(languages)('has a Leave group button (%s)', async (language) => {
      await renderMembers({ language, group: memberGroup, members: anaViewMembers })
      await showsMembers(language)

      expect(leaveButton(language)).toBeTruthy()
    })

    it('sends the leave request for the group with no body and goes to the groups list', async () => {
      const { fetchMock, router } = await renderMembers({
        group: memberGroup,
        members: anaViewMembers,
        answers: { [`POST ${leavePath}`]: noContentAnswer() },
      })
      await showsMembers()

      fireEvent.click(leaveButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groups)
      })
      expect(requestCount(fetchMock, 'POST', leavePath)).toBe(1)
      expect(requestsOf(fetchMock, 'POST', leavePath)[0].body).toBeUndefined()
      expect(screen.getByText('groups page')).toBeTruthy()
    })

    it('shows the left toast with the group name, with no Undo button, in the wine-red danger style', async () => {
      const dangerStyling = referenceDangerToastStyling()
      await renderMembers({
        group: memberGroup,
        members: anaViewMembers,
        answers: { [`POST ${leavePath}`]: noContentAnswer() },
      })
      await showsMembers()

      fireEvent.click(leaveButton())

      const shown = await waitFor(() =>
        toastShownWithText(translated('en', 'groupSettings.left', { name: memberGroup.name })),
      )
      expect(shown.options.action).toBeUndefined()
      expect(shown.options.duration).toBe(4000)
      expect(stylingOf(shown)).toEqual(dangerStyling)
    })

    it('shows the error toast and stays on the screen when leaving is refused', async () => {
      consoleErrorSilenced()
      const { router } = await renderMembers({
        group: memberGroup,
        members: anaViewMembers,
        answers: { [`POST ${leavePath}`]: problemAnswer(400, 'MEMBER_OWNER_CANNOT_LEAVE') },
      })
      await showsMembers()

      fireEvent.click(leaveButton())

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(
          translated('en', 'errors.MEMBER_OWNER_CANNOT_LEAVE'),
        )
      })
      expect(router.state.location.pathname).toBe(routes.groupMembers(testGroup.id))
      expect(shownToastTexts()).toEqual([])
    })
  })

  it('sends no request besides asking for the group and its members when nothing is pressed', async () => {
    const { fetchMock } = await renderMembers()
    await showsMembers()

    expect(requestedUrls(fetchMock).sort()).toEqual([groupPath, membersPath].sort())
  })
})
