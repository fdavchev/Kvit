import { screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { Group } from '@/core/services/groups/groupsService'
import type { GroupMembers } from '@/core/services/groups/membersService'
import { asMemberOf, closingGroup, finishedGroup } from '@/test/closingTestData'
import { groupOf, testGroup } from '@/test/groupTestData'
import { anaViewMembers, ownerViewMembers, petarMember } from '@/test/memberTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { jsonAnswer, stubFetchByRequest } from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { MembersScreen } from './MembersScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupPath = `/api/groups/${testGroup.id}`
const membersPath = `${groupPath}/members`

const openOwnerGroup = groupOf({ memberCount: 5 })
const openMemberGroup = groupOf({ isOwner: false, memberCount: 5 })

interface Controls {
  options: number
  letBackIn: number
  resetLink: number
  addName: number
  thatsMe: number
  leave: number
}

async function renderMembers(group: Group, members: GroupMembers, language: Language = 'en') {
  const fetchMock = stubFetchByRequest({
    [`GET ${groupPath}`]: jsonAnswer(group),
    [`GET ${membersPath}`]: jsonAnswer(members),
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
  await screen.findByRole('heading', { level: 1, name: translated(language, 'members.title') })
  await screen.findByText(petarMember.displayName)
  return { fetchMock, ...rendered }
}

function controlsOnScreen(language: Language = 'en'): Controls {
  const count = (name: string | RegExp): number => screen.queryAllByRole('button', { name }).length
  return {
    options: count(/^(Options for|Опции за) /),
    letBackIn: count(translated(language, 'members.letBackIn')),
    resetLink: count(translated(language, 'members.invite.reset')),
    addName: count(translated(language, 'members.addNameLink')),
    thatsMe: count(translated(language, 'members.thatsMe')),
    leave: count(translated(language, 'groupSettings.leave')),
  }
}

describe('the Members screen when the group is read-only', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('for the owner', () => {
    it('has the options of every other person, Let back in, Reset link and Add a name while the group is open', async () => {
      await renderMembers(openOwnerGroup, ownerViewMembers)

      expect(controlsOnScreen()).toEqual({
        options: 4,
        letBackIn: 1,
        resetLink: 1,
        addName: 1,
        thatsMe: 0,
        leave: 0,
      })
    })

    it('keeps all of them while the group is being closed, because the owner may change who is in the group', async () => {
      await renderMembers(closingGroup, ownerViewMembers)

      expect(controlsOnScreen()).toEqual({
        options: 4,
        letBackIn: 1,
        resetLink: 1,
        addName: 1,
        thatsMe: 0,
        leave: 0,
      })
    })

    it('has none of them when the group is finished, so Remove, Make owner and Undo claim cannot be reached', async () => {
      await renderMembers(finishedGroup, ownerViewMembers)

      expect(controlsOnScreen()).toEqual({
        options: 0,
        letBackIn: 0,
        resetLink: 0,
        addName: 0,
        thatsMe: 0,
        leave: 0,
      })
    })

    it('still lists the people of a finished group', async () => {
      await renderMembers(finishedGroup, ownerViewMembers)

      for (const member of ownerViewMembers.members) {
        expect(screen.getByText(member.displayName)).toBeTruthy()
      }
    })

    it.each(languages)('has none of them when the group is finished, also in %s', async (language) => {
      await renderMembers(finishedGroup, ownerViewMembers, language)

      expect(controlsOnScreen(language)).toEqual({
        options: 0,
        letBackIn: 0,
        resetLink: 0,
        addName: 0,
        thatsMe: 0,
        leave: 0,
      })
    })
  })

  describe('for a member who is not the owner', () => {
    it('has That is me on the two names, Add a name and Leave group while the group is open', async () => {
      await renderMembers(openMemberGroup, anaViewMembers)

      expect(controlsOnScreen()).toEqual({
        options: 0,
        letBackIn: 0,
        resetLink: 0,
        addName: 1,
        thatsMe: 2,
        leave: 1,
      })
    })

    it('has neither That is me nor Add a name while the group is being closed', async () => {
      await renderMembers(asMemberOf(closingGroup), anaViewMembers)

      const controls = controlsOnScreen()

      expect(controls.thatsMe).toBe(0)
      expect(controls.addName).toBe(0)
      expect(controls.options).toBe(0)
      expect(controls.resetLink).toBe(0)
    })

    it('has no Leave group while the group is being closed, because the server refuses it', async () => {
      await renderMembers(asMemberOf(closingGroup), anaViewMembers)

      expect(controlsOnScreen().leave).toBe(0)
    })

    it('has neither That is me nor Add a name when the group is finished, but keeps Leave group', async () => {
      await renderMembers(asMemberOf(finishedGroup), anaViewMembers)

      expect(controlsOnScreen()).toEqual({
        options: 0,
        letBackIn: 0,
        resetLink: 0,
        addName: 0,
        thatsMe: 0,
        leave: 1,
      })
    })

    it('keeps Leave group enabled when the group is finished', async () => {
      await renderMembers(asMemberOf(finishedGroup), anaViewMembers)

      const leave = screen.getByRole<HTMLButtonElement>('button', { name: translated('en', 'groupSettings.leave') })

      expect(leave.disabled).toBe(false)
    })
  })
})
