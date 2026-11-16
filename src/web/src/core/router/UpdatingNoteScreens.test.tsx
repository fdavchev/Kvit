import type { QueryClient } from '@tanstack/react-query'
import { screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { checkRestoredMeWithServer } from '@/core/api/savedQueryCache'
import { meQueryKey } from '@/core/auth/useMe'
import type { Me } from '@/core/services/me/meService'
import { useApiHealth } from '@/features/auth/welcome/hooks/useApiHealth'
import { testGroup } from '@/test/groupTestData'
import { stubDesktopScreen } from '@/test/matchMediaTestHelpers'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { stubFetchThatNeverAnswers } from '@/test/apiTestHelpers'
import { testMe } from '@/test/testMe'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { routeObjects } from './router'
import { routes } from './routes'

vi.mock('@/features/auth/welcome/hooks/useApiHealth', () => ({
  useApiHealth: vi.fn(),
}))

const expenseId = '5b2f9a10-6c34-4d7e-8f01-2a3b4c5d6e7f'
const beforeThePageOpened = performance.timeOrigin - 60 * 60 * 1000

const signedInScreens = [
  ['Home', routes.dashboard],
  ['Groups', routes.groups],
  ['Budget', routes.budget],
  ['Settings', routes.settings],
  ['a group page', routes.group(testGroup.id)],
  ['the group Totals tab', routes.groupTotals(testGroup.id)],
  ['Members', routes.groupMembers(testGroup.id)],
  ['Add expense', routes.groupExpenseNew(testGroup.id)],
  ['an expense detail', routes.groupExpense(testGroup.id, expenseId)],
  ['Edit expense', routes.groupExpenseEdit(testGroup.id, expenseId)],
  ['New group', routes.newGroup],
] as const

function restoredMe(me: Me): (queryClient: QueryClient) => void {
  return (queryClient) => {
    queryClient.setQueryData<Me | null>(meQueryKey, me, { updatedAt: beforeThePageOpened })
    void checkRestoredMeWithServer(queryClient)
  }
}

function updatingNotes(): HTMLElement[] {
  return screen.queryAllByText(translated('en', 'common.updating'))
}

describe('the Updating… note on the signed-in screens of the real route table', () => {
  beforeEach(() => {
    vi.mocked(useApiHealth).mockReset()
    stubDeviceColorScheme('light')
    stubFetchThatNeverAnswers()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(signedInScreens)('shows the note once on %s while the saved person is checked with the server', async (_name, path) => {
    await renderRoutesWithProviders(routeObjects, path, { seedCache: restoredMe(testMe) })

    expect(await screen.findByText(translated('en', 'common.updating'))).toBeTruthy()
    expect(updatingNotes()).toHaveLength(1)
    expect(document.querySelectorAll('[data-updating-note-slot]')).toHaveLength(1)
  })

  it('shows the note on a desktop-sized window on a group page too, where the side menu is', async () => {
    stubDesktopScreen()

    await renderRoutesWithProviders(routeObjects, routes.group(testGroup.id), { seedCache: restoredMe(testMe) })

    expect(await screen.findByText(translated('en', 'common.updating'))).toBeTruthy()
    expect(document.querySelector('[data-kvit-rail]')).toBeTruthy()
    expect(document.querySelectorAll('[data-updating-note-slot]')).toHaveLength(1)
  })
})
