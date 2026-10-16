import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import {
  allGroupsFinishedDashboard,
  dashboardOf,
  dashboardPath,
  fullDashboard,
  newUserDashboard,
} from '@/test/dashboardTestData'
import { renderHome, renderHomeWaitingForServer } from '@/test/homeTestHelpers'
import {
  jsonAnswer,
  networkFailureAnswer,
  requestCount,
} from '@/test/requestTestHelpers'
import { loadingLook } from '@/test/skeletonTestHelpers'
import { translated } from '@/test/translated'

function plusButton(language: 'en' | 'mk' = 'en'): HTMLElement | null {
  return screen.queryByRole('button', { name: translated(language, 'home.plus') })
}

describe('the loading state', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('shows the loading status with pulsing placeholders, not a spinner, while the server has not answered', async () => {
    await renderHomeWaitingForServer()

    expect(screen.getByRole('status')).toBeTruthy()
    const look = loadingLook()
    expect(look.hasSpinner).toBe(false)
    expect(look.skeletonCount).toBeGreaterThan(0)
  })

  it('shows the greeting and the round Settings button while loading', async () => {
    await renderHomeWaitingForServer()

    expect(screen.getByText(translated('en', 'home.greeting', { name: 'Filip' }))).toBeTruthy()
    expect(screen.getByRole('link', { name: translated('en', 'settings.title') })).toBeTruthy()
  })

  it('shows no totals, no groups, no empty state and no plus button while loading', async () => {
    await renderHomeWaitingForServer()

    expect(screen.queryByRole('region', { name: translated('en', 'home.owed') })).toBeNull()
    expect(screen.queryByRole('heading', { name: translated('en', 'home.groups') })).toBeNull()
    expect(screen.queryByText(translated('en', 'home.noGroupsTitle'))).toBeNull()
    expect(plusButton()).toBeNull()
  })
})

describe('the empty state', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('shows the title and the hint for a person without groups in %s', async (language) => {
    await renderHome({ language, dashboard: newUserDashboard })

    expect(await screen.findByText(translated(language, 'home.noGroupsTitle'))).toBeTruthy()
    expect(screen.getByText(translated(language, 'home.noGroupsHint'))).toBeTruthy()
  })

  it.each(languages)('has a New group button in %s that opens the New group screen', async (language) => {
    const { router } = await renderHome({ language, dashboard: newUserDashboard })
    await screen.findByText(translated(language, 'home.noGroupsTitle'))

    const button = screen.getByText(translated(language, 'groups.newButton')).closest('a, button')
    expect(button).not.toBeNull()
    fireEvent.click(button as Element)

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.newGroup)
    })
  })

  it('shows no plus button, because the empty state has its own New group button', async () => {
    await renderHome({ dashboard: newUserDashboard })
    await screen.findByText(translated('en', 'home.noGroupsTitle'))

    expect(plusButton()).toBeNull()
  })

  it('shows no totals, no group cards and no recent activity', async () => {
    await renderHome({ dashboard: newUserDashboard })
    await screen.findByText(translated('en', 'home.noGroupsTitle'))

    expect(screen.queryByRole('region', { name: translated('en', 'home.owed') })).toBeNull()
    expect(screen.queryByRole('region', { name: translated('en', 'home.owe') })).toBeNull()
    expect(screen.queryByRole('heading', { name: translated('en', 'home.groups') })).toBeNull()
    expect(screen.queryByRole('heading', { name: translated('en', 'home.recentActivity') })).toBeNull()
  })

  it('still shows the greeting', async () => {
    await renderHome({ dashboard: newUserDashboard })
    await screen.findByText(translated('en', 'home.noGroupsTitle'))

    expect(screen.getByText(translated('en', 'home.greeting', { name: 'Filip' }))).toBeTruthy()
  })

  it('does not show the empty state to a person who has groups', async () => {
    await renderHome()
    await screen.findByRole('heading', { name: translated('en', 'home.groups') })

    expect(screen.queryByText(translated('en', 'home.noGroupsTitle'))).toBeNull()
  })

  it('shows the empty state when the server lists no groups and no recent activity, whatever else it sends', async () => {
    await renderHome({
      dashboard: dashboardOf({ groups: [], needsYou: [], recentActivity: [], plusTarget: null }),
    })

    expect(await screen.findByText(translated('en', 'home.noGroupsTitle'))).toBeTruthy()
  })
})

describe('the no open groups card', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('shows the short title for a person whose groups are all finished in %s', async (language) => {
    await renderHome({ language, dashboard: allGroupsFinishedDashboard })

    expect(await screen.findByText(translated(language, 'home.noOpenGroupsTitle'))).toBeTruthy()
  })

  it('does not show the full empty state, because the person has used the app before', async () => {
    await renderHome({ dashboard: allGroupsFinishedDashboard })
    await screen.findByText(translated('en', 'home.noOpenGroupsTitle'))

    expect(screen.queryByText(translated('en', 'home.noGroupsTitle'))).toBeNull()
    expect(screen.queryByText(translated('en', 'home.noGroupsHint'))).toBeNull()
  })

  it('does not show the short card on the full empty state of a new person', async () => {
    await renderHome({ dashboard: newUserDashboard })
    await screen.findByText(translated('en', 'home.noGroupsTitle'))

    expect(screen.queryByText(translated('en', 'home.noOpenGroupsTitle'))).toBeNull()
  })

  it('does not show the short card to a person who has open groups', async () => {
    await renderHome()
    await screen.findByRole('heading', { name: translated('en', 'home.groups') })

    expect(screen.queryByText(translated('en', 'home.noOpenGroupsTitle'))).toBeNull()
  })

  it.each(languages)('shows the Recent activity heading and its rows in %s', async (language) => {
    await renderHome({ language, dashboard: allGroupsFinishedDashboard })

    expect(await screen.findByRole('heading', { name: translated(language, 'home.recentActivity') })).toBeTruthy()
    expect(screen.getAllByRole('listitem').length).toBe(allGroupsFinishedDashboard.recentActivity.length)
  })

  it('puts the short card above Recent activity', async () => {
    await renderHome({ dashboard: allGroupsFinishedDashboard })
    const title = await screen.findByText(translated('en', 'home.noOpenGroupsTitle'))

    const heading = screen.getByRole('heading', { name: translated('en', 'home.recentActivity') })

    expect(title.compareDocumentPosition(heading) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
  })

  it.each(languages)('has exactly one New group button in %s that opens the New group screen', async (language) => {
    const { router } = await renderHome({ language, dashboard: allGroupsFinishedDashboard })
    await screen.findByText(translated(language, 'home.noOpenGroupsTitle'))

    const buttons = screen.getAllByText(translated(language, 'groups.newButton'))
    expect(buttons.length).toBe(1)
    const button = buttons[0].closest('a, button')
    expect(button).not.toBeNull()
    fireEvent.click(button as Element)

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.newGroup)
    })
  })

  it('shows no empty Your groups list, because there is no open group to list', async () => {
    await renderHome({ dashboard: allGroupsFinishedDashboard })
    await screen.findByText(translated('en', 'home.noOpenGroupsTitle'))

    expect(screen.queryByRole('heading', { name: translated('en', 'home.groups') })).toBeNull()
  })

  it('still shows the greeting', async () => {
    await renderHome({ dashboard: allGroupsFinishedDashboard })
    await screen.findByText(translated('en', 'home.noOpenGroupsTitle'))

    expect(screen.getByText(translated('en', 'home.greeting', { name: 'Filip' }))).toBeTruthy()
  })
})

describe('the saved dashboard', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('shows the saved dashboard at once, with no loading spinner', async () => {
    await renderHome({
      seedCache: (queryClient) => queryClient.setQueryData(['dashboard'], fullDashboard),
      answers: { [`GET ${dashboardPath}`]: jsonAnswer(fullDashboard) },
    })

    expect(screen.getByRole('heading', { name: translated('en', 'home.groups') })).toBeTruthy()
    expect(screen.queryByRole('status')).toBeNull()
  })

  it('asks the server for a fresh dashboard behind the saved one and replaces it', async () => {
    const { fetchMock } = await renderHome({
      seedCache: (queryClient) => queryClient.setQueryData(['dashboard'], fullDashboard),
      answers: { [`GET ${dashboardPath}`]: jsonAnswer(newUserDashboard) },
    })

    expect(await screen.findByText(translated('en', 'home.noGroupsTitle'))).toBeTruthy()
    expect(requestCount(fetchMock, 'GET', dashboardPath)).toBe(1)
  })
})

describe('the error state', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('shows the translated network text with a retry button, and the greeting, when the server cannot be reached', async () => {
    await renderHome({ answers: { [`GET ${dashboardPath}`]: networkFailureAnswer() } })

    const alert = await screen.findByRole('alert')

    expect(alert.textContent).toContain(translated('en', 'errors.network'))
    expect(screen.getByRole('button', { name: translated('en', 'common.retry') })).toBeTruthy()
    expect(screen.getByText(translated('en', 'home.greeting', { name: 'Filip' }))).toBeTruthy()
  })

  it.each(languages)('shows the generic text for an answer without an error code in %s', async (language) => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    await renderHome({
      language,
      answers: { [`GET ${dashboardPath}`]: () => new Response(null, { status: 500 }) },
    })

    expect((await screen.findByRole('alert')).textContent).toContain(translated(language, 'errors.generic'))
  })

  it('shows the generic text, not a raw error, when the answer has the wrong shape', async () => {
    await renderHome({ answers: { [`GET ${dashboardPath}`]: jsonAnswer({ groups: [] }) } })

    const alert = await screen.findByRole('alert')

    expect(alert.textContent).toContain(translated('en', 'errors.generic'))
    expect(alert.textContent).not.toContain('totals')
  })

  it('shows no totals, groups or plus button while the error is shown', async () => {
    await renderHome({ answers: { [`GET ${dashboardPath}`]: networkFailureAnswer() } })
    await screen.findByRole('alert')

    expect(screen.queryByRole('region', { name: translated('en', 'home.owed') })).toBeNull()
    expect(screen.queryByRole('heading', { name: translated('en', 'home.groups') })).toBeNull()
    expect(plusButton()).toBeNull()
  })

  it('asks again when retry is tapped and shows the dashboard when the second try works', async () => {
    let timesAsked = 0
    const { fetchMock } = await renderHome({
      answers: {
        [`GET ${dashboardPath}`]: () => {
          timesAsked += 1
          return timesAsked === 1 ? networkFailureAnswer()() : Response.json(fullDashboard)
        },
      },
    })
    await screen.findByRole('alert')

    fireEvent.click(screen.getByRole('button', { name: translated('en', 'common.retry') }))

    expect(await screen.findByRole('heading', { name: translated('en', 'home.groups') })).toBeTruthy()
    expect(requestCount(fetchMock, 'GET', dashboardPath)).toBe(2)
    expect(screen.queryByRole('alert')).toBeNull()
  })
})
