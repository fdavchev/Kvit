import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { Group } from '@/core/services/groups/groupsService'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import { stubFetchThatNeverAnswers } from '@/test/apiTestHelpers'
import { expensesTab, activityTab } from '@/test/activityTestHelpers'
import { dinnerRow, testCategories } from '@/test/expenseTestData'
import { categoriesPath, expensesPath, isBefore, startsWith } from '@/test/expenseTestHelpers'
import { expectDisabledWhilePending, typeInto } from '@/test/formTestHelpers'
import {
  addNameButton,
  ancestorsBelow,
  emptyHint,
  emptyTitle,
  expectEmptyStateTexts,
  expectNoAddPeopleActions,
  expectNoAddPeopleHeading,
  expectNoEmptyStateTexts,
  findAddNameButton,
  findEmptyTitle,
  hasKvitLine,
  isBold,
  isCentred,
  isSmallText,
  lowestCommonAncestor,
  noKvitLine,
  receiptEmoji,
  shareLinkButton,
} from '@/test/groupEmptyStateTestHelpers'
import { groupOf, testGroup, testInviteToken } from '@/test/groupTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import {
  jsonAnswer,
  networkFailureAnswer,
  noContentAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
  stubFetchByRequest,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import {
  abortError,
  letPendingWorkFinish,
  removeShareAndClipboard,
  stubClipboard,
  stubShare,
} from '@/test/shareTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { shownToastTexts } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'
import { GroupScreen } from './GroupScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupPath = `/api/groups/${testGroup.id}`
const membersPath = `${groupPath}/members`
const inviteUrl = `${window.location.origin}/join/${testInviteToken}`

const emptyTextCases = [
  ['en', 'No expenses yet', 'Tap + to add the first one.', 'No expenses yet. Tap + to add the first one.'],
  ['mk', 'Сѐ уште нема трошоци', 'Допри + за да додадеш.', 'Сѐ уште нема трошоци. Допри + за да додадеш.'],
] as const

interface RenderOptions {
  language?: Language
  group?: Group
  answers?: Record<string, AnswerFactory>
}

async function renderGroup(options: RenderOptions = {}) {
  const group = options.group ?? testGroup
  const language = options.language ?? 'en'
  const fetchMock = stubFetchByRequest({
    [`GET /api/groups/${group.id}`]: jsonAnswer(group),
    [`GET ${expensesPath}`]: jsonAnswer({ expenses: [] }),
    [`GET ${categoriesPath}`]: jsonAnswer({ categories: testCategories }),
    ...options.answers,
  })
  const rendered = await renderRoutesWithProviders(
    [
      { path: '/groups/:groupId', element: <GroupScreen /> },
      { path: '/groups/:groupId/settings', element: <p>group settings page</p> },
      { path: '/groups/:groupId/members', element: <p>members page</p> },
      { path: routes.groups, element: <p>groups page</p> },
    ],
    routes.group(group.id),
    { language, seedCache: seedMe({ ...testMe, language }) },
  )
  return { fetchMock, ...rendered }
}

async function showsGroupScreen(name: string = testGroup.name): Promise<void> {
  await screen.findByRole('heading', { level: 1, name })
}

async function showsAddPeopleButtons(language: Language = 'en'): Promise<void> {
  await findAddNameButton(language)
}

function recentlyDeletedLink(language: Language = 'en'): HTMLElement {
  return screen.getByRole('link', { name: startsWith(translated(language, 'groups.recentlyDeletedLink')) })
}

function stubGroupWithOneRequestNeverAnswered(neverAnsweredPath: string): void {
  const answers: Record<string, unknown> = {
    [groupPath]: testGroup,
    [expensesPath]: { expenses: [] },
    [categoriesPath]: { categories: testCategories },
  }
  vi.stubGlobal(
    'fetch',
    vi.fn<typeof fetch>(async (input) => {
      const url = String(input)
      if (url === neverAnsweredPath) {
        return new Promise<Response>(() => {})
      }
      const body: unknown = answers[url]
      if (body === undefined) {
        throw new Error(`The test did not expect the request ${url}`)
      }
      return Response.json(body)
    }),
  )
}

function sheet(language: Language = 'en'): HTMLElement {
  return screen.getByRole('dialog', { name: translated(language, 'addName.title') })
}

function sheetAddButton(language: Language = 'en'): HTMLElement {
  return within(sheet(language)).getByRole('button', {
    name: translated(language, 'addName.submit'),
  })
}

async function openSheet(language: Language = 'en'): Promise<void> {
  fireEvent.click(addNameButton(language))
  await screen.findByRole('dialog', { name: translated(language, 'addName.title') })
}

function nameField(language: Language = 'en'): HTMLInputElement {
  return within(sheet(language)).getByLabelText<HTMLInputElement>(translated(language, 'addName.label'))
}

function typeName(name: string, language: Language = 'en'): void {
  typeInto(translated(language, 'addName.label'), name)
}

describe('GroupScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
    removeShareAndClipboard()
  })

  describe('loading and failing', () => {
    it('shows the loading spinner while the group is being asked', async () => {
      stubFetchThatNeverAnswers()

      await renderRoutesWithProviders(
        [{ path: '/groups/:groupId', element: <GroupScreen /> }],
        routes.group(testGroup.id),
        { seedCache: seedMe(testMe) },
      )

      expect(screen.getByRole('status')).toBeTruthy()
    })

    it.each(languages)('shows the GROUP_NOT_FOUND message when the group does not exist or the person is not in it (%s)', async (language) => {
      await renderGroup({
        language,
        answers: { [`GET ${groupPath}`]: problemAnswer(404, 'GROUP_NOT_FOUND') },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated(language, 'errors.GROUP_NOT_FOUND'),
      )
    })

    it.each(languages)('shows the group-not-found message for a 404 without an error code (%s)', async (language) => {
      await renderGroup({
        language,
        answers: { [`GET ${groupPath}`]: () => new Response(null, { status: 404 }) },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated(language, 'errors.GROUP_NOT_FOUND'),
      )
    })

    it.each(languages)('shows a Go to Groups link to the groups list and no retry button when the group is not found (%s)', async (language) => {
      await renderGroup({
        language,
        answers: { [`GET ${groupPath}`]: problemAnswer(404, 'GROUP_NOT_FOUND') },
      })
      await screen.findByRole('alert')

      const link = screen.getByRole('link', { name: translated(language, 'groups.goToGroups') })

      expect(link.getAttribute('href')).toBe(routes.groups)
      expect(
        screen.queryByRole('button', { name: translated(language, 'common.retry') }),
      ).toBeNull()
    })

    it('shows the Go to Groups link and no retry button for a 404 without an error code', async () => {
      await renderGroup({
        answers: { [`GET ${groupPath}`]: () => new Response(null, { status: 404 }) },
      })
      await screen.findByRole('alert')

      expect(
        screen.getByRole('link', { name: translated('en', 'groups.goToGroups') }).getAttribute('href'),
      ).toBe(routes.groups)
      expect(screen.queryByRole('button', { name: translated('en', 'common.retry') })).toBeNull()
    })

    it('opens the groups list when the Go to Groups link is pressed', async () => {
      const { router } = await renderGroup({
        answers: { [`GET ${groupPath}`]: problemAnswer(404, 'GROUP_NOT_FOUND') },
      })

      fireEvent.click(await screen.findByRole('link', { name: translated('en', 'groups.goToGroups') }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groups)
      })
    })

    it.each([
      ['a server error', () => new Response(null, { status: 500 })],
      ['a network failure', networkFailureAnswer()],
    ])('shows no Go to Groups link after %s', async (_label, answer) => {
      await renderGroup({ answers: { [`GET ${groupPath}`]: answer } })
      await screen.findByRole('alert')

      expect(screen.queryByRole('link', { name: translated('en', 'groups.goToGroups') })).toBeNull()
    })

    it('shows the generic message with a retry button after a server error', async () => {
      await renderGroup({
        answers: { [`GET ${groupPath}`]: () => new Response(null, { status: 500 }) },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated('en', 'errors.generic'),
      )
      expect(screen.getByRole('button', { name: translated('en', 'common.retry') })).toBeTruthy()
    })

    it('shows the network message when the server cannot be reached', async () => {
      await renderGroup({ answers: { [`GET ${groupPath}`]: networkFailureAnswer() } })

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated('en', 'errors.network'),
      )
    })

    it('asks for the group again and shows it when the retry button is pressed', async () => {
      let tries = 0
      await renderGroup({
        answers: {
          [`GET ${groupPath}`]: () => {
            tries += 1
            return tries === 1 ? new Response(null, { status: 500 }) : Response.json(testGroup)
          },
        },
      })

      fireEvent.click(await screen.findByRole('button', { name: translated('en', 'common.retry') }))

      await showsGroupScreen()
      expect(screen.queryByRole('alert')).toBeNull()
    })
  })

  describe('the header', () => {
    it('has a back button to the groups list', async () => {
      await renderGroup()
      await showsGroupScreen()

      const back = screen.getByRole('link', { name: translated('en', 'common.back') })

      expect(back.getAttribute('href')).toBe(routes.groups)
    })

    it('shows the name of the group as the heading', async () => {
      await renderGroup({ group: groupOf({ name: 'Flat 4B' }) })

      expect(await screen.findByRole('heading', { level: 1, name: 'Flat 4B' })).toBeTruthy()
    })

    it('shows the emoji of the group', async () => {
      await renderGroup({ group: groupOf({ emoji: '\u{1F355}' }) })
      await showsGroupScreen()

      expect(screen.getByText('\u{1F355}')).toBeTruthy()
    })

    it.each([
      ['en', 1, 'MKD', '1 person · MKD'],
      ['en', 3, 'EUR', '3 people · EUR'],
      ['mk', 1, 'MKD', '1 лице · MKD'],
      ['mk', 3, 'EUR', '3 лица · EUR'],
    ] as const)('shows how many people and which currency (%s, %i people, %s)', async (language, memberCount, defaultCurrency, text) => {
      await renderGroup({ language, group: groupOf({ memberCount, defaultCurrency }) })
      await showsGroupScreen()

      expect(screen.getByText(text)).toBeTruthy()
    })

    it.each(languages)('has a Group settings link to the settings of this group (%s)', async (language) => {
      await renderGroup({ language })
      await showsGroupScreen()

      const link = screen.getByRole('link', { name: translated(language, 'group.settings') })

      expect(link.getAttribute('href')).toBe(routes.groupSettings(testGroup.id))
    })

    it.each(languages)('has a Members link to the members of this group (%s)', async (language) => {
      await renderGroup({ language })
      await showsGroupScreen()

      const link = screen.getByRole('link', { name: translated(language, 'members.title') })

      expect(link.getAttribute('href')).toBe(routes.groupMembers(testGroup.id))
    })

    it('has the Members link for a member who is not the owner too', async () => {
      await renderGroup({ group: groupOf({ isOwner: false, memberCount: 3 }) })
      await showsGroupScreen()

      expect(screen.getByRole('link', { name: translated('en', 'members.title') })).toBeTruthy()
    })

    it('has the Members link when the group has several people', async () => {
      await renderGroup({ group: groupOf({ memberCount: 4 }) })
      await showsGroupScreen()

      expect(screen.getByRole('link', { name: translated('en', 'members.title') })).toBeTruthy()
    })

    it('opens the members of the group when the Members link is pressed', async () => {
      const { router } = await renderGroup()
      await showsGroupScreen()

      fireEvent.click(screen.getByRole('link', { name: translated('en', 'members.title') }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groupMembers(testGroup.id))
      })
    })

    it('has the Group settings link for a member who is not the owner too', async () => {
      await renderGroup({ group: groupOf({ isOwner: false, memberCount: 3 }) })
      await showsGroupScreen()

      expect(screen.getByRole('link', { name: translated('en', 'group.settings') })).toBeTruthy()
    })

    it('opens the settings of the group when the Group settings link is pressed', async () => {
      const { router } = await renderGroup()
      await showsGroupScreen()

      fireEvent.click(screen.getByRole('link', { name: translated('en', 'group.settings') }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groupSettings(testGroup.id))
      })
    })

    it('no longer tells that the expenses will show up soon', async () => {
      await renderGroup()
      await showsGroupScreen()
      await findEmptyTitle()

      expect(screen.queryByText('Expenses will show up here soon.')).toBeNull()
      expect(screen.queryByText(/soon/i)).toBeNull()
    })
  })

  describe('the empty group with one person', () => {
    it.each(languages)('shows the receipt emoji, the title line and the hint line while the owner is alone and there are no expenses (%s)', async (language) => {
      await renderGroup({ language })
      await showsAddPeopleButtons(language)

      expectEmptyStateTexts(language)
    })

    it.each(emptyTextCases)('writes the empty text as a title line and a hint line, not as one sentence (%s)', async (language, title, hint, oldSentence) => {
      await renderGroup({ language })
      await showsAddPeopleButtons(language)

      expect(screen.getByText(title)).not.toBe(screen.getByText(hint))
      expect(screen.queryByText(oldSentence)).toBeNull()
    })

    it('writes the title line in bold and the hint line not in bold', async () => {
      await renderGroup()
      await showsAddPeopleButtons()

      expect(isBold(emptyTitle())).toBe(true)
      expect(isBold(emptyHint())).toBe(false)
    })

    it('centres the emoji, the title line and the hint line', async () => {
      await renderGroup()
      await showsAddPeopleButtons()
      const content = lowestCommonAncestor(screen.getByRole('heading', { level: 1 }), emptyTitle())

      for (const element of [screen.getByText(receiptEmoji), emptyTitle(), emptyHint()]) {
        expect(isCentred(element, content), `"${element.textContent}" should be centred`).toBe(true)
      }
    })

    it.each(languages)('keeps the order header, chips, pill switch, emoji, title, hint, buttons, explanation lines, Recently deleted link (%s)', async (language) => {
      await renderGroup({ language })
      await showsAddPeopleButtons(language)

      const order: [string, HTMLElement][] = [
        ['the name of the group', screen.getByRole('heading', { level: 1, name: testGroup.name })],
        ['the Members chip', screen.getByRole('link', { name: translated(language, 'members.title') })],
        ['the Group settings chip', screen.getByRole('link', { name: translated(language, 'group.settings') })],
        ['the Expenses tab', expensesTab(language)],
        ['the Activity tab', activityTab(language)],
        ['the receipt emoji', screen.getByText(receiptEmoji)],
        ['the title line', emptyTitle(language)],
        ['the hint line', emptyHint(language)],
        ['the Add a name button', addNameButton(language)],
        ['the Share invite link button', shareLinkButton(language)],
        ['the No Kvit line', noKvitLine(language)],
        ['the Have Kvit line', hasKvitLine(language)],
        ['the Recently deleted link', recentlyDeletedLink(language)],
      ]

      order.slice(1).forEach(([name, element], index) => {
        const [previousName, previous] = order[index]
        expect(isBefore(previous, element), `${name} should come after ${previousName}`).toBe(true)
      })
    })

    it.each(languages)('puts the two buttons side by side in one row of their own, Add a name first (%s)', async (language) => {
      await renderGroup({ language })
      await showsAddPeopleButtons(language)

      const row = lowestCommonAncestor(addNameButton(language), shareLinkButton(language))

      expect(row.textContent).toBe(
        `${translated(language, 'group.addName')}${translated(language, 'group.shareLink')}`,
      )
      expect(isBefore(addNameButton(language), shareLinkButton(language))).toBe(true)
    })

    it('lays the row of the two buttons out horizontally', async () => {
      await renderGroup()
      await showsAddPeopleButtons()

      const row = lowestCommonAncestor(addNameButton(), shareLinkButton())

      expect(row.classList.contains('flex') || row.classList.contains('grid')).toBe(true)
      expect(row.classList.contains('flex-col')).toBe(false)
    })

    it('shows Add a name as an outline button and Share invite link as a filled button', async () => {
      await renderGroup()
      await showsAddPeopleButtons()

      expect(addNameButton().classList.contains('bg-transparent')).toBe(true)
      expect(shareLinkButton().classList.contains('bg-transparent')).toBe(false)
    })

    it.each(languages)('shows the two explanation lines under the buttons, each in its own element and outside the button row (%s)', async (language) => {
      await renderGroup({ language })
      await showsAddPeopleButtons(language)
      const row = lowestCommonAncestor(addNameButton(language), shareLinkButton(language))

      for (const line of [noKvitLine(language), hasKvitLine(language)]) {
        expect(isBefore(shareLinkButton(language), line)).toBe(true)
        expect(row.contains(line)).toBe(false)
      }

      expect(noKvitLine(language)).not.toBe(hasKvitLine(language))
      expect(isBefore(noKvitLine(language), hasKvitLine(language))).toBe(true)
    })

    it('writes the two explanation lines as small centred text', async () => {
      await renderGroup()
      await showsAddPeopleButtons()
      const content = lowestCommonAncestor(screen.getByRole('heading', { level: 1 }), noKvitLine())

      for (const line of [noKvitLine(), hasKvitLine()]) {
        expect(isCentred(line, content), `"${line.textContent}" should be centred`).toBe(true)
        expect(isSmallText(line, content), `"${line.textContent}" should be small, its classes are: ${line.className}`).toBe(true)
      }
    })

    it.each(languages)('has no Add people heading and no card around the title, the buttons or the explanation lines (%s)', async (language) => {
      await renderGroup({ language })
      await showsAddPeopleButtons(language)
      const content = lowestCommonAncestor(screen.getByRole('heading', { level: 1 }), emptyTitle(language))

      expectNoAddPeopleHeading(language)
      for (const element of [emptyTitle(language), addNameButton(language), noKvitLine(language), hasKvitLine(language)]) {
        for (const ancestor of ancestorsBelow(element, content)) {
          expect(ancestor.classList.contains('bg-card'), `"${element.textContent}" sits in a card: ${ancestor.className}`).toBe(false)
        }
      }
    })

    it.each([
      ['en', 'No Kvit? Add them as a name.', 'Add a name', 'Have Kvit? Share the link and they join with their own account.', 'Share invite link'],
      ['mk', 'Немаат Kvit? Додај ги како име.', 'Додај име', 'Имаат Kvit? Сподели го линкот и ќе се придружат со свој профил.', 'Сподели линк за покана'],
    ] as const)('writes the approved texts of both explanation lines and both buttons (%s)', async (language, noKvit, addName, hasKvit, share) => {
      await renderGroup({ language })
      await showsAddPeopleButtons(language)

      expect(screen.getByText(noKvit)).toBeTruthy()
      expect(screen.getByRole('button', { name: addName })).toBeTruthy()
      expect(screen.getByText(hasKvit)).toBeTruthy()
      expect(screen.getByRole('button', { name: share })).toBeTruthy()
    })

    it('has exactly one Share invite link button, the one in the row and not one in the header', async () => {
      await renderGroup()
      await showsAddPeopleButtons()

      const buttons = screen.getAllByRole('button', { name: translated('en', 'group.shareLink') })

      expect(buttons).toHaveLength(1)
      expect(isBefore(expensesTab(), buttons[0])).toBe(true)
    })

    it('shows the buttons to a member who is not the owner while the group has one person', async () => {
      await renderGroup({ group: groupOf({ isOwner: false, memberCount: 1 }) })

      await showsAddPeopleButtons()

      expect(shareLinkButton()).toBeTruthy()
      expect(addNameButton()).toBeTruthy()
    })

    it('keeps the dark-mode ring on the pill switch', async () => {
      await renderGroup()
      await showsAddPeopleButtons()

      const pillSwitch = expensesTab().parentElement

      if (pillSwitch === null) {
        throw new Error('The Expenses tab is not inside the pill switch')
      }
      expect(pillSwitch.classList.contains('dark:inset-ring')).toBe(true)
    })
  })

  describe('the empty group with several people or expenses', () => {
    it.each([2, 3, 12])('shows the emoji, the title line and the hint line but no buttons and no explanation lines when the group has %i people', async (memberCount) => {
      await renderGroup({ group: groupOf({ memberCount }) })
      await showsGroupScreen()
      await findEmptyTitle()

      expectEmptyStateTexts()
      expectNoAddPeopleActions()
    })

    it('shows the same in Macedonian when the group has two people', async () => {
      await renderGroup({ language: 'mk', group: groupOf({ memberCount: 2 }) })
      await showsGroupScreen()
      await findEmptyTitle('mk')

      expectEmptyStateTexts('mk')
      expectNoAddPeopleActions('mk')
    })

    it.each(languages)('shows none of the empty state, the buttons or the explanation lines when the group has one person and already has an expense (%s)', async (language) => {
      await renderGroup({
        language,
        answers: { [`GET ${expensesPath}`]: jsonAnswer({ expenses: [dinnerRow] }) },
      })
      await showsGroupScreen()
      await screen.findByRole('link', { name: /Dinner/ })

      expectNoEmptyStateTexts(language)
      expectNoAddPeopleActions(language)
    })
  })

  describe('the empty group while loading and failing', () => {
    it.each([
      ['the expenses', expensesPath],
      ['the categories', categoriesPath],
    ])('shows no empty state, no buttons and no explanation lines while %s are still being asked', async (_name, neverAnsweredPath) => {
      stubGroupWithOneRequestNeverAnswered(neverAnsweredPath)
      await renderRoutesWithProviders(
        [{ path: '/groups/:groupId', element: <GroupScreen /> }],
        routes.group(testGroup.id),
        { seedCache: seedMe(testMe) },
      )
      await showsGroupScreen()

      expect(screen.getByRole('status')).toBeTruthy()
      await letPendingWorkFinish()
      expectNoEmptyStateTexts()
      expectNoAddPeopleActions()
    })

    it.each([
      ['the expenses', expensesPath],
      ['the categories', categoriesPath],
    ])('shows the error and no empty state, no buttons and no explanation lines when %s cannot be loaded', async (_name, failingPath) => {
      await renderGroup({ answers: { [`GET ${failingPath}`]: () => new Response(null, { status: 500 }) } })

      await screen.findByRole('alert')

      expectNoEmptyStateTexts()
      expectNoAddPeopleActions()
    })
  })

  describe('adding a name', () => {
    it('shows no sheet before Add a name is pressed', async () => {
      await renderGroup()
      await showsAddPeopleButtons()

      expect(screen.queryByRole('dialog')).toBeNull()
    })

    it.each(languages)('opens a sheet with the name field, its hint and the Add button when Add a name is pressed (%s)', async (language) => {
      await renderGroup({ language })
      await showsAddPeopleButtons(language)

      await openSheet(language)

      expect(within(sheet(language)).getByLabelText(translated(language, 'addName.label'))).toBeTruthy()
      expect(within(sheet(language)).getByText(translated(language, 'addName.hint'))).toBeTruthy()
      expect(sheetAddButton(language)).toBeTruthy()
    })

    it('sends the typed name to the members endpoint of the group', async () => {
      const { fetchMock } = await renderGroup({
        answers: { [`POST ${membersPath}`]: noContentAnswer() },
      })
      await showsAddPeopleButtons()
      await openSheet()
      typeName('Grandma')

      fireEvent.click(sheetAddButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', membersPath)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', membersPath)[0]).toMatchObject({
        contentType: 'application/json',
        body: { name: 'Grandma' },
      })
    })

    it('sends the typed name when the form is submitted with the Enter key', async () => {
      const { fetchMock } = await renderGroup({
        answers: { [`POST ${membersPath}`]: noContentAnswer() },
      })
      await showsAddPeopleButtons()
      await openSheet()
      typeName('Grandma')
      const form = within(sheet()).getByLabelText<HTMLInputElement>(translated('en', 'addName.label')).form
      if (form === null) {
        throw new Error('The name field is not inside a form')
      }

      fireEvent.submit(form)

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', membersPath)).toBe(1)
      })
    })

    it('closes the sheet and shows the new head count after the name is added', async () => {
      let isNameAdded = false
      await renderGroup({
        answers: {
          [`GET ${groupPath}`]: () => Response.json(isNameAdded ? groupOf({ memberCount: 2 }) : testGroup),
          [`POST ${membersPath}`]: () => {
            isNameAdded = true
            return Response.json({ id: 'new-member', name: 'Grandma' })
          },
        },
      })
      await showsAddPeopleButtons()
      await openSheet()
      typeName('Grandma')

      fireEvent.click(sheetAddButton())

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      expect(await screen.findByText('2 people · MKD')).toBeTruthy()
    })

    it('takes the two buttons and the explanation lines away once the group has a second person and keeps the empty state', async () => {
      let isNameAdded = false
      await renderGroup({
        answers: {
          [`GET ${groupPath}`]: () => Response.json(isNameAdded ? groupOf({ memberCount: 2 }) : testGroup),
          [`POST ${membersPath}`]: () => {
            isNameAdded = true
            return Response.json({ id: 'new-member', name: 'Grandma' })
          },
        },
      })
      await showsAddPeopleButtons()
      await openSheet()
      typeName('Grandma')

      fireEvent.click(sheetAddButton())

      await waitFor(() => {
        expectNoAddPeopleActions()
      })
      expectEmptyStateTexts()
    })

    it('disables the Add button while the request waits for an answer', async () => {
      await renderGroup()
      await showsAddPeopleButtons()
      await openSheet()
      typeName('Grandma')
      stubFetchThatNeverAnswers()

      fireEvent.click(sheetAddButton())

      await expectDisabledWhilePending(sheetAddButton())
    })

    it.each(languages)('shows the empty-name message inside the sheet and keeps it open (%s)', async (language) => {
      await renderGroup({
        language,
        answers: { [`POST ${membersPath}`]: problemAnswer(400, 'MEMBER_NAME_INVALID') },
      })
      await showsAddPeopleButtons(language)
      await openSheet(language)

      fireEvent.click(sheetAddButton(language))

      expect(
        (await within(sheet(language)).findByRole('alert')).textContent,
      ).toContain(translated(language, 'errors.MEMBER_NAME_INVALID'))
    })

    it.each(languages)('shows the name-taken message with the typed name inside the sheet, keeps the sheet open and keeps the typed name (%s)', async (language) => {
      await renderGroup({
        language,
        answers: { [`POST ${membersPath}`]: problemAnswer(400, 'MEMBER_NAME_TAKEN') },
      })
      await showsAddPeopleButtons(language)
      await openSheet(language)
      typeName('Marko', language)

      fireEvent.click(sheetAddButton(language))

      expect(
        (await within(sheet(language)).findByRole('alert')).textContent,
      ).toContain(translated(language, 'errors.MEMBER_NAME_TAKEN', { name: 'Marko' }))
      expect(
        within(sheet(language)).getByLabelText<HTMLInputElement>(translated(language, 'addName.label')).value,
      ).toBe('Marko')
    })

    it.each([
      ['en', 'Someone called “Marko” is already in this group.'],
      ['mk', 'Некој по име „Marko“ е веќе во групата.'],
    ] as const)('words the name-taken message exactly with the typed name between the quotes (%s)', async (language, text) => {
      await renderGroup({
        language,
        answers: { [`POST ${membersPath}`]: problemAnswer(400, 'MEMBER_NAME_TAKEN') },
      })
      await showsAddPeopleButtons(language)
      await openSheet(language)
      typeName('Marko', language)

      fireEvent.click(sheetAddButton(language))

      expect((await within(sheet(language)).findByRole('alert')).textContent).toBe(text)
    })

    it('shows the name in the name-taken message as it was typed, with its capitals and punctuation', async () => {
      await renderGroup({
        answers: { [`POST ${membersPath}`]: problemAnswer(400, 'MEMBER_NAME_TAKEN') },
      })
      await showsAddPeopleButtons()
      await openSheet()
      typeName("mARKO o'Brien & Co.")

      fireEvent.click(sheetAddButton())

      expect((await within(sheet()).findByRole('alert')).textContent).toBe(
        'Someone called “mARKO o\'Brien & Co.” is already in this group.',
      )
    })

    it.each(languages)('does not mark the name field as invalid before a name was refused (%s)', async (language) => {
      await renderGroup({ language })
      await showsAddPeopleButtons(language)
      await openSheet(language)

      expect(nameField(language).getAttribute('aria-invalid')).not.toBe('true')
    })

    it.each([
      ['MEMBER_NAME_TAKEN', 'Marko'],
      ['MEMBER_NAME_INVALID', ''],
    ])('marks the name field with aria-invalid="true" when the server refuses the name with %s', async (code, typedName) => {
      await renderGroup({ answers: { [`POST ${membersPath}`]: problemAnswer(400, code) } })
      await showsAddPeopleButtons()
      await openSheet()
      typeName(typedName)

      fireEvent.click(sheetAddButton())

      await within(sheet()).findByRole('alert')
      expect(nameField().getAttribute('aria-invalid')).toBe('true')
    })

    it.each(languages)('marks the name field with aria-invalid="true" in %s too', async (language) => {
      await renderGroup({
        language,
        answers: { [`POST ${membersPath}`]: problemAnswer(400, 'MEMBER_NAME_TAKEN') },
      })
      await showsAddPeopleButtons(language)
      await openSheet(language)
      typeName('Marko', language)

      fireEvent.click(sheetAddButton(language))

      await within(sheet(language)).findByRole('alert')
      expect(nameField(language).getAttribute('aria-invalid')).toBe('true')
    })

    it('clears the aria-invalid mark of the name field as soon as the typed text changes', async () => {
      await renderGroup({
        answers: { [`POST ${membersPath}`]: problemAnswer(400, 'MEMBER_NAME_TAKEN') },
      })
      await showsAddPeopleButtons()
      await openSheet()
      typeName('Marko')
      fireEvent.click(sheetAddButton())
      await within(sheet()).findByRole('alert')

      typeName('Marko P')

      expect(nameField().getAttribute('aria-invalid')).not.toBe('true')
    })

    it('shows the network message inside the sheet when the server cannot be reached', async () => {
      await renderGroup({ answers: { [`POST ${membersPath}`]: networkFailureAnswer() } })
      await showsAddPeopleButtons()
      await openSheet()
      typeName('Grandma')

      fireEvent.click(sheetAddButton())

      expect((await within(sheet()).findByRole('alert')).textContent).toContain(
        translated('en', 'errors.network'),
      )
    })

    it('closes the sheet when Escape is pressed', async () => {
      await renderGroup()
      await showsAddPeopleButtons()
      await openSheet()

      fireEvent.keyDown(sheet(), { key: 'Escape' })

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
    })

    it('closes the sheet when its Close button is pressed', async () => {
      await renderGroup()
      await showsAddPeopleButtons()
      await openSheet()

      fireEvent.click(within(sheet()).getByRole('button', { name: translated('en', 'common.close') }))

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
    })

    it('shows the field empty and without an error when the sheet is opened again', async () => {
      await renderGroup({
        answers: { [`POST ${membersPath}`]: problemAnswer(400, 'MEMBER_NAME_TAKEN') },
      })
      await showsAddPeopleButtons()
      await openSheet()
      typeName('Marko')
      fireEvent.click(sheetAddButton())
      await within(sheet()).findByRole('alert')
      fireEvent.keyDown(sheet(), { key: 'Escape' })
      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })

      await openSheet()

      expect(
        within(sheet()).getByLabelText<HTMLInputElement>(translated('en', 'addName.label')).value,
      ).toBe('')
      expect(within(sheet()).queryByRole('alert')).toBeNull()
    })
  })

  describe('sharing the invite link', () => {
    it('opens the share menu of the phone with the invite link of the group', async () => {
      const share = stubShare(async () => {})
      await renderGroup()
      await showsAddPeopleButtons()

      fireEvent.click(shareLinkButton())

      await waitFor(() => {
        expect(share).toHaveBeenCalledOnce()
      })
      expect(share).toHaveBeenCalledWith(expect.objectContaining({ url: inviteUrl }))
    })

    it('uses the invite token of the group that is open', async () => {
      const share = stubShare(async () => {})
      await renderGroup({ group: groupOf({ inviteToken: 'another-token_42' }) })
      await showsAddPeopleButtons()

      fireEvent.click(shareLinkButton())

      await waitFor(() => {
        expect(share).toHaveBeenCalledWith(
          expect.objectContaining({ url: `${window.location.origin}/join/another-token_42` }),
        )
      })
    })

    it('does not copy the link when the share menu is available', async () => {
      stubShare(async () => {})
      const writeText = stubClipboard(async () => {})
      await renderGroup()
      await showsAddPeopleButtons()

      fireEvent.click(shareLinkButton())
      await letPendingWorkFinish()

      expect(writeText).not.toHaveBeenCalled()
    })

    it('shows no toast when the share menu was shared through', async () => {
      stubShare(async () => {})
      await renderGroup()
      await showsAddPeopleButtons()

      fireEvent.click(shareLinkButton())
      await letPendingWorkFinish()

      expect(shownToastTexts()).toEqual([])
      expect(toast.error).not.toHaveBeenCalled()
    })

    it('does nothing more when the person closes the share menu without sharing', async () => {
      const share = stubShare(async () => {
        throw abortError()
      })
      await renderGroup()
      await showsAddPeopleButtons()

      fireEvent.click(shareLinkButton())
      await letPendingWorkFinish()

      expect(share).toHaveBeenCalledOnce()
      expect(shownToastTexts()).toEqual([])
      expect(toast.error).not.toHaveBeenCalled()
    })

    it('shows the generic error toast when sharing fails for another reason', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      stubShare(async () => {
        throw new Error('The share menu crashed')
      })
      await renderGroup()
      await showsAddPeopleButtons()

      fireEvent.click(shareLinkButton())

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.generic'))
      })
    })

    it.each(languages)('copies the link to the clipboard and shows the Link copied toast when there is no share menu (%s)', async (language) => {
      const writeText = stubClipboard(async () => {})
      await renderGroup({ language })
      await showsAddPeopleButtons(language)

      fireEvent.click(shareLinkButton(language))

      await waitFor(() => {
        expect(shownToastTexts()).toContain(translated(language, 'group.linkCopied'))
      })
      expect(writeText).toHaveBeenCalledExactlyOnceWith(inviteUrl)
    })

    it('shows the generic error toast and not Link copied when the clipboard refuses the link', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      stubClipboard(async () => {
        throw new DOMException('Clipboard blocked', 'NotAllowedError')
      })
      await renderGroup()
      await showsAddPeopleButtons()

      fireEvent.click(shareLinkButton())

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.generic'))
      })
      expect(shownToastTexts()).not.toContain(translated('en', 'group.linkCopied'))
    })

    it('shows the generic error toast and not Link copied when the phone has neither a share menu nor a clipboard', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderGroup()
      await showsAddPeopleButtons()

      fireEvent.click(shareLinkButton())

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.generic'))
      })
      expect(shownToastTexts()).not.toContain(translated('en', 'group.linkCopied'))
    })
  })

  it('does not send any request besides asking for the group, its expenses and the categories when nothing is pressed', async () => {
    const { fetchMock } = await renderGroup()
    await showsGroupScreen()
    await findEmptyTitle()

    expect(fetchMock.mock.calls.map(([url]) => String(url)).sort()).toEqual(
      [groupPath, expensesPath, categoriesPath].sort(),
    )
  })
})
