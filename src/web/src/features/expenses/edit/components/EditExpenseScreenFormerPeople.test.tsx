import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { routes } from '@/core/router/routes'
import type { ExpenseDetail } from '@/core/services/expenses/expensesService'
import type { GroupMembers, RemovedMember } from '@/core/services/groups/membersService'
import { detailOf, hotelDetail, hotelExpenseId, testCategories } from '@/test/expenseTestData'
import {
  formRow,
  openSheet,
  personBoxValue,
  personGroup,
  saveButton,
  sentBody,
} from '@/test/expenseFormTestHelpers'
import {
  categoriesPath,
  expensePathOf,
  freezeTime,
  groupPath,
  membersPath,
  unfreezeTime,
} from '@/test/expenseTestHelpers'
import { groupOf, testGroupId } from '@/test/groupTestData'
import {
  anaMember,
  cvetaGuestMember,
  filipMember,
  grandmaMember,
  markoMember,
  membersOf,
  ownerViewMembers,
  petarMember,
} from '@/test/memberTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { jsonAnswer, noContentAnswer, requestCount, stubFetchByRequest } from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { membersQueryKey } from '@/features/groups/shared/groupCache'
import { EditExpenseScreen } from './EditExpenseScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const detailPath = expensePathOf(hotelExpenseId)

const markoRemoved: RemovedMember = { id: markoMember.id, displayName: 'Marko', pictureUrl: null }

const membersWithoutMarko: GroupMembers = membersOf({
  members: [filipMember, anaMember, grandmaMember, petarMember],
  removed: [markoRemoved],
  canClaimNames: false,
})

const membersWithCveta: GroupMembers = membersOf({
  members: [...ownerViewMembers.members, cvetaGuestMember],
  removed: [],
  canClaimNames: false,
})

const hotelShares = [
  { memberId: filipMember.id, inputValue: 0 },
  { memberId: anaMember.id, inputValue: 0 },
  { memberId: markoMember.id, inputValue: 60000 },
  { memberId: grandmaMember.id, inputValue: 0 },
  { memberId: petarMember.id, inputValue: 0 },
]

async function renderReadyEditWithLiveMembers(detail: ExpenseDetail, firstMembers: GroupMembers) {
  let currentMembers: GroupMembers = firstMembers
  const fetchMock = stubFetchByRequest({
    [`GET ${groupPath}`]: jsonAnswer(groupOf({ memberCount: 5 })),
    [`GET ${membersPath}`]: () => Response.json(currentMembers),
    [`GET ${categoriesPath}`]: jsonAnswer({ categories: testCategories }),
    [`GET ${expensePathOf(detail.id)}`]: jsonAnswer(detail),
    [`PUT ${expensePathOf(detail.id)}`]: noContentAnswer(),
  })
  const rendered = await renderRoutesWithProviders(
    [
      { path: '/groups/:groupId/expenses/:expenseId/edit', element: <EditExpenseScreen /> },
      { path: '/groups/:groupId/expenses/:expenseId', element: <p>expense detail page</p> },
    ],
    routes.groupExpenseEdit(testGroupId, detail.id),
    { seedCache: seedMe(testMe) },
  )
  await screen.findByRole('button', { name: translated('en', 'groupSettings.save') })

  async function changeMembersTo(members: GroupMembers): Promise<void> {
    const requestsBefore = requestCount(fetchMock, 'GET', membersPath)
    currentMembers = members
    await rendered.queryClient.invalidateQueries({ queryKey: membersQueryKey(testGroupId) })
    await waitFor(() => {
      expect(requestCount(fetchMock, 'GET', membersPath)).toBe(requestsBefore + 1)
    })
  }

  return { fetchMock, changeMembersTo, ...rendered }
}

function sharesOf(body: Record<string, unknown>): { memberId: string; inputValue: number }[] {
  const shares = body.shares
  if (!Array.isArray(shares)) {
    throw new Error(`Expected the body to have a list of shares, found ${JSON.stringify(shares)}`)
  }
  return shares as { memberId: string; inputValue: number }[]
}

function expectTheShares(body: Record<string, unknown>, expected: typeof hotelShares): void {
  const sent = sharesOf(body)
  expect(sent).toHaveLength(expected.length)
  expect(sent).toEqual(expect.arrayContaining(expected))
}

async function saveAndReadBody(fetchMock: ReturnType<typeof stubFetchByRequest>): Promise<Record<string, unknown>> {
  fireEvent.click(saveButton())
  await waitFor(() => {
    expect(requestCount(fetchMock, 'PUT', detailPath)).toBe(1)
  })
  return sentBody(fetchMock, 'PUT', detailPath)
}

describe('EditExpenseScreen with people who are no longer members', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    freezeTime()
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('when the person was removed before the form was opened', () => {
    it('still shows them in the split sheet with what they had typed', async () => {
      await renderReadyEditWithLiveMembers(hotelDetail, membersWithoutMarko)

      const sheet = await openSheet('split')

      expect(personGroup(sheet, 'Marko')).toBeTruthy()
      expect(personBoxValue(sheet, 'Marko')).toBe('600')
    })

    it('still says equally among everyone', async () => {
      await renderReadyEditWithLiveMembers(hotelDetail, membersWithoutMarko)

      const text = formRow('split').textContent ?? ''

      expect(text).toContain(translated('en', 'expense.equally'))
      expect(text).toContain(translated('en', 'expense.everyone'))
    })

    it('sends them again in the split, with the same typed value', async () => {
      const { fetchMock } = await renderReadyEditWithLiveMembers(hotelDetail, membersWithoutMarko)

      const body = await saveAndReadBody(fetchMock)

      expectTheShares(body, hotelShares)
    })

    it('shows them as the payer and sends them as the payer when they paid', async () => {
      const markoPaid = detailOf({ paidByMemberId: markoMember.id, paidByName: 'Marko' })
      const { fetchMock } = await renderReadyEditWithLiveMembers(markoPaid, membersWithoutMarko)

      expect(formRow('paidBy').textContent).toContain('Marko')
      const body = await saveAndReadBody(fetchMock)

      expect(body.paidByMemberId).toBe(markoMember.id)
    })
  })

  describe('when the person is removed while the form is open', () => {
    it('keeps showing the form and keeps them in the split', async () => {
      const { fetchMock, changeMembersTo } = await renderReadyEditWithLiveMembers(hotelDetail, ownerViewMembers)

      await changeMembersTo(membersWithoutMarko)

      expect(screen.queryByText(/unexpected application error/i)).toBeNull()
      const body = await saveAndReadBody(fetchMock)
      expectTheShares(body, hotelShares)
    })

    it('keeps the payer when the removed person was the payer', async () => {
      const markoPaid = detailOf({ paidByMemberId: markoMember.id, paidByName: 'Marko' })
      const { fetchMock, changeMembersTo } = await renderReadyEditWithLiveMembers(markoPaid, ownerViewMembers)

      await changeMembersTo(membersWithoutMarko)
      expect(formRow('paidBy').textContent).toContain('Marko')
      const body = await saveAndReadBody(fetchMock)

      expect(body.paidByMemberId).toBe(markoMember.id)
    })
  })

  describe('when a person joins', () => {
    it('does not add the new person to the split of the expense that is edited', async () => {
      const { fetchMock } = await renderReadyEditWithLiveMembers(hotelDetail, membersWithCveta)

      const body = await saveAndReadBody(fetchMock)

      expectTheShares(body, hotelShares)
    })

    it('does not add a person who joins while the form is open', async () => {
      const { fetchMock, changeMembersTo } = await renderReadyEditWithLiveMembers(hotelDetail, ownerViewMembers)

      await changeMembersTo(membersWithCveta)
      const body = await saveAndReadBody(fetchMock)

      expect(sharesOf(body).map((share) => share.memberId)).not.toContain(cvetaGuestMember.id)
      expectTheShares(body, hotelShares)
    })

    it('does not mention the new person in the Split row', async () => {
      const { changeMembersTo } = await renderReadyEditWithLiveMembers(hotelDetail, ownerViewMembers)

      await changeMembersTo(membersWithCveta)

      const text = formRow('split').textContent ?? ''
      expect(text).toContain(translated('en', 'expense.everyone'))
      expect(text).not.toContain('Cveta')
    })
  })
})
