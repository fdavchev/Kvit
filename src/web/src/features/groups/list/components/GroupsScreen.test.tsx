import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import { stubFetchThatNeverAnswers } from '@/test/apiTestHelpers'
import {
  dinnerDeletedRow,
  emptyGroupList,
  flatGroupRow,
  greeceGroupRow,
  groupListOf,
  summerFinishedRow,
} from '@/test/groupTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import {
  jsonAnswer,
  networkFailureAnswer,
  stubFetchByRequest,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { GroupsScreen } from './GroupsScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const openGroupsList = groupListOf({ groups: [greeceGroupRow, flatGroupRow] })

async function renderGroups(language: Language = 'en') {
  return renderRoutesWithProviders(
    [
      { path: routes.groups, element: <GroupsScreen /> },
      { path: routes.newGroup, element: <p>new group page</p> },
      { path: routes.recentlyDeletedGroups, element: <p>recently deleted page</p> },
      { path: '/groups/:groupId', element: <p>group page</p> },
    ],
    routes.groups,
    { language, seedCache: seedMe({ ...testMe, language }) },
  )
}

function stubGroupList(answer: AnswerFactory): void {
  stubFetchByRequest({ 'GET /api/groups': answer })
}

function finishedToggle(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', {
    name: new RegExp(`^${translated(language, 'groups.finished')}`),
  })
}

function recentlyDeletedLink(language: Language = 'en'): HTMLElement {
  return screen.getByRole('link', {
    name: new RegExp(translated(language, 'groups.recentlyDeletedLink')),
  })
}

function newGroupLink(language: Language = 'en'): HTMLElement {
  return screen.getByRole('link', { name: translated(language, 'groups.newButton') })
}

function rowLink(name: string): HTMLElement {
  return screen.getByRole('link', { name: new RegExp(name) })
}

function isBefore(first: HTMLElement, second: HTMLElement): boolean {
  return Boolean(first.compareDocumentPosition(second) & Node.DOCUMENT_POSITION_FOLLOWING)
}

describe('GroupsScreen', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('shows the Groups heading (%s)', async (language) => {
    stubGroupList(jsonAnswer(openGroupsList))
    await renderGroups(language)

    expect(
      await screen.findByRole('heading', { name: translated(language, 'groups.title') }),
    ).toBeTruthy()
  })

  it('shows the loading spinner while the groups are being asked', async () => {
    stubFetchThatNeverAnswers()

    await renderGroups()

    expect(screen.getByRole('status')).toBeTruthy()
  })

  it('shows one link per open group to the group screen', async () => {
    stubGroupList(jsonAnswer(openGroupsList))
    await renderGroups()

    await screen.findByText(greeceGroupRow.name)

    expect(rowLink(greeceGroupRow.name).getAttribute('href')).toBe(routes.group(greeceGroupRow.id))
    expect(rowLink(flatGroupRow.name).getAttribute('href')).toBe(routes.group(flatGroupRow.id))
  })

  it('shows the open groups in the order the server gave them', async () => {
    stubGroupList(jsonAnswer(openGroupsList))
    await renderGroups()

    await screen.findByText(greeceGroupRow.name)

    expect(isBefore(screen.getByText(greeceGroupRow.name), screen.getByText(flatGroupRow.name))).toBe(true)
  })

  it('shows the emoji of each open group', async () => {
    stubGroupList(jsonAnswer(openGroupsList))
    await renderGroups()

    await screen.findByText(greeceGroupRow.name)

    expect(screen.getByText(greeceGroupRow.emoji)).toBeTruthy()
    expect(screen.getByText(flatGroupRow.emoji)).toBeTruthy()
  })

  it.each([
    ['en', '1 person · MKD', '3 people · EUR'],
    ['mk', '1 лице · MKD', '3 лица · EUR'],
  ] as const)('shows how many people and which currency under each open group (%s)', async (language, greece, flat) => {
    stubGroupList(jsonAnswer(openGroupsList))
    await renderGroups(language)

    expect(await screen.findByText(greece)).toBeTruthy()
    expect(screen.getByText(flat)).toBeTruthy()
  })

  it('opens the group screen when a group is pressed', async () => {
    stubGroupList(jsonAnswer(openGroupsList))
    const { router } = await renderGroups()
    await screen.findByText(flatGroupRow.name)

    fireEvent.click(rowLink(flatGroupRow.name))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.group(flatGroupRow.id))
    })
  })

  it.each(languages)('has a New group link to the new-group screen (%s)', async (language) => {
    stubGroupList(jsonAnswer(openGroupsList))
    await renderGroups(language)
    await screen.findByText(greeceGroupRow.name)

    expect(newGroupLink(language).getAttribute('href')).toBe(routes.newGroup)
  })

  it('opens the new-group screen when New group is pressed', async () => {
    stubGroupList(jsonAnswer(openGroupsList))
    const { router } = await renderGroups()
    await screen.findByText(greeceGroupRow.name)

    fireEvent.click(newGroupLink())

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.newGroup)
    })
  })

  it.each(languages)('shows the empty text and still the New group link when there are no groups (%s)', async (language) => {
    stubGroupList(jsonAnswer(emptyGroupList))
    await renderGroups(language)

    expect(await screen.findByText(translated(language, 'groups.empty'))).toBeTruthy()
    expect(newGroupLink(language)).toBeTruthy()
  })

  it('does not show the empty text when there are open groups', async () => {
    stubGroupList(jsonAnswer(openGroupsList))
    await renderGroups()
    await screen.findByText(greeceGroupRow.name)

    expect(screen.queryByText(translated('en', 'groups.empty'))).toBeNull()
  })

  it('has no Finished row when there are no finished groups', async () => {
    stubGroupList(jsonAnswer(openGroupsList))
    await renderGroups()
    await screen.findByText(greeceGroupRow.name)

    expect(screen.queryByRole('button', { name: new RegExp(translated('en', 'groups.finished')) })).toBeNull()
  })

  it.each(languages)('has a Finished row that starts collapsed when there are finished groups (%s)', async (language) => {
    stubGroupList(jsonAnswer(groupListOf({ groups: [greeceGroupRow], finishedGroups: [summerFinishedRow] })))
    await renderGroups(language)
    await screen.findByText(greeceGroupRow.name)

    expect(finishedToggle(language).getAttribute('aria-expanded')).toBe('false')
    expect(screen.queryByText(summerFinishedRow.name)).toBeNull()
  })

  it('shows the finished groups as read-only links when the Finished row is pressed', async () => {
    stubGroupList(jsonAnswer(groupListOf({ groups: [greeceGroupRow], finishedGroups: [summerFinishedRow] })))
    await renderGroups()
    await screen.findByText(greeceGroupRow.name)

    fireEvent.click(finishedToggle())

    expect(finishedToggle().getAttribute('aria-expanded')).toBe('true')
    expect(rowLink(summerFinishedRow.name).getAttribute('href')).toBe(routes.group(summerFinishedRow.id))
    expect(screen.getByText(translated('en', 'groups.finishedReadOnly'))).toBeTruthy()
  })

  it('hides the finished groups again when the Finished row is pressed twice', async () => {
    stubGroupList(jsonAnswer(groupListOf({ groups: [greeceGroupRow], finishedGroups: [summerFinishedRow] })))
    await renderGroups()
    await screen.findByText(greeceGroupRow.name)

    fireEvent.click(finishedToggle())
    fireEvent.click(finishedToggle())

    expect(finishedToggle().getAttribute('aria-expanded')).toBe('false')
    expect(screen.queryByText(summerFinishedRow.name)).toBeNull()
  })

  it('does not show a finished group among the open ones before the Finished row is pressed', async () => {
    stubGroupList(jsonAnswer(groupListOf({ groups: [greeceGroupRow], finishedGroups: [summerFinishedRow] })))
    await renderGroups()
    await screen.findByText(greeceGroupRow.name)

    expect(screen.queryByRole('link', { name: new RegExp(summerFinishedRow.name) })).toBeNull()
  })

  it.each(languages)('has a Recently deleted link to its own screen (%s)', async (language) => {
    stubGroupList(jsonAnswer(openGroupsList))
    await renderGroups(language)
    await screen.findByText(greeceGroupRow.name)

    expect(recentlyDeletedLink(language).getAttribute('href')).toBe(routes.recentlyDeletedGroups)
  })

  it('shows the Recently deleted link even when nothing was deleted', async () => {
    stubGroupList(jsonAnswer(emptyGroupList))
    await renderGroups()
    await screen.findByText(translated('en', 'groups.empty'))

    expect(recentlyDeletedLink()).toBeTruthy()
  })

  it('opens the Recently deleted screen when its link is pressed', async () => {
    stubGroupList(jsonAnswer(openGroupsList))
    const { router } = await renderGroups()
    await screen.findByText(greeceGroupRow.name)

    fireEvent.click(recentlyDeletedLink())

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.recentlyDeletedGroups)
    })
  })

  it('does not list a deleted group among the groups', async () => {
    stubGroupList(
      jsonAnswer(groupListOf({ groups: [greeceGroupRow], recentlyDeleted: [dinnerDeletedRow] })),
    )
    await renderGroups()
    await screen.findByText(greeceGroupRow.name)

    expect(screen.queryByText(dinnerDeletedRow.name)).toBeNull()
  })

  it('puts the open groups first, then the Finished row, then the Recently deleted link', async () => {
    stubGroupList(
      jsonAnswer(
        groupListOf({
          groups: [greeceGroupRow, flatGroupRow],
          finishedGroups: [summerFinishedRow],
        }),
      ),
    )
    await renderGroups()
    await screen.findByText(greeceGroupRow.name)

    expect(isBefore(rowLink(flatGroupRow.name), finishedToggle())).toBe(true)
    expect(isBefore(finishedToggle(), recentlyDeletedLink())).toBe(true)
  })

  it.each([
    ['a server error', () => new Response(null, { status: 500 }), 'errors.generic'],
    ['a network failure', networkFailureAnswer(), 'errors.network'],
  ] as const)('shows the translated error with a retry button after %s', async (_name, answer, key) => {
    stubGroupList(answer)
    await renderGroups()

    expect((await screen.findByRole('alert')).textContent).toContain(translated('en', key))
    expect(screen.getByRole('button', { name: translated('en', 'common.retry') })).toBeTruthy()
  })

  it('asks for the groups again and shows them when the retry button is pressed', async () => {
    let tries = 0
    stubGroupList(() => {
      tries += 1
      return tries === 1 ? new Response(null, { status: 500 }) : Response.json(openGroupsList)
    })
    await renderGroups()

    fireEvent.click(await screen.findByRole('button', { name: translated('en', 'common.retry') }))

    expect(await screen.findByText(greeceGroupRow.name)).toBeTruthy()
    expect(screen.queryByRole('alert')).toBeNull()
  })
})
