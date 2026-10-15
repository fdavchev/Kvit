import type { ReactElement } from 'react'
import { screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { routes } from '@/core/router/routes'
import { dinnerDeletedRow, greeceGroupRow, groupListOf } from '@/test/groupTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { jsonAnswer, stubFetchByRequest } from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { GroupsScreen } from './GroupsScreen'
import { RecentlyDeletedScreen } from './RecentlyDeletedScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const oneVeryLongMacedonianWord = 'Прекуокеанскотпатувањезаседумнаесетпријателизаедно'

const groupList = groupListOf({
  groups: [{ ...greeceGroupRow, name: oneVeryLongMacedonianWord }],
  recentlyDeleted: [{ ...dinnerDeletedRow, name: oneVeryLongMacedonianWord }],
})

async function renderScreen(path: string, element: ReactElement): Promise<HTMLElement> {
  stubFetchByRequest({ 'GET /api/groups': jsonAnswer(groupList) })
  await renderRoutesWithProviders([{ path, element }], path, {
    language: 'mk',
    seedCache: seedMe({ ...testMe, language: 'mk' }),
  })
  return screen.findByText(oneVeryLongMacedonianWord)
}

function staysInsideItsRow(name: HTMLElement): boolean {
  const cutsOffOrBreaks = name.classList.contains('truncate') || name.classList.contains('wrap-anywhere')
  return cutsOffOrBreaks && name.parentElement?.classList.contains('min-w-0') === true
}

describe('a group name that is one very long word in the lists', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('is cut off or broken inside its row on the Groups list', async () => {
    const name = await renderScreen(routes.groups, <GroupsScreen />)

    expect(staysInsideItsRow(name)).toBe(true)
  })

  it('is cut off or broken inside its row on the Recently deleted list', async () => {
    const name = await renderScreen(routes.recentlyDeletedGroups, <RecentlyDeletedScreen />)

    expect(staysInsideItsRow(name)).toBe(true)
  })
})
