import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { useRouteError } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import {
  dashboardOf,
  dashboardPath,
  fullDashboard,
  longMacedonianGroupName,
  longNameGroupId,
  newUserDashboard,
} from '@/test/dashboardTestData'
import { renderHome } from '@/test/homeTestHelpers'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { jsonAnswer, stubFetchByRequest } from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { HomeScreen } from './HomeScreen'

function ShownRouteError() {
  const error: unknown = useRouteError()
  return <p>{error instanceof Error ? error.message : 'not an Error'}</p>
}

async function openPlusMenu(language: 'en' | 'mk' = 'en'): Promise<HTMLElement> {
  const button = await screen.findByRole('button', { name: translated(language, 'home.plus') })
  fireEvent.click(button)
  return screen.getByRole('menu')
}

describe('the plus button on the home screen', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it.each(languages)('shows a plus button named Add when the person has groups, in %s', async (language) => {
    await renderHome({ language })

    expect(await screen.findByRole('button', { name: translated(language, 'home.plus') })).toBeTruthy()
  })

  it('shows no plus button for a person without groups', async () => {
    await renderHome({ dashboard: newUserDashboard })
    await screen.findByText(translated('en', 'home.noGroupsTitle'))

    expect(screen.queryByRole('button', { name: translated('en', 'home.plus') })).toBeNull()
  })

  it('opens a menu with the three items, the first one naming the group of the plus target', async () => {
    await renderHome()

    const menu = await openPlusMenu()

    const items = within(menu).getAllByRole('menuitem')
    expect(items).toHaveLength(3)
    expect(items[0]).toBe(
      within(menu).getByRole('menuitem', { name: translated('en', 'home.plusAddTo', { group: 'Greece trip' }) }),
    )
    expect(items[1]).toBe(within(menu).getByRole('menuitem', { name: translated('en', 'home.plusNewOneBill') }))
    expect(items[2]).toBe(within(menu).getByRole('menuitem', { name: translated('en', 'home.plusAnotherGroup') }))
  })

  it.each(languages)('has no New group item, because a group is made only in the Groups section, in %s', async (language) => {
    await renderHome({ language })

    const menu = await openPlusMenu(language)

    expect(within(menu).queryByRole('menuitem', { name: translated(language, 'groups.newButton') })).toBeNull()
  })

  it.each(languages)('writes the first item in %s', async (language) => {
    await renderHome({ language })

    const menu = await openPlusMenu(language)

    expect(
      within(menu).getByRole('menuitem', { name: translated(language, 'home.plusAddTo', { group: 'Greece trip' }) }),
    ).toBeTruthy()
  })

  it('names the target group by its name from the list of groups, whichever group the server chose', async () => {
    await renderHome({ dashboard: dashboardOf({ plusTarget: { groupId: longNameGroupId } }) })

    const menu = await openPlusMenu()

    expect(
      within(menu).getByRole('menuitem', { name: translated('en', 'home.plusAddTo', { group: longMacedonianGroupName }) }),
    ).toBeTruthy()
  })

  it('opens Add expense of the target group when the first item is tapped', async () => {
    const { router } = await renderHome({ dashboard: dashboardOf({ plusTarget: { groupId: longNameGroupId } }) })
    const menu = await openPlusMenu()

    fireEvent.click(
      within(menu).getByRole('menuitem', { name: translated('en', 'home.plusAddTo', { group: longMacedonianGroupName }) }),
    )

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.groupExpenseNew(longNameGroupId))
    })
  })

  it('has only two items, with no Add to, when the server sends no plus target', async () => {
    await renderHome({ dashboard: dashboardOf({ plusTarget: null }) })

    const menu = await openPlusMenu()

    const items = within(menu).getAllByRole('menuitem')
    expect(items).toHaveLength(2)
    expect(items[0]).toBe(within(menu).getByRole('menuitem', { name: translated('en', 'home.plusNewOneBill') }))
    expect(items[1]).toBe(within(menu).getByRole('menuitem', { name: translated('en', 'home.plusAnotherGroup') }))
    expect(within(menu).queryByRole('menuitem', { name: /Greece trip/ })).toBeNull()
  })

  it('opens the One bill screen from New One bill', async () => {
    const { router } = await renderHome()
    const menu = await openPlusMenu()

    fireEvent.click(within(menu).getByRole('menuitem', { name: translated('en', 'home.plusNewOneBill') }))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.newBill)
    })
  })

  it.each([0, 1, 2])('never opens a New group screen when item number %i is tapped', async (index) => {
    const { router } = await renderHome()
    const menu = await openPlusMenu()

    fireEvent.click(within(menu).getAllByRole('menuitem')[index])

    await waitFor(() => {
      expect(router.state.location.pathname).not.toBe(routes.dashboard)
    })
    expect(router.state.location.pathname).not.toBe(routes.newGroup)
    expect(router.state.location.pathname).not.toBe(routes.newGroupGroup)
  })

  it('opens the Groups list from Another group', async () => {
    const { router } = await renderHome()
    const menu = await openPlusMenu()

    fireEvent.click(within(menu).getByRole('menuitem', { name: translated('en', 'home.plusAnotherGroup') }))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.groups)
    })
  })

  it('throws an error naming the group id when the plus target is not one of the listed groups', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    const strayId = 'ffffffff-0000-4000-8000-000000000000'
    stubFetchByRequest({
      [`GET ${dashboardPath}`]: jsonAnswer({ ...fullDashboard, plusTarget: { groupId: strayId } }),
    })

    await renderRoutesWithProviders(
      [{ path: routes.dashboard, element: <HomeScreen />, errorElement: <ShownRouteError /> }],
      routes.dashboard,
      { seedCache: seedMe(testMe) },
    )

    expect(await screen.findByText(new RegExp(strayId))).toBeTruthy()
  })
})
