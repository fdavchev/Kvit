import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { Group } from '@/core/services/groups/groupsService'
import type { GroupMembers } from '@/core/services/groups/membersService'
import { activityQueryKey } from '@/features/groups/shared/groupCache'
import { stubFetchThatNeverAnswers } from '@/test/apiTestHelpers'
import { closingGroup, finishedGroup } from '@/test/closingTestData'
import { groupOf, testGroup } from '@/test/groupTestData'
import {
  anaMember,
  cvetaGuestMember,
  filipMember,
  markoMember,
  ownerViewMembers,
  petarMember,
} from '@/test/memberTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import {
  jsonAnswer,
  noContentAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
  stubFetchByRequest,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { seedMe, testGuestMe, testMe } from '@/test/testMe'
import { shownToastTexts } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'
import { MembersScreen } from './MembersScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupPath = `/api/groups/${testGroup.id}`
const membersPath = `${groupPath}/members`
const letInAgainPath = `${membersPath}/${cvetaGuestMember.id}/let-in-again`
const undoClaimPath = `${membersPath}/${cvetaGuestMember.id}/undo-claim`

const ownerGroup = groupOf({ memberCount: 6 })
const membersWithGuest: GroupMembers = {
  ...ownerViewMembers,
  members: [...ownerViewMembers.members, cvetaGuestMember],
}
const membersWithTappedGuest: GroupMembers = {
  ...ownerViewMembers,
  members: [...ownerViewMembers.members, { ...cvetaGuestMember, claimedName: cvetaGuestMember.displayName }],
}

const guestMe = { ...testGuestMe, id: cvetaGuestMember.userId ?? '', displayName: cvetaGuestMember.displayName }
const guestViewGroup = groupOf({ isOwner: false, memberCount: 3 })
const guestViewMembers: GroupMembers = {
  members: [{ ...filipMember, isYou: false }, markoMember, { ...cvetaGuestMember, isYou: true }],
  removed: [],
  canClaimNames: false,
}

const namesOnScreen = [
  filipMember.displayName,
  anaMember.displayName,
  markoMember.displayName,
  petarMember.displayName,
  cvetaGuestMember.displayName,
]

interface RenderOptions {
  language?: Language
  group?: Group
  members?: GroupMembers
  asGuest?: boolean
  answers?: Record<string, AnswerFactory>
}

async function renderMembers(options: RenderOptions = {}) {
  const language = options.language ?? 'en'
  const group = options.group ?? ownerGroup
  const fetchMock = stubFetchByRequest({
    [`GET ${groupPath}`]: jsonAnswer(group),
    [`GET ${membersPath}`]: jsonAnswer(options.members ?? membersWithGuest),
    ...options.answers,
  })
  const me = options.asGuest === true ? guestMe : testMe
  const rendered = await renderRoutesWithProviders(
    [
      { path: '/groups/:groupId/members', element: <MembersScreen /> },
      { path: '/groups/:groupId', element: <p>group page</p> },
    ],
    routes.groupMembers(group.id),
    {
      language,
      seedCache: (queryClient) => {
        seedMe({ ...me, language })(queryClient)
        queryClient.setQueryData(activityQueryKey(group.id), [])
      },
    },
  )
  await screen.findByRole('heading', { level: 1, name: translated(language, 'members.title') })
  await screen.findByText(cvetaGuestMember.displayName)
  return { fetchMock, ...rendered }
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

async function openOptions(name: string, language: Language = 'en'): Promise<HTMLElement> {
  fireEvent.click(screen.getByRole('button', { name: translated(language, 'members.optionsFor', { name }) }))
  return screen.findByRole('dialog', { name })
}

function letInAgainButton(sheet: HTMLElement, language: Language = 'en'): HTMLElement {
  return within(sheet).getByRole('button', { name: translated(language, 'members.letInAgain') })
}

function consoleErrorSilenced(): void {
  vi.spyOn(console, 'error').mockImplementation(() => {})
}

describe('the Members screen with a guest in the group', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('the Guest badge', () => {
    it.each(languages)('shows the Guest badge on the guest row only (%s)', async (language) => {
      await renderMembers({ language })

      const badges = screen.getAllByText(translated(language, 'members.guest'))

      expect(badges).toHaveLength(1)
      expect(rowOf(cvetaGuestMember.displayName).contains(badges[0])).toBe(true)
    })

    it('draws the Guest badge as a dashed outline in the muted text colour, apart from the filled Owner badge', async () => {
      await renderMembers()

      const badge = screen.getByText(translated('en', 'members.guest'))

      expect(badge.className).toMatch(/(^|\s)border-dashed(\s|$)/)
      expect(badge.className).toMatch(/(^|\s)text-muted-foreground(\s|$)/)
      expect(badge.className).not.toMatch(/(^|\s)bg-pill(\s|$)/)
    })

    it('keeps the badge in the same line box as the name, which wraps, so a long name pushes it to the next line instead of off the screen', async () => {
      await renderMembers()

      const badge = screen.getByText(translated('en', 'members.guest'))
      const nameLine = badge.parentElement

      expect(nameLine?.className).toMatch(/(^|\s)flex-wrap(\s|$)/)
      expect(within(nameLine as HTMLElement).getByText(cvetaGuestMember.displayName).className).toMatch(/(^|\s)break-words(\s|$)/)
    })

    it.each(languages)('shows the owner only the Guest badge and no "took the name" line under a guest who tapped the free name that became their own name (%s)', async (language) => {
      const tappedGuest = { ...cvetaGuestMember, claimedName: cvetaGuestMember.displayName }
      await renderMembers({ language, members: { ...ownerViewMembers, members: [...ownerViewMembers.members, tappedGuest] } })

      const guestRow = rowOf(cvetaGuestMember.displayName)

      expect(within(guestRow).getByText(translated(language, 'members.guest'))).toBeTruthy()
      expect(
        within(guestRow).queryByText(translated(language, 'members.tookName', { name: cvetaGuestMember.displayName })),
      ).toBeNull()
      expect(within(rowOf(petarMember.displayName)).getByText(translated(language, 'members.tookName', { name: 'Darko' }))).toBeTruthy()
    })

    it('shows no "took the name" line either once that guest made an account and their name is still the name they tapped', async () => {
      const upgradedGuest = { ...cvetaGuestMember, isGuest: false, claimedName: cvetaGuestMember.displayName }
      await renderMembers({ members: { ...ownerViewMembers, members: [...ownerViewMembers.members, upgradedGuest] } })

      expect(within(rowOf(cvetaGuestMember.displayName)).queryByText(/Took the name/)).toBeNull()
      expect(screen.getAllByText(/Took the name/)).toHaveLength(1)
    })

    it.each(languages)('shows the guest their own row with both the You and the Guest badge (%s)', async (language) => {
      await renderMembers({ language, asGuest: true, group: guestViewGroup, members: guestViewMembers })

      const ownRow = rowOf(cvetaGuestMember.displayName)

      expect(within(ownRow).getByText(translated(language, 'members.you'))).toBeTruthy()
      expect(within(ownRow).getByText(translated(language, 'members.guest'))).toBeTruthy()
    })
  })

  describe("the owner's options of a guest", () => {
    it.each(languages)('offers Let in again with its help line and Remove from group, but no Make owner (%s)', async (language) => {
      await renderMembers({ language })

      const sheet = await openOptions(cvetaGuestMember.displayName, language)

      expect(letInAgainButton(sheet, language)).toBeTruthy()
      expect(
        within(sheet).getByText(translated(language, 'members.letInAgainHint', { name: cvetaGuestMember.displayName })),
      ).toBeTruthy()
      expect(within(sheet).getByRole('button', { name: translated(language, 'members.remove') })).toBeTruthy()
      expect(within(sheet).queryByRole('button', { name: translated(language, 'members.makeOwner') })).toBeNull()
    })

    it.each(languages)('offers Let in again but no Undo claim for a guest who tapped a free name, so the owner has one way to free it (%s)', async (language) => {
      await renderMembers({ language, members: membersWithTappedGuest })

      const sheet = await openOptions(cvetaGuestMember.displayName, language)

      expect(letInAgainButton(sheet, language)).toBeTruthy()
      expect(within(sheet).queryByRole('button', { name: translated(language, 'members.undoClaim') })).toBeNull()
      expect(within(sheet).getByRole('button', { name: translated(language, 'members.remove') })).toBeTruthy()
    })

    it('frees the name of a guest who tapped a free name with two taps, the options button and Let in again, and sends no undo-claim request', async () => {
      const { fetchMock } = await renderMembers({
        members: membersWithTappedGuest,
        answers: { [`POST ${letInAgainPath}`]: noContentAnswer() },
      })

      const sheet = await openOptions(cvetaGuestMember.displayName)
      fireEvent.click(letInAgainButton(sheet))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', letInAgainPath)).toBe(1)
      })
      expect(requestCount(fetchMock, 'POST', undoClaimPath)).toBe(0)
    })

    it('offers Undo claim again once the guest who tapped a free name made an account', async () => {
      const upgradedGuest = { ...cvetaGuestMember, isGuest: false, claimedName: cvetaGuestMember.displayName }
      await renderMembers({ members: { ...ownerViewMembers, members: [...ownerViewMembers.members, upgradedGuest] } })

      const sheet = await openOptions(cvetaGuestMember.displayName)

      expect(within(sheet).getByRole('button', { name: translated('en', 'members.undoClaim') })).toBeTruthy()
      expect(within(sheet).queryByRole('button', { name: translated('en', 'members.letInAgain') })).toBeNull()
    })

    it('puts Let in again first in the sheet, before Remove from group', async () => {
      await renderMembers()

      const sheet = await openOptions(cvetaGuestMember.displayName)
      const buttonNames = within(sheet).getAllByRole('button').map((button) => button.textContent)

      expect(buttonNames[0]).toBe(translated('en', 'members.letInAgain'))
    })

    it.each([anaMember.displayName, markoMember.displayName, petarMember.displayName])('offers no Let in again for %s, who is not a guest', async (name) => {
      await renderMembers()

      const sheet = await openOptions(name)

      expect(within(sheet).queryByRole('button', { name: translated('en', 'members.letInAgain') })).toBeNull()
      expect(within(sheet).queryByText(/lost their phone/)).toBeNull()
    })

    it('sends the let-in-again request for the guest row with no body after two taps: the options button and Let in again', async () => {
      const { fetchMock } = await renderMembers({ answers: { [`POST ${letInAgainPath}`]: noContentAnswer() } })

      const sheet = await openOptions(cvetaGuestMember.displayName)
      fireEvent.click(letInAgainButton(sheet))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', letInAgainPath)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', letInAgainPath)[0].body).toBeUndefined()
    })

    it('asks no "are you sure" question before letting the guest in again', async () => {
      const { fetchMock } = await renderMembers({ answers: { [`POST ${letInAgainPath}`]: noContentAnswer() } })
      const sheet = await openOptions(cvetaGuestMember.displayName)

      fireEvent.click(letInAgainButton(sheet))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', letInAgainPath)).toBe(1)
      })
      expect(screen.queryAllByRole('dialog').length).toBeLessThanOrEqual(1)
      expect(screen.queryByRole('alertdialog')).toBeNull()
    })

    it.each(languages)('shows the can-tap-their-name toast, closes the sheet and loads the members and the activity again (%s)', async (language) => {
      const { fetchMock, queryClient } = await renderMembers({
        language,
        answers: { [`POST ${letInAgainPath}`]: noContentAnswer() },
      })
      const sheet = await openOptions(cvetaGuestMember.displayName, language)

      fireEvent.click(letInAgainButton(sheet, language))

      await waitFor(() => {
        expect(shownToastTexts()).toContain(
          translated(language, 'members.letInAgainDone', { name: cvetaGuestMember.displayName }),
        )
      })
      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', membersPath)).toBe(2)
      })
      expect(queryClient.getQueryState(activityQueryKey(ownerGroup.id))?.isInvalidated).toBe(true)
    })

    it('disables every action of the sheet while Let in again waits for an answer, so a double tap sends one request', async () => {
      await renderMembers()
      const sheet = await openOptions(cvetaGuestMember.displayName)
      stubFetchThatNeverAnswers()

      fireEvent.click(letInAgainButton(sheet))

      await waitFor(() => {
        expect(letInAgainButton(sheet).matches(':disabled')).toBe(true)
      })
      expect(within(sheet).getByRole('button', { name: translated('en', 'members.remove') }).matches(':disabled')).toBe(true)
      fireEvent.click(letInAgainButton(sheet))
      expect(vi.mocked(fetch).mock.calls).toHaveLength(1)
    })

    it.each(
      languages.flatMap((language) =>
        (
          [
            [400, 'MEMBER_NOT_GUEST'],
            [400, 'GROUP_FINISHED'],
            [400, 'GROUP_CLOSING'],
            [404, 'MEMBER_NOT_FOUND'],
          ] as const
        ).map(([status, code]) => [code, language, status] as const),
      ),
    )('shows the %s error toast and no success toast when the server refuses (%s)', async (code, language, status) => {
      consoleErrorSilenced()
      await renderMembers({ language, answers: { [`POST ${letInAgainPath}`]: problemAnswer(status, code) } })
      const sheet = await openOptions(cvetaGuestMember.displayName, language)

      fireEvent.click(letInAgainButton(sheet, language))

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated(language, `errors.${code}`))
      })
      expect(shownToastTexts()).toEqual([])
    })
  })

  describe('while the group is read-only', () => {
    it('lets the owner open the options of the guest and Let in again while the group is being closed, as for every other member action', async () => {
      await renderMembers({ group: { ...closingGroup, memberCount: 6 } })

      const sheet = await openOptions(cvetaGuestMember.displayName)

      expect(letInAgainButton(sheet)).toBeTruthy()
    })

    it('shows no options button on the guest row once the group is finished', async () => {
      await renderMembers({ group: { ...finishedGroup, memberCount: 6 } })

      expect(
        screen.queryByRole('button', { name: translated('en', 'members.optionsFor', { name: cvetaGuestMember.displayName }) }),
      ).toBeNull()
      expect(screen.getByText(translated('en', 'members.guest'))).toBeTruthy()
    })
  })

  describe('for a guest looking at the screen', () => {
    it.each(languages)('shows no + Add a name link, because a guest cannot add names (%s)', async (language) => {
      await renderMembers({ language, asGuest: true, group: guestViewGroup, members: guestViewMembers })

      expect(screen.queryByText(translated(language, 'members.addNameLink'))).toBeNull()
    })

    it('still shows the invite link card to a guest', async () => {
      await renderMembers({ asGuest: true, group: guestViewGroup, members: guestViewMembers })

      expect(screen.getByRole('button', { name: translated('en', 'members.invite.share') })).toBeTruthy()
    })

    it('shows the + Add a name link to a member with an account who is not the owner', async () => {
      await renderMembers({ group: groupOf({ isOwner: false, memberCount: 6 }) })

      expect(screen.getByText(translated('en', 'members.addNameLink'))).toBeTruthy()
    })
  })
})
