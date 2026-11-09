import { screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { routes } from '@/core/router/routes'
import type { Group } from '@/core/services/groups/groupsService'
import type { GroupMembers } from '@/core/services/groups/membersService'
import { asMemberOf, closedByAdminFinishedGroup, closedByAdminGroup } from '@/test/closingTestData'
import { testCategories } from '@/test/expenseTestData'
import { categoriesPath, freezeTime, groupPath, membersPath, unfreezeTime } from '@/test/expenseTestHelpers'
import { groupOf, testGroupId } from '@/test/groupTestData'
import { anaViewMembers, ownerViewMembers } from '@/test/memberTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { jsonAnswer, stubFetchByRequest } from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { AddExpenseScreen } from './AddExpenseScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const openOwnerGroup = groupOf({ memberCount: 5 })

async function renderAddByUrl(group: Group, members: GroupMembers) {
  stubFetchByRequest({
    [`GET ${groupPath}`]: jsonAnswer(group),
    [`GET ${membersPath}`]: jsonAnswer(members),
    [`GET ${categoriesPath}`]: jsonAnswer({ categories: testCategories }),
  })
  await renderRoutesWithProviders(
    [
      { path: '/groups/:groupId/expenses/new', element: <AddExpenseScreen /> },
      { path: '/groups/:groupId', element: <p>group page</p> },
    ],
    routes.groupExpenseNew(testGroupId),
    { seedCache: seedMe(testMe) },
  )
}

function saveButton(): HTMLElement | null {
  return screen.queryByRole('button', { name: translated('en', 'groupSettings.save') })
}

describe('the Add expense screen reached by its address when the owner of Kvit closed the group', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    freezeTime()
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('shows the form for the owner of an open group, so the next tests prove something', async () => {
    await renderAddByUrl(openOwnerGroup, ownerViewMembers)

    expect(await screen.findByRole('button', { name: translated('en', 'groupSettings.save') })).toBeTruthy()
  })

  it.each([
    ['the owner of the group', closedByAdminGroup, ownerViewMembers],
    ['a member of the group', asMemberOf(closedByAdminGroup), anaViewMembers],
    ['the owner of a group that was finished', closedByAdminFinishedGroup, ownerViewMembers],
  ])('sends %s to the group screen instead of showing the form', async (_name, group, members) => {
    await renderAddByUrl(group, members)

    expect(await screen.findByText('group page')).toBeTruthy()
    expect(saveButton()).toBeNull()
  })
})
