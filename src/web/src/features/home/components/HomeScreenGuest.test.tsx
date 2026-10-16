import { fireEvent, screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import {
  allGroupsFinishedDashboard,
  greeceGroupId,
  newUserDashboard,
} from '@/test/dashboardTestData'
import { renderHome } from '@/test/homeTestHelpers'
import { stubDesktopScreen } from '@/test/matchMediaTestHelpers'
import { jsonAnswer } from '@/test/requestTestHelpers'
import { seedMe, testGuestMe } from '@/test/testMe'
import { translated } from '@/test/translated'

function renderGuestHome(options: Parameters<typeof renderHome>[0] = {}) {
  const language: Language = options.language ?? 'en'
  return renderHome({
    ...options,
    answers: { 'GET /api/me': jsonAnswer({ ...testGuestMe, language }), ...options.answers },
    seedCache: seedMe({ ...testGuestMe, language }),
  })
}

async function openPlusMenu(language: Language = 'en'): Promise<HTMLElement> {
  fireEvent.click(await screen.findByRole('button', { name: translated(language, 'home.plus') }))
  return screen.getByRole('menu')
}

describe('the home screen for a guest', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('offers Add to the last group and Another group in the plus menu, but no New One bill (%s)', async (language) => {
    await renderGuestHome({ language })

    const menu = await openPlusMenu(language)

    expect(within(menu).getAllByRole('menuitem').map((item) => item.getAttribute('href'))).toEqual([
      routes.groupExpenseNew(greeceGroupId),
      routes.groups,
    ])
    expect(within(menu).queryByRole('menuitem', { name: translated(language, 'home.plusNewOneBill') })).toBeNull()
  })

  it('keeps adding an expense at three taps for a guest: plus, Add to, then Save on the next screen', async () => {
    await renderGuestHome()

    const menu = await openPlusMenu()

    expect(
      within(menu).getByRole('menuitem', { name: translated('en', 'home.plusAddTo', { group: 'Greece trip' }) }).getAttribute('href'),
    ).toBe(routes.groupExpenseNew(greeceGroupId))
  })

  it('shows no New One bill round button on a desktop-sized window', async () => {
    stubDesktopScreen()
    await renderGuestHome()

    const addTo = await screen.findByRole('link', { name: translated('en', 'home.plusAddTo', { group: 'Greece trip' }) })

    expect(addTo).toBeTruthy()
    expect(screen.getByRole('link', { name: translated('en', 'home.plusAnotherGroup') })).toBeTruthy()
    expect(screen.queryByRole('link', { name: translated('en', 'home.plusNewOneBill') })).toBeNull()
  })

  it.each(languages)('shows the guest hint and no New group button when the guest has no groups (%s)', async (language) => {
    await renderGuestHome({ language, dashboard: newUserDashboard })

    expect(await screen.findByText(translated(language, 'home.noGroupsTitle'))).toBeTruthy()
    expect(screen.getByText(translated(language, 'home.noGroupsHintGuest'))).toBeTruthy()
    expect(screen.queryByText(translated(language, 'home.noGroupsHint'))).toBeNull()
    expect(screen.queryByRole('link', { name: translated(language, 'groups.newButton') })).toBeNull()
  })

  it.each(languages)('shows the no-open-groups card without a New group button (%s)', async (language) => {
    await renderGuestHome({ language, dashboard: allGroupsFinishedDashboard })

    expect(await screen.findByText(translated(language, 'home.noOpenGroupsTitle'))).toBeTruthy()
    expect(screen.queryByRole('link', { name: translated(language, 'groups.newButton') })).toBeNull()
  })
})
