import { screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { supportEmail } from '@/core/legal/legalInfo'
import { routes } from '@/core/router/routes'
import type { Group } from '@/core/services/groups/groupsService'
import type { Me } from '@/core/services/me/meService'
import { stubFetchThatNeverAnswers } from '@/test/apiTestHelpers'
import { asMemberOf, closingGroup, finishedGroup } from '@/test/closingTestData'
import { flatGroupRow, groupOf, testGroup } from '@/test/groupTestData'
import { mailtoLinks } from '@/test/legalScreenTestHelpers'
import { parseMailto } from '@/test/mailtoTestHelpers'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import {
  jsonAnswer,
  networkFailureAnswer,
  problemAnswer,
  stubFetchByRequest,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { seedMe, testGuestMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { GroupSettingsScreen } from './GroupSettingsScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const openOwnerGroup = groupOf({ memberCount: 3 })
const openMemberGroup = groupOf({ isOwner: false, memberCount: 3 })

const views = [
  ['the owner of an open group', openOwnerGroup, testMe],
  ['a member of an open group', openMemberGroup, testMe],
  ['a guest in an open group', openMemberGroup, testGuestMe],
  ['the owner of a finished group', finishedGroup, testMe],
  ['a member of a finished group', asMemberOf(finishedGroup), testMe],
  ['the owner of a group that is being closed', closingGroup, testMe],
  ['a member of a group that is being closed', asMemberOf(closingGroup), testMe],
] as const

const viewsWithLanguage = views.flatMap(([name, group, me]) =>
  languages.map((language) => [`${name} (${language})`, group, me, language] as const),
)

interface RenderOptions {
  language?: Language
  group?: Group
  me?: Me
  answers?: Record<string, AnswerFactory>
}

async function renderSettings(options: RenderOptions = {}) {
  const group = options.group ?? openOwnerGroup
  const language = options.language ?? 'en'
  const me = options.me ?? testMe
  const fetchMock = stubFetchByRequest({
    [`GET /api/groups/${group.id}`]: jsonAnswer(group),
    ...options.answers,
  })
  const rendered = await renderRoutesWithProviders(
    [
      { path: '/groups/:groupId/settings', element: <GroupSettingsScreen /> },
      { path: '/groups/:groupId', element: <p>group page</p> },
    ],
    routes.groupSettings(group.id),
    { language, seedCache: seedMe({ ...me, language }) },
  )
  return { fetchMock, ...rendered }
}

async function showsSettings(language: Language = 'en'): Promise<void> {
  await screen.findByRole('heading', {
    level: 1,
    name: translated(language, 'groupSettings.title'),
  })
}

function reportLink(language: Language = 'en'): HTMLAnchorElement {
  return screen.getByRole<HTMLAnchorElement>('link', {
    name: translated(language, 'groupSettings.report'),
  })
}

function reportHref(language: Language = 'en'): string {
  const href = reportLink(language).getAttribute('href')
  if (href === null) {
    throw new Error('The Report this group link has no href')
  }
  return href
}

function expectedBody(language: Language, groupId: string): string {
  return translated(language, 'groupSettings.reportBody', { id: groupId }).replaceAll('\n', '\r\n')
}

function isAfter(later: Node, earlier: Node): boolean {
  return Boolean(earlier.compareDocumentPosition(later) & Node.DOCUMENT_POSITION_FOLLOWING)
}

describe('the Report this group link on the Group settings screen', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it.each(viewsWithLanguage)('is shown to %s', async (_name, group, me, language) => {
    await renderSettings({ group, me, language })
    await showsSettings(language)

    expect(reportLink(language)).toBeTruthy()
  })

  it.each(languages)('is a mailto link to the support address with only a subject and a body (%s)', async (language) => {
    await renderSettings({ language })
    await showsSettings(language)

    const parsed = parseMailto(reportHref(language))

    expect(parsed.address).toBe(supportEmail)
    expect(parsed.parameterNames).toEqual(['subject', 'body'])
  })

  it.each(languages)('has the subject in the language of the app (%s)', async (language) => {
    await renderSettings({ language })
    await showsSettings(language)

    expect(parseMailto(reportHref(language)).parameters.subject).toBe(
      translated(language, 'groupSettings.reportSubject'),
    )
  })

  it.each(languages)('has the body in the language of the app with the group id filled in and unchanged, and CR LF line breaks (%s)', async (language) => {
    await renderSettings({ language })
    await showsSettings(language)

    const body = parseMailto(reportHref(language)).parameters.body

    expect(body).toBe(expectedBody(language, testGroup.id))
    expect(body).toContain(testGroup.id)
  })

  it.each(languages)('writes the whole link exactly as mailto:address?subject=...&body=... with both texts passed through encodeURIComponent (%s)', async (language) => {
    await renderSettings({ language })
    await showsSettings(language)

    expect(reportHref(language)).toBe(
      `mailto:${supportEmail}` +
        `?subject=${encodeURIComponent(translated(language, 'groupSettings.reportSubject'))}` +
        `&body=${encodeURIComponent(expectedBody(language, testGroup.id))}`,
    )
  })

  it('writes each line break of the body as %0D%0A in the link', async () => {
    await renderSettings()
    await showsSettings()

    expect(reportHref()).toContain('%0D%0A')
    expect(reportHref()).not.toMatch(/(?<!%0D)%0A/)
  })

  it('uses the id of the group on the screen and not a fixed one', async () => {
    const otherGroup = groupOf({ id: flatGroupRow.id, name: flatGroupRow.name })
    await renderSettings({ group: otherGroup })
    await showsSettings()

    const body = parseMailto(reportHref()).parameters.body

    expect(body).toContain(flatGroupRow.id)
    expect(body).not.toContain(testGroup.id)
  })

  it.each(views)('has the grey hint under the link for %s', async (_name, group, me) => {
    await renderSettings({ group, me })
    await showsSettings()

    const hint = screen.getByText(translated('en', 'groupSettings.reportHint'))

    expect(isAfter(hint, reportLink())).toBe(true)
    expect(hint.closest('a')).toBeNull()
  })

  it.each(languages)('writes the hint in the language of the app (%s)', async (language) => {
    await renderSettings({ language })
    await showsSettings(language)

    expect(screen.getByText(translated(language, 'groupSettings.reportHint'))).toBeTruthy()
  })

  it.each([
    ['the owner', openOwnerGroup],
    ['a member who is not the owner', openMemberGroup],
  ])('comes after every button on the screen, so it is the last thing for %s', async (_name, group) => {
    await renderSettings({ group })
    await showsSettings()
    const link = reportLink()

    const buttons = screen.getAllByRole('button')

    expect(buttons.length).toBeGreaterThan(0)
    for (const button of buttons) {
      expect(isAfter(link, button)).toBe(true)
    }
  })

  it('comes after the Leave group button', async () => {
    await renderSettings({ group: openMemberGroup })
    await showsSettings()

    const leave = screen.getByRole('button', { name: translated('en', 'groupSettings.leave') })

    expect(isAfter(reportLink(), leave)).toBe(true)
  })

  it('is the only mailto link on the screen', async () => {
    await renderSettings({ group: openMemberGroup })
    await showsSettings()

    expect(mailtoLinks()).toEqual([reportLink()])
  })

  it('asks the server for nothing but the group, so showing the link sends no request of its own', async () => {
    const { fetchMock } = await renderSettings()
    await showsSettings()

    expect(fetchMock.mock.calls.map(([url]) => String(url))).toEqual([`/api/groups/${testGroup.id}`])
  })

  it('is not shown while the group is still loading', async () => {
    stubFetchThatNeverAnswers()
    await renderRoutesWithProviders(
      [{ path: '/groups/:groupId/settings', element: <GroupSettingsScreen /> }],
      routes.groupSettings(testGroup.id),
      { seedCache: seedMe(testMe) },
    )

    expect(screen.getByRole('status')).toBeTruthy()
    expect(screen.queryByRole('link', { name: translated('en', 'groupSettings.report') })).toBeNull()
    expect(screen.queryByText(translated('en', 'groupSettings.reportHint'))).toBeNull()
  })

  it.each([
    ['the group is not found', problemAnswer(404, 'GROUP_NOT_FOUND')],
    ['the server fails', () => new Response(null, { status: 500 })],
    ['the network fails', networkFailureAnswer()],
  ])('is not shown when %s', async (_name, answer) => {
    await renderSettings({ answers: { [`GET /api/groups/${testGroup.id}`]: answer } })
    await screen.findByRole('alert')

    expect(screen.queryByRole('link', { name: translated('en', 'groupSettings.report') })).toBeNull()
    expect(screen.queryByText(translated('en', 'groupSettings.reportHint'))).toBeNull()
  })
})
