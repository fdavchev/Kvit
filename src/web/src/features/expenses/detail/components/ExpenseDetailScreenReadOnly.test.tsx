import { screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { Language } from '@/core/i18n/language'
import { languages } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { Group } from '@/core/services/groups/groupsService'
import type { GroupMembers } from '@/core/services/groups/membersService'
import { asMemberOf, closingGroup, finishedGroup } from '@/test/closingTestData'
import { hotelDetail, testCategories } from '@/test/expenseTestData'
import {
  categoriesPath,
  expensePathOf,
  freezeTime,
  groupPath,
  membersPath,
  unfreezeTime,
} from '@/test/expenseTestHelpers'
import { groupOf, testGroupId } from '@/test/groupTestData'
import { anaViewMembers, ownerViewMembers } from '@/test/memberTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { jsonAnswer, stubFetchByRequest } from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { ExpenseDetailScreen } from './ExpenseDetailScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const editableDetail = { ...hotelDetail, canEdit: true }
const openOwnerGroup = groupOf({ memberCount: 5 })
const openMemberGroup = groupOf({ isOwner: false, memberCount: 5 })

async function renderDetail(group: Group, members: GroupMembers, language: Language = 'en') {
  stubFetchByRequest({
    [`GET ${groupPath}`]: jsonAnswer(group),
    [`GET ${membersPath}`]: jsonAnswer(members),
    [`GET ${categoriesPath}`]: jsonAnswer({ categories: testCategories }),
    [`GET ${expensePathOf(editableDetail.id)}`]: jsonAnswer(editableDetail),
  })
  await renderRoutesWithProviders(
    [
      { path: '/groups/:groupId/expenses/:expenseId', element: <ExpenseDetailScreen /> },
      { path: '/groups/:groupId/expenses/:expenseId/edit', element: <p>edit expense page</p> },
      { path: '/groups/:groupId', element: <p>group page</p> },
    ],
    routes.groupExpense(testGroupId, editableDetail.id),
    { language, seedCache: seedMe({ ...testMe, language }) },
  )
  await screen.findByRole('heading', { level: 1, name: 'Hotel' })
}

function editLink(language: Language = 'en'): HTMLElement | null {
  return screen.queryByRole('link', { name: translated(language, 'expense.edit') })
}

function deleteButton(language: Language = 'en'): HTMLElement | null {
  return screen.queryByRole('button', { name: translated(language, 'expense.delete') })
}

describe('the expense detail when the group is read-only', () => {
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
    ['a member of an open group who added the expense', openMemberGroup, anaViewMembers],
    ['the owner of a group that is being closed', closingGroup, ownerViewMembers],
  ])('has Edit and Delete for %s', async (_name, group, members) => {
    await renderDetail(group, members)

    expect(editLink()).not.toBeNull()
    expect(deleteButton()).not.toBeNull()
  })

  it.each([
    ['a member of a group that is being closed', asMemberOf(closingGroup), anaViewMembers],
    ['the owner of a finished group', finishedGroup, ownerViewMembers],
    ['a member of a finished group', asMemberOf(finishedGroup), anaViewMembers],
  ])('has neither Edit nor Delete for %s, although the server says the person could edit', async (_name, group, members) => {
    await renderDetail(group, members)

    expect(editLink()).toBeNull()
    expect(deleteButton()).toBeNull()
  })

  it.each(languages)('has neither Edit nor Delete for the owner of a finished group, also in %s', async (language) => {
    await renderDetail(finishedGroup, ownerViewMembers, language)

    expect(editLink(language)).toBeNull()
    expect(deleteButton(language)).toBeNull()
  })

  it('still shows the amount, the split and the history of the expense in a finished group', async () => {
    await renderDetail(finishedGroup, ownerViewMembers)

    expect(screen.getByRole('heading', { level: 2, name: translated('en', 'expense.split') })).toBeTruthy()
    expect(screen.getByRole('heading', { level: 2, name: translated('en', 'expense.history') })).toBeTruthy()
  })
})
