import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi, type MockInstance } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { routeObjects } from '@/core/router/router'
import { routes } from '@/core/router/routes'
import { problemResponse } from '@/test/apiTestHelpers'
import {
  activityEventOf,
  activityPath,
  editCases,
  editedEventOf,
  numberEvents,
  sentenceCases,
  type ActivityEventJson,
} from '@/test/activityTestData'
import {
  activityRowOf,
  activityTab,
  expensesTab,
  renderGroupApp,
  sentenceElement,
  stubGroupApp,
} from '@/test/activityTestHelpers'
import {
  bojanUserId,
  dinnerExpenseId,
  expenseRows,
  hotelExpenseId,
  museumExpenseId,
  taxiExpenseId,
  testCategories,
} from '@/test/expenseTestData'
import {
  categoriesPath,
  elementWithAll,
  expensesPath,
  freezeTime,
  groupPath,
  isBefore,
  macedonianDay,
  membersPath,
  plainSpaces,
  relativeTime,
  restoreRealRetryPolicy,
  unfreezeTime,
} from '@/test/expenseTestHelpers'
import { groupOf, testGroup, testInviteToken } from '@/test/groupTestData'
import {
  anaMember,
  anaUserId,
  filipMember,
  filipPictureUrl,
  grandmaMember,
  markoMember,
  membersOf,
  ownerViewMembers,
  petarMember,
  petarUserId,
} from '@/test/memberTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { networkFailureAnswer, problemAnswer, requestCount } from '@/test/requestTestHelpers'
import {
  removeShareAndClipboard,
  stubClipboard,
  stubShare,
} from '@/test/shareTestHelpers'
import { testMe } from '@/test/testMe'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupId = testGroup.id
const inviteUrl = `${window.location.origin}/join/${testInviteToken}`
const crashMessage = 'A screen crashed while rendering'

const oneBillGroup = groupOf({ kind: 'OneBill', memberCount: 3 })
const plainGroup = groupOf({ kind: 'Group', memberCount: 3 })

async function showsGroupScreen(name: string = testGroup.name): Promise<void> {
  await screen.findByRole('heading', { level: 1, name })
}

function sentenceOf(
  language: Language,
  key: string,
  params: Record<string, string>,
): string {
  return translated(language, key, params)
}

function colourIndexOf(row: HTMLElement): number {
  const match = /--avatar-(\d+)/.exec(row.innerHTML)
  if (match === null) {
    throw new Error(`Found no avatar colour in the row: ${row.innerHTML}`)
  }
  return Number(match[1])
}

function crashErrors(consoleError: MockInstance<typeof console.error>): Error[] {
  return consoleError.mock.calls
    .filter(([message]) => message === crashMessage)
    .map(([, error]) => error)
    .filter((error): error is Error => error instanceof Error)
}

const inviteLinkResetEvent: ActivityEventJson = activityEventOf({ type: 'InviteLinkReset' })

function createdEventAt(createdAt: string): ActivityEventJson {
  return activityEventOf({ type: 'GroupCreated', data: { name: 'Greece trip' }, createdAt })
}

describe('GroupScreen with the Expenses and Activity tabs', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    freezeTime()
    stubDeviceColorScheme('light')
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
    removeShareAndClipboard()
  })

  describe('the pill switch', () => {
    it.each(languages)('shows two links, Expenses and then Activity, under the Members and Settings links (%s)', async (language) => {
      await renderGroupApp(routes.group(groupId), { language })
      await showsGroupScreen()

      const members = screen.getByRole('link', { name: translated(language, 'members.title') })
      const settings = screen.getByRole('link', { name: translated(language, 'group.settings') })

      expect(isBefore(members, expensesTab(language))).toBe(true)
      expect(isBefore(settings, expensesTab(language))).toBe(true)
      expect(isBefore(expensesTab(language), activityTab(language))).toBe(true)
    })

    it('links Expenses to the group and Activity to its activity address', async () => {
      await renderGroupApp(routes.group(groupId))
      await showsGroupScreen()

      expect(expensesTab().getAttribute('href')).toBe(routes.group(groupId))
      expect(activityTab().getAttribute('href')).toBe(routes.groupActivity(groupId))
    })

    it('marks Expenses as the current page on the Expenses tab and not Activity', async () => {
      await renderGroupApp(routes.group(groupId))
      await showsGroupScreen()

      expect(expensesTab().getAttribute('aria-current')).toBe('page')
      expect(activityTab().getAttribute('aria-current')).not.toBe('page')
    })

    it('marks Activity as the current page on the Activity tab and not Expenses', async () => {
      await renderGroupApp(routes.groupActivity(groupId))
      await showsGroupScreen()

      expect(activityTab().getAttribute('aria-current')).toBe('page')
      expect(expensesTab().getAttribute('aria-current')).not.toBe('page')
    })

    it('opens the Activity tab when its link is pressed, and the Expenses tab again when that link is pressed', async () => {
      const { router } = await renderGroupApp(routes.group(groupId))
      await showsGroupScreen()

      fireEvent.click(activityTab())
      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groupActivity(groupId))
      })
      expect(await screen.findByText(translated('en', 'activity.empty'))).toBeTruthy()
      expect(activityTab().getAttribute('aria-current')).toBe('page')

      fireEvent.click(expensesTab())
      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(groupId))
      })
      expect(await screen.findByRole('link', { name: /Dinner/ })).toBeTruthy()
      expect(expensesTab().getAttribute('aria-current')).toBe('page')
    })

    it.each([
      ['the Expenses tab', routes.group(groupId)],
      ['the Activity tab', routes.groupActivity(groupId)],
    ])('keeps the group header with its name, size and the Members and Settings links on %s', async (_name, path) => {
      await renderGroupApp(path, { group: groupOf({ memberCount: 3 }) })

      await showsGroupScreen()

      expect(screen.getByText('3 people · MKD')).toBeTruthy()
      expect(screen.getByRole('link', { name: translated('en', 'members.title') }).getAttribute('href')).toBe(
        routes.groupMembers(groupId),
      )
      expect(screen.getByRole('link', { name: translated('en', 'group.settings') }).getAttribute('href')).toBe(
        routes.groupSettings(groupId),
      )
      expect(screen.getByRole('link', { name: translated('en', 'common.back') }).getAttribute('href')).toBe(
        routes.groups,
      )
    })

    it('shows the + button on the Expenses tab', async () => {
      await renderGroupApp(routes.group(groupId))
      await screen.findByRole('link', { name: /Dinner/ })

      expect(
        screen.getByRole('link', { name: translated('en', 'expense.addTitle') }).getAttribute('href'),
      ).toBe(routes.groupExpenseNew(groupId))
    })

    it.each([
      ['an empty activity', [], translated('en', 'activity.empty')],
      ['activity with events', [inviteLinkResetEvent], 'Ana reset the invite link'],
    ])('shows no + button on the Activity tab with %s', async (_name, events, shownText) => {
      await renderGroupApp(routes.groupActivity(groupId), { events })
      await screen.findByText(shownText)

      expect(screen.queryByRole('link', { name: translated('en', 'expense.addTitle') })).toBeNull()
    })

    it('shows no expense rows on the Activity tab and no activity on the Expenses tab', async () => {
      await renderGroupApp(routes.groupActivity(groupId), { events: [inviteLinkResetEvent] })
      await screen.findByText('Ana reset the invite link')

      expect(screen.queryByText(translated('en', 'expenses.empty'))).toBeNull()
      expect(screen.queryByRole('link', { name: /Dinner/ })).toBeNull()
    })

    it('does not ask for the activity while the Expenses tab is open', async () => {
      const { fetchMock } = await renderGroupApp(routes.group(groupId))
      await screen.findByRole('link', { name: /Dinner/ })

      expect(requestCount(fetchMock, 'GET', activityPath)).toBe(0)
    })
  })

  describe('what the Activity tab asks for', () => {
    it('asks for the activity of this group, for its members and for its expenses', async () => {
      const { fetchMock } = await renderGroupApp(routes.groupActivity(groupId), {
        events: [inviteLinkResetEvent],
      })
      await screen.findByText('Ana reset the invite link')

      const urls = fetchMock.mock.calls.map(([url]) => String(url))
      expect(urls).toContain(activityPath)
      expect(urls).toContain(membersPath)
      expect(urls).toContain(expensesPath)
    })

    it('keeps the activity in the cache under the key groups, the group id, activity', async () => {
      const events = numberEvents([inviteLinkResetEvent])
      const { queryClient } = await renderGroupApp(routes.groupActivity(groupId), { events })
      await screen.findByText('Ana reset the invite link')

      expect(queryClient.getQueryData(['groups', groupId, 'activity'])).toEqual(events)
    })
  })

  describe('loading and failing', () => {
    it('shows a loading spinner while the activity is being asked', async () => {
      vi.stubGlobal(
        'fetch',
        vi.fn<typeof fetch>(async (input) => {
          const url = String(input)
          const answers: Record<string, unknown> = {
            '/api/me': testMe,
            [groupPath]: testGroup,
            [membersPath]: ownerViewMembers,
            [expensesPath]: { expenses: expenseRows },
            [categoriesPath]: { categories: testCategories },
          }
          if (url === activityPath) {
            return new Promise<Response>(() => {})
          }
          if (url in answers) {
            return Response.json(answers[url])
          }
          throw new Error(`The test did not expect a request to ${url}`)
        }),
      )
      await renderRoutesWithProviders(routeObjects, routes.groupActivity(groupId))
      await showsGroupScreen()

      expect(screen.getByRole('status')).toBeTruthy()
    })

    it.each(languages)('shows the empty text and no row when nothing has happened yet (%s)', async (language) => {
      await renderGroupApp(routes.groupActivity(groupId), { language, events: [] })

      expect(await screen.findByText(translated(language, 'activity.empty'))).toBeTruthy()
    })

    it('does not show the empty text while there are events', async () => {
      await renderGroupApp(routes.groupActivity(groupId), { events: [inviteLinkResetEvent] })
      await screen.findByText('Ana reset the invite link')

      expect(screen.queryByText(translated('en', 'activity.empty'))).toBeNull()
    })

    it('shows the translated error with a retry button when the activity cannot be loaded, and keeps the group header', async () => {
      await renderGroupApp(routes.groupActivity(groupId), {
        answers: { [`GET ${activityPath}`]: () => new Response(null, { status: 500 }) },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.generic'))
      expect(screen.getByRole('button', { name: translated('en', 'common.retry') })).toBeTruthy()
      expect(screen.getByRole('heading', { level: 1, name: testGroup.name })).toBeTruthy()
    })

    it('shows the network message when the activity cannot be reached', async () => {
      await renderGroupApp(routes.groupActivity(groupId), {
        answers: { [`GET ${activityPath}`]: networkFailureAnswer() },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.network'))
    })

    it('asks for the activity again and shows it when the retry button is pressed', async () => {
      let tries = 0
      await renderGroupApp(routes.groupActivity(groupId), {
        events: [inviteLinkResetEvent],
        answers: {
          [`GET ${activityPath}`]: () => {
            tries += 1
            return tries === 1
              ? new Response(null, { status: 500 })
              : Response.json({ events: numberEvents([inviteLinkResetEvent]) })
          },
        },
      })

      fireEvent.click(await screen.findByRole('button', { name: translated('en', 'common.retry') }))

      expect(await screen.findByText('Ana reset the invite link')).toBeTruthy()
      expect(screen.queryByRole('alert')).toBeNull()
    })

    it.each([
      [404, 'GROUP_NOT_FOUND'],
      [429, 'RATE_LIMITED'],
    ])('asks for the activity only once after a %i answer, with the retry policy of the app', async (status, code) => {
      const { fetchMock } = await renderGroupApp(routes.groupActivity(groupId), {
        seedCache: restoreRealRetryPolicy,
        answers: { [`GET ${activityPath}`]: problemAnswer(status, code) },
      })

      await screen.findByRole('alert')

      expect(requestCount(fetchMock, 'GET', activityPath)).toBe(1)
    })

    it('shows a Go to Groups link and no retry button when the activity answers that the group is not found', async () => {
      await renderGroupApp(routes.groupActivity(groupId), {
        answers: { [`GET ${activityPath}`]: () => problemResponse(404, 'GROUP_NOT_FOUND') },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.GROUP_NOT_FOUND'))
      expect(screen.getByRole('link', { name: translated('en', 'groups.goToGroups') })).toBeTruthy()
      expect(screen.queryByRole('button', { name: translated('en', 'common.retry') })).toBeNull()
    })

    it.each([
      ['the members', membersPath],
      ['the expenses', expensesPath],
    ])('shows the error with a retry button, not the events, when %s cannot be loaded', async (_name, path) => {
      await renderGroupApp(routes.groupActivity(groupId), {
        events: [inviteLinkResetEvent],
        answers: { [`GET ${path}`]: () => new Response(null, { status: 500 }) },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.generic'))
      expect(screen.getByRole('button', { name: translated('en', 'common.retry') })).toBeTruthy()
      expect(screen.queryByText('Ana reset the invite link')).toBeNull()
    })
  })

  describe('the Share invite link chip of a One bill', () => {
    function shareChip(language: Language = 'en'): HTMLElement {
      return screen.getByRole('button', { name: translated(language, 'group.shareLink') })
    }

    it.each([
      ['the Expenses tab', routes.group(groupId)],
      ['the Activity tab', routes.groupActivity(groupId)],
    ])('shows the chip in the header of a One bill on %s, under the name and above the pill switch', async (_name, path) => {
      await renderGroupApp(path, { group: oneBillGroup })
      await showsGroupScreen(oneBillGroup.name)

      const heading = screen.getByRole('heading', { level: 1, name: oneBillGroup.name })

      expect(isBefore(heading, shareChip())).toBe(true)
      expect(isBefore(shareChip(), expensesTab())).toBe(true)
    })

    it.each(languages)('writes the chip with the existing Share invite link text (%s)', async (language) => {
      await renderGroupApp(routes.group(groupId), { group: oneBillGroup, language })
      await showsGroupScreen(oneBillGroup.name)

      expect(shareChip(language).textContent).toContain(translated(language, 'group.shareLink'))
    })

    it('opens the share menu of the phone with the invite link of the One bill when the chip is pressed', async () => {
      const share = stubShare(async () => {})
      await renderGroupApp(routes.group(groupId), { group: oneBillGroup })
      await showsGroupScreen(oneBillGroup.name)

      fireEvent.click(shareChip())

      await waitFor(() => {
        expect(share).toHaveBeenCalledWith(expect.objectContaining({ url: inviteUrl }))
      })
    })

    it('copies the link when the phone has no share menu', async () => {
      const writeText = stubClipboard(async () => {})
      await renderGroupApp(routes.groupActivity(groupId), { group: oneBillGroup })
      await showsGroupScreen(oneBillGroup.name)

      fireEvent.click(shareChip())

      await waitFor(() => {
        expect(writeText).toHaveBeenCalledExactlyOnceWith(inviteUrl)
      })
    })

    it.each([
      ['the Expenses tab', routes.group(groupId)],
      ['the Activity tab', routes.groupActivity(groupId)],
    ])('shows no chip in the header of a normal group on %s', async (_name, path) => {
      await renderGroupApp(path, { group: plainGroup })
      await showsGroupScreen(plainGroup.name)

      expect(screen.queryByRole('button', { name: translated('en', 'group.shareLink') })).toBeNull()
    })
  })

  describe('the Add people card and the tabs', () => {
    const aloneGroup = groupOf({ kind: 'Group', memberCount: 1 })
    const aloneOneBill = groupOf({ kind: 'OneBill', memberCount: 1 })

    function addPeopleHeading(): HTMLElement {
      return screen.getByRole('heading', { name: translated('en', 'group.addPeople') })
    }

    function expectNoAddPeopleCard(): void {
      expect(screen.queryByRole('heading', { name: translated('en', 'group.addPeople') })).toBeNull()
      expect(screen.queryByText(translated('en', 'group.addPeopleNoKvit'))).toBeNull()
      expect(screen.queryByText(translated('en', 'group.addPeopleHasKvit'))).toBeNull()
      expect(screen.queryByRole('button', { name: translated('en', 'group.addName') })).toBeNull()
    }

    it.each([
      ['an empty activity', [], translated('en', 'activity.empty')],
      ['activity with events', [inviteLinkResetEvent], 'Ana reset the invite link'],
    ])('shows no card and no Share invite link button on the Activity tab of a group with one person and no expenses, with %s', async (_name, events, shownText) => {
      await renderGroupApp(routes.groupActivity(groupId), { group: aloneGroup, expenses: [], events })
      await screen.findByText(shownText)

      expectNoAddPeopleCard()
      expect(screen.queryByRole('button', { name: translated('en', 'group.shareLink') })).toBeNull()
    })

    it('shows the card on the Expenses tab only: it goes away on the Activity tab and comes back on the Expenses tab', async () => {
      await renderGroupApp(routes.group(groupId), { group: aloneGroup, expenses: [] })
      await screen.findByText(translated('en', 'expenses.empty'))
      expect(addPeopleHeading()).toBeTruthy()

      fireEvent.click(activityTab())
      await screen.findByText(translated('en', 'activity.empty'))
      expectNoAddPeopleCard()

      fireEvent.click(expensesTab())
      await screen.findByText(translated('en', 'expenses.empty'))
      expect(addPeopleHeading()).toBeTruthy()
    })

    it.each([
      ['the Expenses tab', routes.group(groupId), translated('en', 'expenses.empty')],
      ['the Activity tab', routes.groupActivity(groupId), translated('en', 'activity.empty')],
    ])('shows the Share invite link chip of a One bill with one person and no expenses but never the card, on %s', async (_name, path, shownText) => {
      await renderGroupApp(path, { group: aloneOneBill, expenses: [], events: [] })
      await showsGroupScreen(aloneOneBill.name)
      await screen.findByText(shownText)

      const chips = screen.getAllByRole('button', { name: translated('en', 'group.shareLink') })

      expect(chips).toHaveLength(1)
      expect(isBefore(chips[0], expensesTab())).toBe(true)
      expectNoAddPeopleCard()
    })
  })

  describe('the sentences of the events', () => {
    describe.each(languages)('in %s', (language) => {
      it.each(sentenceCases)('writes the event $name as one sentence', async ({ event, key, params }) => {
        await renderGroupApp(routes.groupActivity(groupId), { language, events: [event] })
        await showsGroupScreen()
        const sentence = sentenceOf(language, key, params(language))

        expect(await screen.findByText(plainSpaces(sentence), { normalizer: plainSpaces })).toBeTruthy()
      })

      it.each(editCases)('writes an edit of the $field of an expense as the sentence of the expense history', async (edit) => {
        await renderGroupApp(routes.groupActivity(groupId), {
          language,
          events: [editedEventOf([{ field: edit.field, old: edit.old, new: edit.new }])],
        })
        await showsGroupScreen()
        const sentence = sentenceOf(language, edit.key, edit.params(language))

        expect(await screen.findByText(plainSpaces(sentence), { normalizer: plainSpaces })).toBeTruthy()
      })
    })

    it('writes the English sentences the way Filip approved them', async () => {
      await renderGroupApp(routes.groupActivity(groupId), {
        events: [
          activityEventOf({
            type: 'ExpenseAdded',
            expenseId: dinnerExpenseId,
            data: { title: 'Dinner', amountMinor: 240000, currency: 'MKD' },
          }),
          activityEventOf({ type: 'MemberAdded', data: { name: 'Grandma' } }),
          activityEventOf({ type: 'MemberJoined', data: { name: 'Petar' } }),
          editedEventOf([{ field: 'amount', old: '280000', new: '300000' }]),
        ],
      })

      expect(await screen.findByText('Ana added “Dinner” · 2,400 MKD', { normalizer: plainSpaces })).toBeTruthy()
      expect(screen.getByText('Ana added “Grandma”')).toBeTruthy()
      expect(screen.getByText('Petar joined')).toBeTruthy()
      expect(screen.getByText('Ana changed the amount: 2,800 MKD → 3,000 MKD', { normalizer: plainSpaces })).toBeTruthy()
    })

    it('writes an event that changed the emoji and the currency of the group as two sentences', async () => {
      await renderGroupApp(routes.groupActivity(groupId), {
        events: [
          activityEventOf({
            type: 'GroupSettingsChanged',
            changes: [
              { field: 'emoji', old: '\u{1F3D6}\u{FE0F}', new: '\u{1F3E0}' },
              { field: 'defaultCurrency', old: 'MKD', new: 'EUR' },
            ],
          }),
        ],
      })

      expect(
        await screen.findByText("Ana changed the group's emoji: \u{1F3D6}\u{FE0F} → \u{1F3E0}"),
      ).toBeTruthy()
      expect(screen.getByText("Ana changed the group's currency: MKD → EUR")).toBeTruthy()
    })

    it('writes the name of the member from the data of the event, not the name of the person who did it', async () => {
      await renderGroupApp(routes.groupActivity(groupId), {
        events: [activityEventOf({ type: 'MemberRemoved', data: { name: 'Bojan' } })],
      })

      expect(await screen.findByText('Ana removed “Bojan”')).toBeTruthy()
    })

    it('writes the amount of an expense event in the currency of that expense, not the currency of the group', async () => {
      await renderGroupApp(routes.groupActivity(groupId), {
        group: groupOf({ defaultCurrency: 'MKD' }),
        events: [
          activityEventOf({
            type: 'ExpenseAdded',
            expenseId: dinnerExpenseId,
            data: { title: null, amountMinor: 4500, currency: 'EUR' },
          }),
        ],
      })

      expect(await screen.findByText('Ana added an expense · €45.00', { normalizer: plainSpaces })).toBeTruthy()
    })

    it('writes one row for every change of an edit, each with its own sentence', async () => {
      await renderGroupApp(routes.groupActivity(groupId), {
        events: [
          editedEventOf([
            { field: 'amount', old: '280000', new: '300000' },
            { field: 'paidBy', old: 'Ana', new: 'Filip' },
          ]),
        ],
      })
      const amount = 'Ana changed the amount: 2,800 MKD → 3,000 MKD'
      const paidBy = 'Ana changed who paid: Ana → Filip'
      await screen.findByText(amount, { normalizer: plainSpaces })

      expect(activityRowOf(amount)).not.toBe(activityRowOf(paidBy))
      expect(within(activityRowOf(amount)).queryByText(paidBy)).toBeNull()
      expect(within(activityRowOf(paidBy)).queryByText(amount, { normalizer: plainSpaces })).toBeNull()
    })

    it('shows the title of the expense as a second line in every row of an edit when the expense is in the list', async () => {
      await renderGroupApp(routes.groupActivity(groupId), {
        events: [
          editedEventOf([
            { field: 'amount', old: '280000', new: '300000' },
            { field: 'paidBy', old: 'Ana', new: 'Filip' },
          ]),
        ],
      })
      await screen.findByText('Ana changed who paid: Ana → Filip')

      expect(within(activityRowOf('Ana changed who paid: Ana → Filip')).getByText('Hotel')).toBeTruthy()
      expect(
        within(activityRowOf('Ana changed the amount: 2,800 MKD → 3,000 MKD')).getByText('Hotel'),
      ).toBeTruthy()
    })

    it('shows the title of the expense as the list has it', async () => {
      await renderGroupApp(routes.groupActivity(groupId), {
        events: [editedEventOf([{ field: 'paidBy', old: 'Filip', new: 'Ana' }], taxiExpenseId)],
      })
      await screen.findByText('Ana changed who paid: Filip → Ana')

      expect(within(activityRowOf('Ana changed who paid: Filip → Ana')).getByText('Taxi')).toBeTruthy()
    })

    it('shows no second line for an edit of an expense that is not in the list', async () => {
      await renderGroupApp(routes.groupActivity(groupId), {
        events: [editedEventOf([{ field: 'paidBy', old: 'Ana', new: 'Filip' }], museumExpenseId)],
      })
      const sentence = 'Ana changed who paid: Ana → Filip'
      await screen.findByText(sentence)

      const row = activityRowOf(sentence)
      const shownTime = relativeTime('en', -5, 'minute')
      const initialOfActor = 1
      const room = 6
      expect(plainSpaces(row.textContent ?? '').length).toBeLessThanOrEqual(
        initialOfActor + sentence.length + shownTime.length + room,
      )
    })

    it('shows no second line for an event that is not about an edit', async () => {
      await renderGroupApp(routes.groupActivity(groupId), {
        events: [
          activityEventOf({
            type: 'ExpenseAdded',
            expenseId: dinnerExpenseId,
            data: { title: 'Dinner', amountMinor: 240000, currency: 'MKD' },
          }),
        ],
      })
      const sentence = 'Ana added “Dinner” · 2,400 MKD'
      await screen.findByText(sentence, { normalizer: plainSpaces })

      expect(within(activityRowOf(sentence)).queryAllByText('Dinner')).toHaveLength(0)
    })
  })

  describe('the order', () => {
    it('shows the events in the order of the answer, newest first', async () => {
      await renderGroupApp(routes.groupActivity(groupId), {
        events: [
          createdEventAt('2026-10-06T09:55:00Z'),
          activityEventOf({ type: 'InviteLinkReset', createdAt: '2026-10-06T08:00:00Z' }),
          activityEventOf({ type: 'GroupDeleted', createdAt: '2026-10-04T10:00:00Z' }),
        ],
      })
      await screen.findByText('Ana created the group')

      expect(isBefore(sentenceElement('Ana created the group'), sentenceElement('Ana reset the invite link'))).toBe(true)
      expect(isBefore(sentenceElement('Ana reset the invite link'), sentenceElement('Ana deleted the group'))).toBe(true)
    })
  })

  describe('the time of an event', () => {
    const timedEvents: ActivityEventJson[] = [
      createdEventAt('2026-10-06T09:55:00Z'),
      activityEventOf({ type: 'InviteLinkReset', createdAt: '2026-10-06T08:00:00Z' }),
      activityEventOf({ type: 'GroupDeleted', createdAt: '2026-10-04T10:00:00Z' }),
      activityEventOf({ type: 'GroupRestored', createdAt: '2026-09-28T09:00:00Z' }),
      activityEventOf({ type: 'InviteLinkRestored', createdAt: '2026-09-20T09:00:00Z' }),
    ]

    it.each(languages)('writes how long ago a recent event was with Intl.RelativeTimeFormat in minutes, hours and days (%s)', async (language) => {
      await renderGroupApp(routes.groupActivity(groupId), { language, events: timedEvents })
      await screen.findByText(translated(language, 'activity.groupCreated', { name: 'Ana' }))

      expect(
        elementWithAll([translated(language, 'activity.groupCreated', { name: 'Ana' }), relativeTime(language, -5, 'minute')], 40),
      ).toBeTruthy()
      expect(
        elementWithAll([translated(language, 'activity.inviteLinkReset', { name: 'Ana' }), relativeTime(language, -2, 'hour')], 40),
      ).toBeTruthy()
      expect(
        elementWithAll([translated(language, 'activity.groupDeleted', { name: 'Ana' }), relativeTime(language, -2, 'day')], 40),
      ).toBeTruthy()
    })

    it.each(languages)('writes the date instead of a relative time for an event older than seven days (%s)', async (language) => {
      await renderGroupApp(routes.groupActivity(groupId), { language, events: timedEvents })
      await screen.findByText(translated(language, 'activity.groupCreated', { name: 'Ana' }))
      const olderDay = language === 'en' ? '28 Sep' : macedonianDay('2026-09-28', false)
      const oldestDay = language === 'en' ? '20 Sep' : macedonianDay('2026-09-20', false)

      expect(
        elementWithAll([translated(language, 'activity.groupRestored', { name: 'Ana' }), olderDay], 40),
      ).toBeTruthy()
      expect(
        elementWithAll([translated(language, 'activity.inviteLinkRestored', { name: 'Ana' }), oldestDay], 40),
      ).toBeTruthy()
      expect(screen.queryByText(relativeTime(language, -8, 'day'))).toBeNull()
    })

    it('decides the day of an old event in the time zone of the account', async () => {
      await renderGroupApp(routes.groupActivity(groupId), {
        timeZone: 'Pacific/Pago_Pago',
        events: [activityEventOf({ type: 'GroupRestored', createdAt: '2026-09-28T09:00:00Z' })],
      })
      await screen.findByText('Ana restored the group')

      expect(elementWithAll(['Ana restored the group', '27 Sep'], 40)).toBeTruthy()
    })
  })

  describe('the avatar of the person who did it', () => {
    const resetBy = (name: string, userId: string): ActivityEventJson =>
      activityEventOf({ type: 'InviteLinkReset', actorUserId: userId, actorName: name })

    it('shows the Google picture of the person when the members list has one', async () => {
      await renderGroupApp(routes.groupActivity(groupId), {
        members: membersOf({
          members: [{ ...filipMember, pictureUrl: filipPictureUrl }, anaMember, markoMember, grandmaMember, petarMember],
        }),
        events: [resetBy('Filip', testMe.id)],
      })
      await screen.findByText('Filip reset the invite link')

      const picture = activityRowOf('Filip reset the invite link').querySelector('img')

      expect(picture?.getAttribute('src')).toBe(filipPictureUrl)
      expect(picture?.getAttribute('referrerpolicy')).toBe('no-referrer')
      expect(picture?.getAttribute('alt')).toBe('')
    })

    it('shows the initial on the colour of the place in the joining order when the person has no picture', async () => {
      await renderGroupApp(routes.groupActivity(groupId), {
        events: [resetBy('Ana', anaUserId), resetBy('Petar', petarUserId), resetBy('Filip', testMe.id)],
      })
      await screen.findByText('Ana reset the invite link')

      const ana = activityRowOf('Ana reset the invite link')
      const petar = activityRowOf('Petar reset the invite link')
      const filip = activityRowOf('Filip reset the invite link')

      expect(ana.querySelector('img')).toBeNull()
      expect(within(ana).getByText('A')).toBeTruthy()
      expect(colourIndexOf(ana)).toBe(1)
      expect(within(petar).getByText('P')).toBeTruthy()
      expect(colourIndexOf(petar)).toBe(4)
      expect(within(filip).getByText('F')).toBeTruthy()
      expect(colourIndexOf(filip)).toBe(0)
    })

    it('shows the initial on the neutral colour 9 when the person is not in the members list any more', async () => {
      await renderGroupApp(routes.groupActivity(groupId), {
        events: [resetBy('Bojan', bojanUserId)],
      })
      await screen.findByText('Bojan reset the invite link')

      const bojan = activityRowOf('Bojan reset the invite link')

      expect(within(bojan).getByText('B')).toBeTruthy()
      expect(colourIndexOf(bojan)).toBe(9)
      expect(bojan.querySelector('img')).toBeNull()
    })

  })

  describe('the link to the expense', () => {
    it.each([
      [
        'an added expense that is in the list',
        activityEventOf({
          type: 'ExpenseAdded',
          expenseId: dinnerExpenseId,
          data: { title: 'Dinner', amountMinor: 240000, currency: 'MKD' },
        }),
        'Ana added “Dinner” · 2,400 MKD',
        dinnerExpenseId,
      ],
      [
        'an edited expense that is in the list',
        editedEventOf([{ field: 'paidBy', old: 'Ana', new: 'Filip' }], hotelExpenseId),
        'Ana changed who paid: Ana → Filip',
        hotelExpenseId,
      ],
      [
        'a restored expense that is in the list',
        activityEventOf({ type: 'ExpenseRestored', expenseId: taxiExpenseId }),
        'Ana restored an expense',
        taxiExpenseId,
      ],
    ])('links the row of %s to the detail of that expense', async (_name, event, sentence, expenseId) => {
      await renderGroupApp(routes.groupActivity(groupId), { events: [event] })
      await screen.findByText(sentence, { normalizer: plainSpaces })

      const link = sentenceElement(sentence).closest('a')

      expect(link?.getAttribute('href')).toBe(routes.groupExpense(groupId, expenseId))
    })

    it('opens the detail of the expense when the row is pressed', async () => {
      const { router } = await renderGroupApp(routes.groupActivity(groupId), {
        events: [editedEventOf([{ field: 'paidBy', old: 'Ana', new: 'Filip' }], hotelExpenseId)],
      })
      await screen.findByText('Ana changed who paid: Ana → Filip')

      fireEvent.click(sentenceElement('Ana changed who paid: Ana → Filip'))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groupExpense(groupId, hotelExpenseId))
      })
    })

    it('has no link for the event of an expense that was deleted and is not in the list', async () => {
      await renderGroupApp(routes.groupActivity(groupId), {
        events: [
          activityEventOf({
            type: 'ExpenseDeleted',
            expenseId: museumExpenseId,
            data: { title: 'Museum tickets', amountMinor: 80000, currency: 'MKD' },
          }),
        ],
      })
      const sentence = 'Ana deleted “Museum tickets” · 800 MKD'
      await screen.findByText(sentence, { normalizer: plainSpaces })

      expect(sentenceElement(sentence).closest('a')).toBeNull()
    })

    it('has no link for the event of an expense that is not in the list any more, because it was deleted later', async () => {
      await renderGroupApp(routes.groupActivity(groupId), {
        events: [
          activityEventOf({
            type: 'ExpenseAdded',
            expenseId: museumExpenseId,
            data: { title: 'Museum tickets', amountMinor: 80000, currency: 'MKD' },
          }),
        ],
      })
      const sentence = 'Ana added “Museum tickets” · 800 MKD'
      await screen.findByText(sentence, { normalizer: plainSpaces })

      expect(sentenceElement(sentence).closest('a')).toBeNull()
    })

    it('has no link for an event that is not about an expense', async () => {
      await renderGroupApp(routes.groupActivity(groupId), {
        events: [activityEventOf({ type: 'GroupCreated', data: { name: 'Greece trip' } })],
      })
      await screen.findByText('Ana created the group')

      expect(sentenceElement('Ana created the group').closest('a')).toBeNull()
    })
  })

  describe('an event that Kvit has no sentence for', () => {
    it('stops with an error that names the event type and shows the generic error instead of skipping the event', async () => {
      const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderGroupApp(routes.groupActivity(groupId), {
        events: [
          activityEventOf({ type: 'GroupCreated', data: { name: 'Greece trip' } }),
          activityEventOf({ type: 'SettlementRecorded' }),
        ],
      })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.generic'))
      await waitFor(() => {
        expect(crashErrors(consoleError).some((error) => error.message.includes('SettlementRecorded'))).toBe(true)
      })
      expect(screen.queryByText('Ana created the group')).toBeNull()
    })

    it.each([
      ['an expense edit', editedEventOf([{ field: 'weight', old: '1', new: '2' }]), 'weight'],
      [
        'a change of the group settings',
        activityEventOf({ type: 'GroupSettingsChanged', changes: [{ field: 'theme', old: 'a', new: 'b' }] }),
        'theme',
      ],
      [
        'a rename of the group',
        activityEventOf({ type: 'GroupRenamed', changes: [{ field: 'description', old: 'a', new: 'b' }] }),
        'description',
      ],
    ])('stops with an error that names the field when %s changes a field Kvit does not know', async (_name, event, field) => {
      const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderGroupApp(routes.groupActivity(groupId), { events: [event] })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.generic'))
      await waitFor(() => {
        expect(crashErrors(consoleError).some((error) => error.message.includes(field))).toBe(true)
      })
    })
  })

  it('asks only for the signed-in person, the group, its expenses and the categories when the Expenses tab opens', async () => {
    const fetchMock = stubGroupApp()
    await renderRoutesWithProviders(routeObjects, routes.group(groupId))
    await screen.findByRole('link', { name: /Dinner/ })

    const urls = fetchMock.mock.calls.map(([url]) => String(url)).sort()
    expect(urls).toEqual(['/api/me', groupPath, expensesPath, categoriesPath].sort())
  })
})
