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

async function renderMembers(group: Group, members: GroupMembers, language: Language = 'en') {
  stubFetchByRequest({
    [`GET ${groupPath}`]: jsonAnswer(group),
    [`GET ${membersPath}`]: jsonAnswer(members),
  })
  await renderRoutesWithProviders(
    [
      { path: '/groups/:groupId/members', element: <MembersScreen /> },
      { path: '/groups/:groupId', element: <p>group page</p> },
      { path: routes.groups, element: <p>groups page</p> },
    ],
    routes.groupMembers(group.id),
    { language, seedCache: seedMe({ ...testMe, language }) },
  )
  await screen.findByText(petarMember.displayName)
}

function inviteButtons(language: Language = 'en'): { share: number; copy: number } {
  const count = (key: string): number =>
    screen.queryAllByRole('button', { name: translated(language, key) }).length
  return { share: count('members.invite.share'), copy: count('members.invite.copy') }
}

const noInviteButtons = { share: 0, copy: 0 }
const bothInviteButtons = { share: 1, copy: 1 }

describe('the invite link on the Members screen', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it.each([
    ['the owner of an open group', openOwnerGroup, ownerViewMembers],
    ['a member of an open group', openMemberGroup, anaViewMembers],
  ])('has Share and Copy for %s', async (_name, group, members) => {
    await renderMembers(group, members)

    expect(inviteButtons()).toEqual(bothInviteButtons)
  })

  it.each([
    ['the owner of a group that is being closed, because nobody can join while it is closing', closingGroup, ownerViewMembers],
    ['a member of a group that is being closed', asMemberOf(closingGroup), anaViewMembers],
    ['the owner of a finished group', finishedGroup, ownerViewMembers],
    ['a member of a finished group', asMemberOf(finishedGroup), anaViewMembers],
  ])('has neither Share nor Copy for %s', async (_name, group, members) => {
    await renderMembers(group, members)

    expect(inviteButtons()).toEqual(noInviteButtons)
  })

  it.each(languages)('has neither Share nor Copy for a finished group, also in %s', async (language) => {
    await renderMembers(finishedGroup, ownerViewMembers, language)

    expect(inviteButtons(language)).toEqual(noInviteButtons)
  })

  it('still lists the people of a finished group', async () => {
    await renderMembers(finishedGroup, ownerViewMembers)

    for (const member of ownerViewMembers.members) {
      expect(screen.getByText(member.displayName)).toBeTruthy()
    }
  })

  it('keeps Reset link for the owner of a group that is being closed, because the server allows it', async () => {
    await renderMembers(closingGroup, ownerViewMembers)

    expect(screen.getAllByRole('button', { name: translated('en', 'members.invite.reset') })).toHaveLength(1)
  })
})
