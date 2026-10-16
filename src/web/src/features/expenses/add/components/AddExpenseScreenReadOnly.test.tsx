import { screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { Group } from '@/core/services/groups/groupsService'
import type { GroupMembers } from '@/core/services/groups/membersService'
import { asMemberOf, closingGroup, finishedGroup } from '@/test/closingTestData'
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
const openMemberGroup = groupOf({ isOwner: false, memberCount: 5 })

async function renderAddByUrl(group: Group, members: GroupMembers, language: Language = 'en') {
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
    { language, seedCache: seedMe({ ...testMe, language }) },
  )
}

function saveButton(language: Language = 'en'): HTMLElement | null {
  return screen.queryByRole('button', { name: translated(language, 'groupSettings.save') })
}

describe('the Add expense screen reached by its address', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    freezeTime()
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it.each([
    ['the owner of an open group', openOwnerGroup, ownerViewMembers],
    ['a member of an open group', openMemberGroup, anaViewMembers],
    ['the owner of a group that is being closed', closingGroup, ownerViewMembers],
  ])('shows the form to %s', async (_name, group, members) => {
    await renderAddByUrl(group, members)

    expect(await screen.findByRole('button', { name: translated('en', 'groupSettings.save') })).toBeTruthy()
    expect(screen.queryByText('group page')).toBeNull()
  })

  it.each([
    ['a member of a group that is being closed', asMemberOf(closingGroup), anaViewMembers],
    ['the owner of a finished group', finishedGroup, ownerViewMembers],
    ['a member of a finished group', asMemberOf(finishedGroup), anaViewMembers],
  ])('sends %s to the group screen instead of showing the form', async (_name, group, members) => {
    await renderAddByUrl(group, members)

    expect(await screen.findByText('group page')).toBeTruthy()
    expect(saveButton()).toBeNull()
  })

  it.each(languages)('sends a person to the group screen without a form for a finished group, also in %s', async (language) => {
    await renderAddByUrl(finishedGroup, ownerViewMembers, language)

    expect(await screen.findByText('group page')).toBeTruthy()
    expect(saveButton(language)).toBeNull()
  })
})
