import { fireEvent, screen, waitFor } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import { stubFetchThatNeverAnswers } from '@/test/apiTestHelpers'
import {
  dinnerDeletedRow,
  emptyGroupList,
  greeceGroupRow,
  groupListOf,
} from '@/test/groupTestData'
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
import { seedMe, testMe } from '@/test/testMe'
import { shownToastTexts } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'
import { RecentlyDeletedScreen } from './RecentlyDeletedScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const flatDeletedRow = {
  ...dinnerDeletedRow,
  id: 'b7a6c5d4-e3f2-4a1b-8c9d-0e1f2a3b4c5d',
  name: 'Flat 4B',
  emoji: '\u{1F3E0}',
  deletedAt: '2026-09-20T12:00:00Z',
  restorableUntil: '2026-10-20T12:00:00Z',
}

const twoDeletedGroups = groupListOf({
  groups: [greeceGroupRow],
  recentlyDeleted: [dinnerDeletedRow, flatDeletedRow],
})

const restoreDinnerPath = `/api/groups/${dinnerDeletedRow.id}/restore`
const restoreFlatPath = `/api/groups/${flatDeletedRow.id}/restore`

async function renderRecentlyDeleted(language: Language = 'en') {
  return renderRoutesWithProviders(
    [
      { path: routes.recentlyDeletedGroups, element: <RecentlyDeletedScreen /> },
      { path: routes.groups, element: <p>groups page</p> },
    ],
    routes.recentlyDeletedGroups,
    { language, seedCache: seedMe({ ...testMe, language }) },
  )
}

function stubRecentlyDeleted(
  listAnswer: AnswerFactory,
  extraAnswers: Record<string, AnswerFactory> = {},
) {
  return stubFetchByRequest({ 'GET /api/groups': listAnswer, ...extraAnswers })
}

function restoreButtons(language: Language = 'en'): HTMLElement[] {
  return screen.getAllByRole('button', { name: translated(language, 'recentlyDeleted.restore') })
}

describe('RecentlyDeletedScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it.each(languages)('shows the Recently deleted heading (%s)', async (language) => {
    stubRecentlyDeleted(jsonAnswer(twoDeletedGroups))
    await renderRecentlyDeleted(language)

    expect(
      await screen.findByRole('heading', { name: translated(language, 'recentlyDeleted.title') }),
    ).toBeTruthy()
  })

  it('has a back button to the groups list', async () => {
    stubRecentlyDeleted(jsonAnswer(twoDeletedGroups))
    await renderRecentlyDeleted()

    const back = await screen.findByRole('link', { name: translated('en', 'common.back') })

    expect(back.getAttribute('href')).toBe(routes.groups)
  })

  it.each(languages)('explains that deleted groups stay for 30 days and only the owner restores them (%s)', async (language) => {
    stubRecentlyDeleted(jsonAnswer(twoDeletedGroups))
    await renderRecentlyDeleted(language)

    expect(await screen.findByText(translated(language, 'recentlyDeleted.intro'))).toBeTruthy()
  })

  it('shows the loading spinner while the groups are being asked', async () => {
    stubFetchThatNeverAnswers()

    await renderRecentlyDeleted()

    expect(screen.getByRole('status')).toBeTruthy()
  })

  it('shows the name and the emoji of each deleted group', async () => {
    stubRecentlyDeleted(jsonAnswer(twoDeletedGroups))
    await renderRecentlyDeleted()

    expect(await screen.findByText(dinnerDeletedRow.name)).toBeTruthy()
    expect(screen.getByText(flatDeletedRow.name)).toBeTruthy()
    expect(screen.getByText(dinnerDeletedRow.emoji)).toBeTruthy()
    expect(screen.getByText(flatDeletedRow.emoji)).toBeTruthy()
  })

  it('does not show a group that is not deleted', async () => {
    stubRecentlyDeleted(jsonAnswer(twoDeletedGroups))
    await renderRecentlyDeleted()
    await screen.findByText(dinnerDeletedRow.name)

    expect(screen.queryByText(greeceGroupRow.name)).toBeNull()
  })

  it('shows each deleted group with its own restore-until date in English', async () => {
    stubRecentlyDeleted(jsonAnswer(twoDeletedGroups))
    await renderRecentlyDeleted()

    expect(await screen.findByText('Can be restored until 2 Nov 2026')).toBeTruthy()
    expect(screen.getByText('Can be restored until 20 Oct 2026')).toBeTruthy()
  })

  it('shows each deleted group with its own restore-until date in Macedonian', async () => {
    stubRecentlyDeleted(jsonAnswer(twoDeletedGroups))
    await renderRecentlyDeleted('mk')

    expect(await screen.findByText('Може да се врати до 2 ноември 2026')).toBeTruthy()
    expect(screen.getByText('Може да се врати до 20 октомври 2026')).toBeTruthy()
  })

  it.each(languages)('has one Restore button for each deleted group (%s)', async (language) => {
    stubRecentlyDeleted(jsonAnswer(twoDeletedGroups))
    await renderRecentlyDeleted(language)
    await screen.findByText(dinnerDeletedRow.name)

    expect(restoreButtons(language)).toHaveLength(2)
  })

  it('shows the empty text and no Restore button when nothing was deleted', async () => {
    stubRecentlyDeleted(jsonAnswer(emptyGroupList))
    await renderRecentlyDeleted()

    expect(await screen.findByText(translated('en', 'recentlyDeleted.empty'))).toBeTruthy()
    expect(
      screen.queryByRole('button', { name: translated('en', 'recentlyDeleted.restore') }),
    ).toBeNull()
  })

  it('does not show the empty text while there are deleted groups', async () => {
    stubRecentlyDeleted(jsonAnswer(twoDeletedGroups))
    await renderRecentlyDeleted()
    await screen.findByText(dinnerDeletedRow.name)

    expect(screen.queryByText(translated('en', 'recentlyDeleted.empty'))).toBeNull()
  })

  it('sends the restore request for the group whose Restore button is pressed', async () => {
    const fetchMock = stubRecentlyDeleted(jsonAnswer(twoDeletedGroups), {
      [`POST ${restoreFlatPath}`]: noContentAnswer(),
    })
    await renderRecentlyDeleted()
    await screen.findByText(flatDeletedRow.name)

    fireEvent.click(restoreButtons()[1])

    await waitFor(() => {
      expect(requestCount(fetchMock, 'POST', restoreFlatPath)).toBe(1)
    })
    expect(requestCount(fetchMock, 'POST', restoreDinnerPath)).toBe(0)
    expect(requestsOf(fetchMock, 'POST', restoreFlatPath)[0].body).toBeUndefined()
  })

  it.each(languages)('shows the Group restored toast after a restore (%s)', async (language) => {
    stubRecentlyDeleted(jsonAnswer(twoDeletedGroups), {
      [`POST ${restoreDinnerPath}`]: noContentAnswer(),
    })
    await renderRecentlyDeleted(language)
    await screen.findByText(dinnerDeletedRow.name)

    fireEvent.click(restoreButtons(language)[0])

    await waitFor(() => {
      expect(shownToastTexts()).toContain(translated(language, 'recentlyDeleted.restored'))
    })
  })

  it('takes the restored group off the list and keeps the other one', async () => {
    let isRestored = false
    stubRecentlyDeleted(
      () =>
        Response.json(
          isRestored
            ? groupListOf({ groups: [greeceGroupRow], recentlyDeleted: [flatDeletedRow] })
            : twoDeletedGroups,
        ),
      {
        [`POST ${restoreDinnerPath}`]: () => {
          isRestored = true
          return new Response(null, { status: 204 })
        },
      },
    )
    await renderRecentlyDeleted()
    await screen.findByText(dinnerDeletedRow.name)

    fireEvent.click(restoreButtons()[0])

    await waitFor(() => {
      expect(screen.queryByText(dinnerDeletedRow.name)).toBeNull()
    })
    expect(screen.getByText(flatDeletedRow.name)).toBeTruthy()
  })

  it('shows the empty text after the last deleted group is restored', async () => {
    let isRestored = false
    stubRecentlyDeleted(
      () =>
        Response.json(
          isRestored
            ? emptyGroupList
            : groupListOf({ recentlyDeleted: [dinnerDeletedRow] }),
        ),
      {
        [`POST ${restoreDinnerPath}`]: () => {
          isRestored = true
          return new Response(null, { status: 204 })
        },
      },
    )
    await renderRecentlyDeleted()
    await screen.findByText(dinnerDeletedRow.name)

    fireEvent.click(restoreButtons()[0])

    expect(await screen.findByText(translated('en', 'recentlyDeleted.empty'))).toBeTruthy()
  })

  it.each(
    languages.flatMap((language) =>
      (['GROUP_RESTORE_EXPIRED', 'GROUP_NOT_DELETED'] as const).map((code) => [language, code] as const),
    ),
  )('shows the %s error toast for the answer %s and keeps the group on the list', async (language, code) => {
    stubRecentlyDeleted(jsonAnswer(twoDeletedGroups), {
      [`POST ${restoreDinnerPath}`]: problemAnswer(400, code),
    })
    vi.spyOn(console, 'error').mockImplementation(() => {})
    await renderRecentlyDeleted(language)
    await screen.findByText(dinnerDeletedRow.name)

    fireEvent.click(restoreButtons(language)[0])

    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledWith(translated(language, `errors.${code}`))
    })
    expect(screen.getByText(dinnerDeletedRow.name)).toBeTruthy()
    expect(shownToastTexts()).not.toContain(translated(language, 'recentlyDeleted.restored'))
  })

  it('shows the network error toast when the restore cannot reach the server', async () => {
    stubRecentlyDeleted(jsonAnswer(twoDeletedGroups), {
      [`POST ${restoreDinnerPath}`]: networkFailureAnswer(),
    })
    vi.spyOn(console, 'error').mockImplementation(() => {})
    await renderRecentlyDeleted()
    await screen.findByText(dinnerDeletedRow.name)

    fireEvent.click(restoreButtons()[0])

    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.network'))
    })
  })

  it('shows the translated error with a retry button when the groups cannot be loaded', async () => {
    stubRecentlyDeleted(() => new Response(null, { status: 500 }))

    await renderRecentlyDeleted()

    expect((await screen.findByRole('alert')).textContent).toContain(
      translated('en', 'errors.generic'),
    )
    expect(screen.getByRole('button', { name: translated('en', 'common.retry') })).toBeTruthy()
  })

  it('asks for the groups again and shows them when the retry button is pressed', async () => {
    let tries = 0
    stubRecentlyDeleted(() => {
      tries += 1
      return tries === 1 ? new Response(null, { status: 500 }) : Response.json(twoDeletedGroups)
    })
    await renderRecentlyDeleted()

    fireEvent.click(await screen.findByRole('button', { name: translated('en', 'common.retry') }))

    expect(await screen.findByText(dinnerDeletedRow.name)).toBeTruthy()
  })
})
