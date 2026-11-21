import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { routes } from '@/core/router/routes'
import type { GroupMembers } from '@/core/services/groups/membersService'
import { testCategories, hotelDetail } from '@/test/expenseTestData'
import {
  amountField,
  doneButton,
  formRow,
  openSheet,
  personBoxValue,
  personButton,
  personGroup,
  saveButton,
  sentBody,
  splitTab,
  typeAmount,
  typeInPerson,
  waitForSheetToClose,
} from '@/test/expenseFormTestHelpers'
import {
  categoriesPath,
  expensesPath,
  freezeTime,
  groupPath,
  membersPath,
  startsWith,
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
import { jsonAnswer, requestCount, stubFetchByRequest } from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { membersQueryKey } from '@/features/groups/shared/groupCache'
import { AddExpenseScreen } from './AddExpenseScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const hotelMinor = 300000

const fiveMembers: GroupMembers = ownerViewMembers

function withoutMember(members: GroupMembers, memberId: string): GroupMembers {
  return membersOf({
    members: members.members.filter((member) => member.id !== memberId),
    removed: [],
    canClaimNames: false,
  })
}

function withMembers(members: GroupMembers, joined: GroupMembers['members']): GroupMembers {
  return membersOf({ members: [...members.members, ...joined], removed: [], canClaimNames: false })
}

async function renderReadyAddWithLiveMembers() {
  let currentMembers: GroupMembers = fiveMembers
  const fetchMock = stubFetchByRequest({
    [`GET ${groupPath}`]: jsonAnswer(groupOf({ memberCount: 5 })),
    [`GET ${membersPath}`]: () => Response.json(currentMembers),
    [`GET ${categoriesPath}`]: jsonAnswer({ categories: testCategories }),
    [`POST ${expensesPath}`]: jsonAnswer(hotelDetail),
  })
  const rendered = await renderRoutesWithProviders(
    [
      { path: '/groups/:groupId/expenses/new', element: <AddExpenseScreen /> },
      { path: '/groups/:groupId', element: <p>group page</p> },
    ],
    routes.groupExpenseNew(testGroupId),
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

function expectFormStillShown(): void {
  expect(screen.getByRole('button', { name: translated('en', 'groupSettings.save') })).toBeTruthy()
  expect(screen.queryByText(/unexpected application error/i)).toBeNull()
}

async function saveAndReadBody(fetchMock: ReturnType<typeof stubFetchByRequest>): Promise<Record<string, unknown>> {
  fireEvent.click(saveButton())
  await waitFor(() => {
    expect(requestCount(fetchMock, 'POST', expensesPath)).toBe(1)
  })
  return sentBody(fetchMock, 'POST', expensesPath)
}

async function tickOut(name: string): Promise<void> {
  const sheet = await openSheet('split')
  fireEvent.click(within(personGroup(sheet, name)).getByRole('button', { name: startsWith(name) }))
  fireEvent.click(doneButton(sheet))
  await waitForSheetToClose()
}

async function typeExtraFor(name: string, text: string): Promise<void> {
  const sheet = await openSheet('split')
  fireEvent.click(within(personGroup(sheet, name)).getByRole('button', { name: translated('en', 'expense.extra') }))
  typeInPerson(sheet, name, text)
  fireEvent.click(doneButton(sheet))
  await waitForSheetToClose()
}

describe('AddExpenseScreen while the members list changes', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    freezeTime()
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('when a member is removed in another session', () => {
    it('keeps showing the form and keeps the typed amount', async () => {
      const { changeMembersTo } = await renderReadyAddWithLiveMembers()
      typeAmount('3000')

      await changeMembersTo(withoutMember(fiveMembers, markoMember.id))

      expectFormStillShown()
      expect(amountField().value).toBe('3000')
    })

    it('still says equally among everyone when everyone was in the split', async () => {
      const { changeMembersTo } = await renderReadyAddWithLiveMembers()

      await changeMembersTo(withoutMember(fiveMembers, markoMember.id))

      const text = formRow('split').textContent ?? ''
      expect(text).toContain(translated('en', 'expense.equally'))
      expect(text).toContain(translated('en', 'expense.everyone'))
    })

    it('lists the people still in the split by name, without the removed one, when someone else was ticked out', async () => {
      const { changeMembersTo } = await renderReadyAddWithLiveMembers()
      typeAmount('3000')
      await tickOut('Ana')

      await changeMembersTo(withoutMember(fiveMembers, markoMember.id))

      const text = formRow('split').textContent ?? ''
      expect(text).toContain('Filip, Grandma, Petar')
      expect(text).not.toContain('Marko')
      expect(text).not.toContain(translated('en', 'expense.everyone'))
    })

    it('sends the split without the removed person', async () => {
      const { fetchMock, changeMembersTo } = await renderReadyAddWithLiveMembers()
      typeAmount('3000')

      await changeMembersTo(withoutMember(fiveMembers, markoMember.id))
      const body = await saveAndReadBody(fetchMock)

      expect(sharesOf(body)).toEqual(
        [filipMember, anaMember, grandmaMember, petarMember].map((member) => ({ memberId: member.id, inputValue: 0 })),
      )
      expect(body).toMatchObject({ amountMinor: hotelMinor, splitType: 'Equal' })
    })

    it('keeps what the others typed in the split', async () => {
      const { fetchMock, changeMembersTo } = await renderReadyAddWithLiveMembers()
      typeAmount('3000')
      await typeExtraFor('Petar', '600')

      await changeMembersTo(withoutMember(fiveMembers, markoMember.id))
      const body = await saveAndReadBody(fetchMock)

      expect(sharesOf(body)).toEqual([
        { memberId: filipMember.id, inputValue: 0 },
        { memberId: anaMember.id, inputValue: 0 },
        { memberId: grandmaMember.id, inputValue: 0 },
        { memberId: petarMember.id, inputValue: 60000 },
      ])
    })

    it('keeps the ticked-out person ticked out', async () => {
      const { fetchMock, changeMembersTo } = await renderReadyAddWithLiveMembers()
      typeAmount('3000')
      await tickOut('Ana')

      await changeMembersTo(withoutMember(fiveMembers, markoMember.id))
      const body = await saveAndReadBody(fetchMock)

      expect(sharesOf(body).map((share) => share.memberId)).toEqual([filipMember.id, grandmaMember.id, petarMember.id])
    })

    it('keeps what the others typed on the Exact tab of the Split sheet that is open', async () => {
      const { changeMembersTo } = await renderReadyAddWithLiveMembers()
      typeAmount('3000')
      const sheet = await openSheet('split')
      fireEvent.click(splitTab(sheet, 'splitExact'))
      typeInPerson(sheet, 'Filip', '700')

      await changeMembersTo(withoutMember(fiveMembers, markoMember.id))

      expect(personBoxValue(sheet, 'Filip')).toBe('700')
      expect(within(sheet).queryByRole('group', { name: startsWith('Marko') })).toBeNull()
    })

    it('shows the Paid by row with the signed-in person when the payer was the removed one', async () => {
      const { changeMembersTo } = await renderReadyAddWithLiveMembers()
      const sheet = await openSheet('paidBy')
      fireEvent.click(personButton(sheet, 'Marko'))
      await waitForSheetToClose()
      expect(formRow('paidBy').textContent).toContain('Marko')

      await changeMembersTo(withoutMember(fiveMembers, markoMember.id))

      expectFormStillShown()
      expect(formRow('paidBy').textContent).toContain(`Filip (${translated('en', 'expense.me')})`)
      expect(formRow('paidBy').textContent).not.toContain('Marko')
    })

    it('sends the signed-in person as the payer when the payer was the removed one', async () => {
      const { fetchMock, changeMembersTo } = await renderReadyAddWithLiveMembers()
      typeAmount('3000')
      const sheet = await openSheet('paidBy')
      fireEvent.click(personButton(sheet, 'Marko'))
      await waitForSheetToClose()

      await changeMembersTo(withoutMember(fiveMembers, markoMember.id))
      const body = await saveAndReadBody(fetchMock)

      expect(body.paidByMemberId).toBe(filipMember.id)
    })

    it('keeps another payer who is still a member', async () => {
      const { fetchMock, changeMembersTo } = await renderReadyAddWithLiveMembers()
      typeAmount('3000')
      const sheet = await openSheet('paidBy')
      fireEvent.click(personButton(sheet, 'Ana'))
      await waitForSheetToClose()

      await changeMembersTo(withoutMember(fiveMembers, markoMember.id))
      const body = await saveAndReadBody(fetchMock)

      expect(body.paidByMemberId).toBe(anaMember.id)
    })

    it('does not crash when the Split sheet is open at that moment, and the sheet no longer shows the removed person', async () => {
      const { changeMembersTo } = await renderReadyAddWithLiveMembers()
      typeAmount('3000')
      const sheet = await openSheet('split')
      expect(personGroup(sheet, 'Marko')).toBeTruthy()

      await changeMembersTo(withoutMember(fiveMembers, markoMember.id))

      expect(screen.getByRole('dialog', { name: translated('en', 'expense.split') })).toBeTruthy()
      expect(within(sheet).queryByRole('group', { name: startsWith('Marko') })).toBeNull()
      expect(personGroup(sheet, 'Ana')).toBeTruthy()
    })

    it('saves the split without the removed person after the sheet that was open is closed with Done', async () => {
      const { fetchMock, changeMembersTo } = await renderReadyAddWithLiveMembers()
      typeAmount('3000')
      const sheet = await openSheet('split')

      await changeMembersTo(withoutMember(fiveMembers, markoMember.id))
      fireEvent.click(doneButton(sheet))
      await waitForSheetToClose()
      const body = await saveAndReadBody(fetchMock)

      expect(sharesOf(body).map((share) => share.memberId)).toEqual([
        filipMember.id,
        anaMember.id,
        grandmaMember.id,
        petarMember.id,
      ])
    })

    it('does not crash when the Paid by sheet is open at that moment', async () => {
      const { changeMembersTo } = await renderReadyAddWithLiveMembers()
      const sheet = await openSheet('paidBy')

      await changeMembersTo(withoutMember(fiveMembers, markoMember.id))

      expect(screen.getByRole('dialog', { name: translated('en', 'expense.paidBy') })).toBeTruthy()
      expect(within(sheet).queryByRole('button', { name: startsWith('Marko') })).toBeNull()
    })

    it('does not crash when several people are removed at once', async () => {
      const { fetchMock, changeMembersTo } = await renderReadyAddWithLiveMembers()
      typeAmount('3000')

      await changeMembersTo(
        membersOf({ members: [filipMember, petarMember], removed: [], canClaimNames: false }),
      )
      const body = await saveAndReadBody(fetchMock)

      expect(sharesOf(body).map((share) => share.memberId)).toEqual([filipMember.id, petarMember.id])
    })
  })

  describe('when a person joins while the form is open', () => {
    it('puts the new person in an untouched equally-among-everyone split', async () => {
      const { fetchMock, changeMembersTo } = await renderReadyAddWithLiveMembers()
      typeAmount('3000')

      await changeMembersTo(withMembers(fiveMembers, [cvetaGuestMember]))
      const body = await saveAndReadBody(fetchMock)

      expect(sharesOf(body)).toEqual(
        [filipMember, anaMember, markoMember, grandmaMember, petarMember, cvetaGuestMember].map((member) => ({
          memberId: member.id,
          inputValue: 0,
        })),
      )
    })

    it('still says equally among everyone after the new person is added', async () => {
      const { changeMembersTo } = await renderReadyAddWithLiveMembers()

      await changeMembersTo(withMembers(fiveMembers, [cvetaGuestMember]))

      expect(formRow('split').textContent).toContain(translated('en', 'expense.everyone'))
    })

    it('does not add the new person when someone was ticked out', async () => {
      const { fetchMock, changeMembersTo } = await renderReadyAddWithLiveMembers()
      typeAmount('3000')
      await tickOut('Ana')

      await changeMembersTo(withMembers(fiveMembers, [cvetaGuestMember]))
      const body = await saveAndReadBody(fetchMock)

      expect(sharesOf(body).map((share) => share.memberId)).toEqual([
        filipMember.id,
        markoMember.id,
        grandmaMember.id,
        petarMember.id,
      ])
    })

    it('does not add the new person when someone has a typed extra', async () => {
      const { fetchMock, changeMembersTo } = await renderReadyAddWithLiveMembers()
      typeAmount('3000')
      await typeExtraFor('Marko', '600')

      await changeMembersTo(withMembers(fiveMembers, [cvetaGuestMember]))
      const body = await saveAndReadBody(fetchMock)

      expect(sharesOf(body).map((share) => share.memberId)).not.toContain(cvetaGuestMember.id)
      expect(sharesOf(body)).toHaveLength(5)
    })

    it('does not add the new person to a split of another type', async () => {
      const { fetchMock, changeMembersTo } = await renderReadyAddWithLiveMembers()
      typeAmount('3000')
      const sheet = await openSheet('split')
      fireEvent.click(splitTab(sheet, 'splitShares'))
      fireEvent.click(doneButton(sheet))
      await waitForSheetToClose()

      await changeMembersTo(withMembers(fiveMembers, [cvetaGuestMember]))
      const body = await saveAndReadBody(fetchMock)

      expect(body.splitType).toBe('Shares')
      expect(sharesOf(body).map((share) => share.memberId)).not.toContain(cvetaGuestMember.id)
    })

    it('does not crash when the Split sheet is open at that moment', async () => {
      const { changeMembersTo } = await renderReadyAddWithLiveMembers()
      typeAmount('3000')
      await openSheet('split')

      await changeMembersTo(withMembers(fiveMembers, [cvetaGuestMember]))

      expect(screen.getByRole('dialog', { name: translated('en', 'expense.split') })).toBeTruthy()
    })
  })

  describe('when a person leaves and another joins in the same refresh', () => {
    it('sends the new person and not the one who left in an untouched split', async () => {
      const { fetchMock, changeMembersTo } = await renderReadyAddWithLiveMembers()
      typeAmount('3000')

      await changeMembersTo(withMembers(withoutMember(fiveMembers, markoMember.id), [cvetaGuestMember]))
      const body = await saveAndReadBody(fetchMock)

      expect(sharesOf(body).map((share) => share.memberId)).toEqual([
        filipMember.id,
        anaMember.id,
        grandmaMember.id,
        petarMember.id,
        cvetaGuestMember.id,
      ])
    })
  })
})
