import { screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { routes } from '@/core/router/routes'
import type { Group } from '@/core/services/groups/groupsService'
import type { GroupMembers } from '@/core/services/groups/membersService'
import { asMemberOf, closedByAdminGroup } from '@/test/closingTestData'
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

async function renderMembers(group: Group, members: GroupMembers) {
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
    { seedCache: seedMe(testMe) },
  )
  await screen.findByRole('heading', { level: 1, name: translated('en', 'members.title') })
  await screen.findByText(petarMember.displayName)
}

function controlsOnScreen(): Controls {
  const count = (name: string | RegExp): number => screen.queryAllByRole('button', { name }).length
  return {
    options: count(/^Options for /),
    letBackIn: count(translated('en', 'members.letBackIn')),
    resetLink: count(translated('en', 'members.invite.reset')),
    addName: count(translated('en', 'members.addNameLink')),
    thatsMe: count(translated('en', 'members.thatsMe')),
    leave: count(translated('en', 'groupSettings.leave')),
  }
}

describe('the Members screen when the owner of Kvit closed the group', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('has the options of every other person, Let back in, Reset link and Add a name for the owner while the group is open, so the next tests prove something', async () => {
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

  it('has none of them for the owner of a group that the owner of Kvit closed', async () => {
    await renderMembers(closedByAdminGroup, ownerViewMembers)

    expect(controlsOnScreen()).toEqual({
      options: 0,
      letBackIn: 0,
      resetLink: 0,
      addName: 0,
      thatsMe: 0,
      leave: 0,
    })
  })

  it('still lists the people of that group', async () => {
    await renderMembers(closedByAdminGroup, ownerViewMembers)

    for (const member of ownerViewMembers.members) {
      expect(screen.getByText(member.displayName)).toBeTruthy()
    }
  })

  it('has That is me on the two names, Add a name and Leave group for a member while the group is open, so the next tests prove something', async () => {
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

  it('has neither That is me nor Add a name for a member of that group, but keeps Leave group', async () => {
    await renderMembers(asMemberOf(closedByAdminGroup), anaViewMembers)

    expect(controlsOnScreen()).toEqual({
      options: 0,
      letBackIn: 0,
      resetLink: 0,
      addName: 0,
      thatsMe: 0,
      leave: 1,
    })
  })

  it('keeps Leave group enabled for a member of that group', async () => {
    await renderMembers(asMemberOf(closedByAdminGroup), anaViewMembers)

    const leave = screen.getByRole<HTMLButtonElement>('button', { name: translated('en', 'groupSettings.leave') })

    expect(leave.disabled).toBe(false)
  })
})
